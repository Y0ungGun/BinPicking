import socket
import struct
import torch
import onnxruntime as ort
import numpy as np
import cv2
from PIL import Image
from queue import Queue
import torchvision.transforms as T
import threading
from threading import Thread, Semaphore
import nms
import io
import os
import time

os.chdir("C:/Users/dudrj/unityworkspace/MultiAgent/py")
print(os.getcwd())
# 모델 로드 (한 번만 실행)
onnx_model_path = "best.onnx"
session = ort.InferenceSession(onnx_model_path)

# 이미지 전처리 함수
def preprocess_image(image):
    transform = T.Compose([
        T.Resize(736),
        T.ToTensor()
    ])
    img_tensor = transform(image).unsqueeze(0)
    return img_tensor.numpy()

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

def process_inference(instance_id):
    img_path = os.path.join(IMAGE_PATH, f"image_{instance_id}.png")
    img = Image.open(img_path)
    detections = run_inference(img)
    draw_bounding_boxes(img, detections)

    response = struct.pack('I', len(detections[0]))
    for det in detections[0]:
        response += struct.pack('6f', *det.tolist())
    return response

# 서버 설정
HOST = '127.0.0.1'
PORT = 7779
IMAGE_PATH = "./images"


script_dir = os.path.dirname(os.path.abspath(__file__))
os.chdir(script_dir)
absolute_path = os.path.abspath(IMAGE_PATH)
print("이미지 파일의 절대 경로:", absolute_path)
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

def log_metrics():
    global processed_requests
    avg_response_time = sum(response_times) / len(response_times) if response_times else 0
    failure_rate = (failure_count / request_count) * 100 if request_count > 0 else 0
    gpu_memory = torch.cuda.memory_allocated() / (1024 ** 2) if torch.cuda.is_available() else 0

    print(f"Processed Requests: {processed_requests}")
    print(f"Average Response Time: {avg_response_time:.2f} seconds")
    print(f"Failure Rate: {failure_rate:.2f}%")
    print(f"GPU Memory Usage: {gpu_memory:.2f} MB")

def handle_client(client_socket):
    global failure_count, request_count, processed_requests
    start_time = time.time()
    request_count += 1

    with concurrent_clients:  # Limit concurrent clients
        try:
            message = client_socket.recv(1024).decode("utf-8").strip()
            if not message.isdigit():
                print(f"Invalid message: {message}")
                client_socket.close()
                failure_count += 1
                return

            instance_id = message
            response = process_inference(instance_id)
            client_socket.sendall(response)
            processed_requests += 1

        except Exception as e:
            print(f"Client disconnected: {e}")
            failure_count += 1
        finally:
            client_socket.close()
            response_time = time.time() - start_time
            response_times.append(response_time)
            log_metrics()

def worker():
    while True:
        client_socket = request_queue.get()
        if client_socket is None:
            break
        handle_client(client_socket)

for _ in range(1):
    Thread(target=worker, daemon=True).start()

while True:
    client_sock, addr = server_socket.accept()
    print(f"Connection from {addr}")
    #threading.Thread(target=handle_client, args=(client_sock,)).start()
    request_queue.put(client_sock)

