import socket
import struct
import torch
import torch.nn as nn
import onnxruntime as ort
import numpy as np
import cv2
from PIL import Image
from queue import Queue
import torchvision.transforms as T
import torchvision.models as models
import threading
from threading import Thread, Semaphore
import nms
import io
import os
import glob
import time
import csv

save_lock = threading.Lock()
os.chdir("C:/Users/smsla/MultiAgent/py")
#os.chdir("C:/Users/dudrj/unityworkspace/BinPicking/py")
print(os.getcwd())
# 모델 로드 (한 번만 실행)
onnx_model_path = "best.onnx"
session = ort.InferenceSession(onnx_model_path, providers=['CUDAExecutionProvider'])
print(session.get_providers())

class GraspabilityModel(nn.Module):
    def __init__(self, feature_dim=256):
        super().__init__()
        # ResNet18 구조와 동일하게 생성
        resnet = models.resnet18(weights=models.ResNet18_Weights.IMAGENET1K_V1)
        resnet.conv1 = nn.Conv2d(3, 64, kernel_size=3, stride=1, padding=1, bias=False)
        resnet.maxpool = nn.Identity()
        self.features = nn.Sequential(*list(resnet.children())[:-1])  # (B, 512, 4, 4)
        self.flatten = nn.Flatten()
        self.fc = nn.Linear(512, feature_dim)
        self.output = nn.Linear(feature_dim, 1)  # 노드 1개짜리 출력층 추가

        # feature extractor와 fc는 freeze
        for param in self.features.parameters():
            param.requires_grad = False
        for param in self.fc.parameters():
            param.requires_grad = False
        # output만 학습 가능
        for param in self.output.parameters():
            param.requires_grad = True

    def forward(self, x):
        x = self.features(x)
        x = self.flatten(x)
        feature_vec = self.fc(x)
        grasp_prob = torch.sigmoid(self.output(feature_vec)).squeeze(1)
        return grasp_prob, feature_vec
    

# 이미지 전처리 함수
def preprocess_image(image):
    img = cv2.resize(image, (736, 736))
    img = img.astype(np.float32) / 255.0
    img = np.transpose(img, (2, 0, 1))
    return img[np.newaxis, :]

def clean_online_data():
    save_dir = "online_data"
    if not os.path.exists(save_dir):
        return
    for fname in os.listdir(save_dir):
        file_path = os.path.join(save_dir, fname)
        try:
            os.remove(file_path)
            print(f"Deleted: {file_path}")
        except Exception as e:
            print(f"Failed to delete {file_path}: {e}")

def get_next_data_no():
    save_dir = "online_data"
    pattern = os.path.join(save_dir, f"*_*_*.png")
    files = glob.glob(pattern)
    max_no = -1
    for f in files:
        base = os.path.basename(f)
        parts = base.split("_")
        if len(parts) >= 3:
            try:
                no = int(parts[0])
                if no > max_no:
                    max_no = no
            except ValueError:
                continue
    return max_no + 1

# Bounding Box 시각화 함수
def draw_bounding_boxes(image, detections, save_path="output.png"):
    img = np.array(image)
    colors = {
        0: (255, 0, 0),   # Class 0: Red
        1: (0, 255, 0),   # Class 1: Green
        2: (0, 0, 255),   # Class 2: Blue
    }
    for det in detections[0]:
        x1, y1, x2, y2, conf, class_id = det
        x1, y1, x2, y2 = map(int, [x1, y1, x2, y2])
        x = (x1+x2)//2
        y = (y1+y2)//2
        width = 120
        height = 120
        x_new1, y_new1, x_new2, y_new2 = map(int, [x-(width/2), y-(height/2), x+(width/2), y+(height/2)])
        color = colors.get(int(class_id), (0, 255, 0))
        #cv2.rectangle(img, (x1, y1), (x2, y2), color, 2)
        cv2.rectangle(img, (x_new1, y_new1), (x_new2, y_new2), color, 2)
        cv2.putText(img, f"Class {int(class_id)}: {conf:.2f}", (x1, y1 - 10),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.5, color, 2)
    cv2.imwrite(save_path, cv2.cvtColor(img, cv2.COLOR_RGB2BGR))

# YOLO 추론 및 NMS 적용
def run_inference(img_array):
    #img = Image.fromarray(img_array.astype(np.uint8))
    img_array = preprocess_image(img_array)
    input_name = session.get_inputs()[0].name
    output_name = session.get_outputs()[0].name
    raw_output = session.run([output_name], {input_name: img_array.astype(np.float32)})
    output_data = raw_output[0].squeeze(0)
    
    conf_thres = 0.45
    iou_thres = 0.35
    mask = output_data[:, 4] > conf_thres
    boxes = output_data[mask, :4]
    confidence = output_data[mask, 4]
    class_probs = output_data[mask, 5:]
    prediction = torch.cat((torch.tensor(boxes), torch.tensor(confidence).unsqueeze(1), torch.tensor(class_probs)), 1)
    prediction = prediction.unsqueeze(0)
    
    # NMS 적용
    nms_output = nms.non_max_suppression(prediction, conf_thres=conf_thres, iou_thres=iou_thres)
    
    return nms_output

def extract_objects(image, detections):
    save_dir="cropped_objects"
    os.makedirs(save_dir, exist_ok=True)
    objects = []
    for i, det in enumerate(detections[0]):
        x1, y1, x2, y2, conf, class_id = map(int, det[:6])
        
        # 중심 좌표 계산
        x = (x1 + x2) // 2
        y = (y1 + y2) // 2
        width = 120
        height = 120

        # 크롭 영역 계산 (이미지 경계 확인)
        x_new1 = max(0, x - (width // 2))
        y_new1 = max(0, y - (height // 2))
        x_new2 = min(image.shape[1], x + (width // 2))
        y_new2 = min(image.shape[0], y + (height // 2))

        # 크롭된 이미지 추가
        cropped_img = image[y_new1:y_new2, x_new1:x_new2]
        objects.append(cropped_img)

    return objects

def run_graspability_model(instance_id, img_array):
    global data_no

    yolo_start = time.time()
    detections = run_inference(img_array)
    yolo_end = time.time()

    grasp_start = time.time()
    cropped_objects = extract_objects(img_array, detections)
    for i, obj in enumerate(cropped_objects):
        if not isinstance(obj, np.ndarray):
            print(f"cropped_objects[{i}] is not ndarray: {type(obj)}")
        
    if cropped_objects:
        objects_tensor = torch.from_numpy(np.array(cropped_objects)).permute(0, 3, 1, 2).float() / 255.0
        objects_tensor = objects_tensor.to(device)
        with torch.no_grad():
            grasp_probs, feature_vectors = grasp_model(objects_tensor)
        
    else:
        print(f"No objects detected in image {instance_id}.")

    grasp_probs_np = grasp_probs.cpu().numpy()
    best_idx = int(np.argmax(grasp_probs_np))
    best_feature = feature_vectors[best_idx].cpu().numpy()
    best_prob = grasp_probs_np[best_idx]
    best_img = cropped_objects[best_idx]

    # best info 출력
    x1, y1, x2, y2, conf, class_id = detections[0][best_idx]
    x = float((x1 + x2) / 2)
    y = float((y1 + y2) / 2)
    print(f"Best Object: Class: {class_id}, Confidence: {conf}, Graspability: {best_prob}")
    # response 생성
    response = struct.pack('I', len(best_feature))
    response += struct.pack(f'{len(best_feature)}f', *best_feature)
    response += struct.pack('2f', x, y)

    # img 저장
    save_dir="online_data"
    os.makedirs(save_dir, exist_ok=True)
    save_path = os.path.join(save_dir, f"{data_no}_{instance_id}_{best_prob:.4f}.png")
    data_no += 1
    cv2.imwrite(save_path, cv2.cvtColor(best_img, cv2.COLOR_RGB2BGR))
    grasp_end = time.time()
    print(f"[Timing] YOLO inference: {yolo_end - yolo_start:.4f}s, Graspability model inference: {grasp_end - grasp_start:.4f}s, Graspability Model Inference Count: {len(cropped_objects)}")
    return response


def online_learning_from_dir(batch_size=128):
    global optimizer
    pred_dir = "online_data"
    loss_log_path = "loss_log.csv" 
    # 피드백이 포함된 파일만 (가장 최신 128개)
    img_files = sorted(
        glob.glob(os.path.join(pred_dir, "*_[01].png")),
        key=os.path.getmtime,
        reverse=True
    )[:batch_size]
    if len(img_files) < batch_size:
        return  # 데이터가 충분하지 않으면 학습하지 않음

    images = []
    labels = []
    for img_file in img_files:
        img = cv2.imread(img_file)
        img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
        img = cv2.resize(img, (120, 120))
        images.append(img)
        # 파일명에서 피드백(0/1) 추출
        feedback = int(img_file.split("_")[-1].split(".")[0])
        labels.append(feedback)

    images = np.stack(images)
    images = torch.from_numpy(images).permute(0, 3, 1, 2).float() / 255.0
    labels = torch.tensor(labels, dtype=torch.float32)
    images = images.to(device)
    labels = labels.to(device)

    grasp_model.train()
    criterion = torch.nn.BCELoss()
    optimizer.zero_grad()
    outputs, _ = grasp_model(images)
    loss = criterion(outputs, labels)
    loss.backward()
    optimizer.step()
    grasp_model.eval()

    # === loss 기록 ===
    # loss_log.csv에 (timestamp, loss) 저장
    with save_lock:
        import datetime
        now = datetime.datetime.now().isoformat()
        with open(loss_log_path, "a", newline="") as f:
            writer = csv.writer(f)
            writer.writerow([now, float(loss.item())])

        # === best loss 체크포인트 저장 ===
        best_loss = float("inf")
        try:
            with open(loss_log_path, "r") as f:
                reader = csv.reader(f)
                for row in reader:
                    if len(row) >= 2:
                        try:
                            l = float(row[1])
                            if l < best_loss:
                                best_loss = l
                        except:
                            continue
        except FileNotFoundError:
            best_loss = float("inf")

        if loss.item() <= best_loss:
            torch.save(grasp_model.output.state_dict(), "grasp_out.pth")
            print(f"New best loss {loss.item():.4f}, checkpoint saved to grasp_out.pth")
        else:
            print(f"Loss {loss.item():.4f} (best: {best_loss:.4f})")

        for img_file in img_files:
            try:
                os.remove(img_file)
            except Exception as e:
                print(f"Failed to delete {img_file}: {e}")

        print(f"[Online Learning] Trained on {batch_size} samples. Loss: {loss.item():.4f}")

def recv_all(sock, n):
    data = b''
    while len(data) < n:
        packet = sock.recv(n - len(data))
        if not packet:
            raise ConnectionError("Socket connection lost")
        data += packet
    return data

def handle_client(client_socket):
    with concurrent_clients:
        comm_start = time.time() 
        try:
            # 1. AgentID (4 bytes, int)
            agent_id_bytes = recv_all(client_socket, 4)
            agent_id = int.from_bytes(agent_id_bytes, byteorder='little', signed=True)
            # 2. success (4 bytes, int)
            success_bytes = recv_all(client_socket, 4)
            success = int.from_bytes(success_bytes, byteorder='little', signed=True)
            # 3. 이미지 길이 (4 bytes, int)
            img_len_bytes = recv_all(client_socket, 4)
            img_len = int.from_bytes(img_len_bytes, byteorder='little', signed=True)
            # 4. 이미지 데이터 (img_len bytes)
            img_bytes = recv_all(client_socket, img_len)
            img_array = np.frombuffer(img_bytes, np.uint8)
            img_array = cv2.imdecode(img_array, cv2.IMREAD_COLOR)
            img_array = cv2.cvtColor(img_array, cv2.COLOR_BGR2RGB)
            

            # === success 처리 ===
            pred_dir = "online_data"
            if success in (0, 1):
                # agent_id로 {success}가 없는 가장 최근 파일 찾기
                pattern = os.path.join(pred_dir, f"*_{agent_id}_*.png")
                candidates = sorted(
                    [f for f in glob.glob(pattern) if not f.endswith(("_0.png", "_1.png"))],
                    key=os.path.getmtime, reverse=True
                )
                if candidates:
                    fname = candidates[0]
                    base = os.path.basename(fname)
                    parts = base.replace(".png", "").split("_")
                    if len(parts) == 3:
                        data_no_str, inst_id_str, best_prob_str = parts
                        new_name = os.path.join(pred_dir, f"{data_no_str}_{inst_id_str}_{best_prob_str}_{success}.png")
                        os.rename(fname, new_name)

            # === 추론 및 응답 ===
            infer_start = time.time()
            response = run_graspability_model(agent_id, img_array)
            infer_end = time.time() 
            client_socket.sendall(response)
            comm_end = time.time()
            if (infer_end - infer_start) > 0.07:
                print(f"Agent {agent_id} processed in {comm_end - comm_start:.2f}s (Inference: {infer_end - infer_start:.2f}s, Comm: {comm_end - comm_start - (infer_end - infer_start):.2f}s)")
            # === 온라인 학습 트리거 ===
            feedback_files = glob.glob(os.path.join(pred_dir, "*_[01].png"))
            if len(feedback_files) >= 128:
                online_learning_from_dir(batch_size=128)

        except Exception as e:
            print(f"Client disconnected: {e}")
            try:
                dummy_feature = [0.0] * 256
                dummy_response = struct.pack('I', 256)
                dummy_response += struct.pack('256f', *dummy_feature)
                dummy_response += struct.pack('2f', 736//2, 736//2)
                client_socket.sendall(dummy_response)
            except Exception as e2:
                print(f"Failed to send dummy response: {e2}")
        finally:
            client_socket.close()


def worker():
    while True:
        client_socket = request_queue.get()
        if client_socket is None:
            break
        handle_client(client_socket)

# 서버 설정
HOST = '127.0.0.1'
PORT = 7779
IMAGE_PATH = "./images"
NUM_WORKERS = 1

# GraspabilityModel 초기화
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
grasp_model = GraspabilityModel().to(device)

state_dict = torch.load("encoder/rn256/encoder_rn256.pth", map_location="cpu")
model_dict = grasp_model.state_dict()

pretrained_dict = {k: v for k, v in state_dict.items() if k in model_dict and 'output' not in k}
model_dict.update(pretrained_dict)
grasp_model.load_state_dict(model_dict)
print("Loaded encoder_rn256 weights (feature extractor + fc).")

output_ckpt = "grasp_out.pth"
if os.path.exists(output_ckpt):
    grasp_model.output.load_state_dict(torch.load(output_ckpt, map_location="cpu"))
    print(f"Loaded output head weights from {output_ckpt}")

grasp_model.eval()
optimizer = torch.optim.Adam(grasp_model.output.parameters(), lr=5e-4)  # 출력층만 학습

script_dir = os.path.dirname(os.path.abspath(__file__))
os.chdir(script_dir)
absolute_path = os.path.abspath(IMAGE_PATH)
print("이미지 파일의 절대 경로:", absolute_path)

clean_online_data()
global data_no
data_no = get_next_data_no()

server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server_socket.bind((HOST, PORT))
server_socket.listen(20)
server_socket.settimeout(None)  # 무한 대기 (기본값)

request_queue = Queue()

print(f"Server listening on {HOST}:{PORT}")

# Metrics tracking
concurrent_clients = Semaphore(32)  # Limit to 4 concurrent clients (adjust as needed)

for _ in range(NUM_WORKERS):
    Thread(target=worker, daemon=True).start()

while True:
    client_sock, addr = server_socket.accept()
    request_queue.put(client_sock)

