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
import matplotlib

#os.chdir("C:/Users/smsla/MultiAgent/py")
os.chdir("C:/Users/dudrj/unityworkspace/RL-Bin-Picking/py")
print(os.getcwd())
# 모델 로드 (한 번만 실행)
onnx_model_path = "best.onnx"
session = ort.InferenceSession(onnx_model_path)

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
    transform = T.Compose([
        T.Resize(736),
        T.ToTensor()
    ])
    img_tensor = transform(image).unsqueeze(0)
    return img_tensor.numpy()

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

# YOLO 추론 및 NMS 적용
def run_inference(img):
    img_array = preprocess_image(img)
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

def run_graspability_model(instance_id):
    global data_no
    img_path = os.path.join(IMAGE_PATH, f"image_{instance_id}.png")
    img = Image.open(img_path)
    img_array = np.array(img)
    detections = run_inference(img)

    cropped_objects = extract_objects(img_array, detections)
    if cropped_objects:
        objects_tensor = torch.from_numpy(np.array(cropped_objects)).permute(0, 3, 1, 2).float() / 255.0
        objects_tensor = objects_tensor.to(device)
        with torch.no_grad():
            grasp_probs, feature_vectors = grasp_model(objects_tensor)
    
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

    # === online_data 저장 및 online_learning 제거 ===
    # save_dir="online_data"
    # os.makedirs(save_dir, exist_ok=True)
    # save_path = os.path.join(save_dir, f"{data_no}_{instance_id}_{best_prob:.4f}.png")
    # data_no += 1
    # cv2.imwrite(save_path, cv2.cvtColor(best_img, cv2.COLOR_RGB2BGR))

    save_dir = "figures"
    os.makedirs(save_dir, exist_ok=True)
    vis_save_path = os.path.join(save_dir, f"{data_no}_{instance_id}_grasp_vis.png")
    cmap_save_path = os.path.join(save_dir, f"cmap.png")
    visualize_graspability_with_boxes(img_path, detections, grasp_probs_np, save_path=vis_save_path, cmap_save_path=cmap_save_path)
    data_no += 1

    return response


def visualize_graspability_with_boxes(orig_img_path, detections, grasp_probs, save_path="grasp_vis.png", cmap_save_path=None):
    """
    orig_img_path: YOLO inference에 사용된 원본 이미지 경로(str)
    detections: YOLO NMS 결과 (list of [x1, y1, x2, y2, conf, class_id])
    grasp_probs: 각 detection에 대한 graspability 확률 (list or np.ndarray, 0~1)
    save_path: graspability 시각화 이미지 저장 경로
    cmap_save_path: 컬러맵 이미지 저장 경로 (None이면 저장하지 않음)
    """
    import matplotlib.pyplot as plt
    from matplotlib.backends.backend_agg import FigureCanvasAgg as FigureCanvas
    from matplotlib.figure import Figure

    orig_img = np.array(Image.open(orig_img_path).convert("RGB"))
    img = orig_img.copy()

    # 연속적인 컬러맵 (빨강~파랑)
    cmap = matplotlib.cm.get_cmap('jet')  # 0=red, 1=blue

    for det, prob in zip(detections[0], grasp_probs):
        x1, y1, x2, y2, conf, class_id = det
        x1, y1, x2, y2 = map(int, [x1, y1, x2, y2])
        x = (x1 + x2) // 2
        y = (y1 + y2) // 2
        width = 120
        height = 120
        x_new1 = max(0, x - (width // 2))
        y_new1 = max(0, y - (height // 2))
        x_new2 = min(img.shape[1], x + (width // 2))
        y_new2 = min(img.shape[0], y + (height // 2))
        color_float = cmap(float(prob))  # RGBA, 0~1
        color = tuple(int(255 * c) for c in color_float[:3])
        cv2.rectangle(img, (x_new1, y_new1), (x_new2, y_new2), color, 2)
        label = f"G:{prob:.3f}"
        cv2.putText(
            img, label, (x_new1, y_new1 - 10),
            cv2.FONT_HERSHEY_SIMPLEX, 0.7, color, 2
        )

    # grasp_vis 이미지를 저장
    cv2.imwrite(save_path, cv2.cvtColor(img, cv2.COLOR_RGB2BGR))
    print(f"Graspability 시각화 결과 저장: {save_path}")

    # 컬러맵 이미지를 별도 파일로 저장
    if cmap_save_path is not None:
        fig = Figure(figsize=(6, 1))
        canvas = FigureCanvas(fig)
        ax = fig.add_axes([0.05, 0.5, 0.9, 0.3])
        norm = matplotlib.colors.Normalize(vmin=0, vmax=1)
        cb1 = matplotlib.colorbar.ColorbarBase(ax, cmap=cmap, norm=norm, orientation='horizontal')
        cb1.set_label('Graspability (0=red, 1=blue)')
        canvas.draw()
        fig.savefig(cmap_save_path)
        plt.close(fig)
        print(f"컬러맵 저장: {cmap_save_path}")

# 성공률 기록용 변수 추가
success_history = []
success_count = 0
total_count = 0

def handle_client(client_socket):
    global failure_count, request_count, processed_requests
    global success_count, total_count, success_history
    start_time = time.time()
    request_count += 1

    with concurrent_clients:  # Limit concurrent clients
        try:
            message = client_socket.recv(1024).decode("utf-8").strip()
            if "," in message:
                parts = message.split(",")
                if len(parts) == 2 and parts[0].isdigit() and parts[1].isdigit():
                    instance_id = int(parts[0])
                    success = int(parts[1])
                    print(f"Received instance_id: {instance_id}, success: {success}")
                    # === 성공률 누적 ===
                    total_count += 1
                    if success == 1:
                        success_count += 1
                    success_history.append(success_count / total_count if total_count > 0 else 0)
                    # 100회마다 그래프 저장
                    if total_count % 100 == 0:
                        import matplotlib.pyplot as plt
                        save_dir = "figures"
                        os.makedirs(save_dir, exist_ok=True)
                        plt.figure(figsize=(8, 4))
                        plt.plot(success_history, label="Success Rate")
                        plt.xlabel("Attempt")
                        plt.ylabel("Success Rate")
                        plt.title("Cumulative Success Rate")
                        plt.ylim(0, 1)
                        plt.grid(True)
                        plt.legend()
                        plt.tight_layout()
                        plt.savefig(os.path.join(save_dir, "success_rate.png"))
                        plt.close()
                        print(f"[Metric] Success rate graph saved: {os.path.join(save_dir, 'success_rate.png')}")
                else:
                    print(f"Invalid message: {message}")
                    client_socket.close()
                    failure_count += 1
                    return
            elif message.isdigit():
                instance_id = int(message)
                success = None
                print(f"Received instance_id: {instance_id}")
            else:
                print(f"Invalid message: {message}")
                client_socket.close()
                failure_count += 1
                return
            if success is not None:
                pred_dir = "online_data"
                # 파일명 형식: {data_no}_{instance_id}_{best_prob}.png 또는 {data_no}_{instance_id}_{best_prob}_{success}.png
                # 새롭게 바뀐 data_no를 포함한 형식에 맞춰 pattern 인식
                pattern = os.path.join(pred_dir, f"*_{instance_id}_*.png")
                candidates = sorted(glob.glob(pattern), key=os.path.getmtime, reverse=True)
                for fname in candidates:
                    base = os.path.basename(fname)
                    parts = base.replace(".png", "").split("_")
                    # 파일명 형식: {data_no}_{instance_id}_{best_prob}.png (success 없는 파일만 처리)
                    if len(parts) == 3:
                        data_no_str, inst_id_str, best_prob_str = parts
                        new_name = os.path.join(pred_dir, f"{data_no_str}_{inst_id_str}_{best_prob_str}_{success}.png")
                        os.rename(fname, new_name)
                        break

            response = run_graspability_model(instance_id)
            client_socket.sendall(response)
            processed_requests += 1

            pred_dir = "online_data"
            feedback_files = glob.glob(os.path.join(pred_dir, "*_[01].png"))
            # === 32, 64, 96, ... 개가 될 때마다 학습 ===
            # if len(feedback_files) >= 128:
            #     online_learning_from_dir(batch_size=128)

        except Exception as e:
            print(f"Client disconnected: {e}")
            failure_count += 1
        finally:
            client_socket.close()
            response_time = time.time() - start_time
            response_times.append(response_time)
            #log_metrics()


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
concurrent_clients = Semaphore(16)  # Limit to 4 concurrent clients (adjust as needed)
processed_requests = 0
request_count = 0
failure_count = 0
response_times = []

for _ in range(1):
    Thread(target=worker, daemon=True).start()

try:
    while True:
        client_sock, addr = server_socket.accept()
        request_queue.put(client_sock)
except KeyboardInterrupt:
    print("서버를 종료합니다.")
    for _ in range(1):
        request_queue.put(None)
    server_socket.close()
