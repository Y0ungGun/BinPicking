## 서버에서 YOLO v5 불러와 image0 ~ image3000을 통해 target object의 탐지와 bb를 계산하도록 Fine Tuning
import os
import cv2
import shutil
import yaml
from tqdm import tqdm

if not os.path.exists("yolov5"):
    os.system("git clone https://github.com/ultralytics/yolov5.git")

os.system("pip install -r yolov5/requirements.txt")

# 데이터셋 경로 설정
data_root = "D:/ObjectDetection"
images_dir = os.path.join(data_root, "FullImages")
labels_dir = os.path.join(data_root, "YOLOAnnotations")

# 크롭된 이미지 저장 폴더
cropped_images_dir = os.path.join(data_root, "CroppedImages")
cropped_labels_dir = os.path.join(data_root, "CroppedAnnotations")

train_images = os.path.join(data_root, "CroppedImages")
train_labels = os.path.join(data_root, "CroppedAnnotations")
dataset_yaml = {
    "train": "D:/ObjectDetection/images/train",
    "val": "D:/ObjectDetection/images/val",  # 여기서는 검증 데이터도 동일하게 사용
    "nc": 3,  # 클래스 개수 (cube, cylinder, capsule)
    "names": ["cube", "cylinder", "capsule"]
}

with open("dataset.yaml", "w") as f:
    yaml.dump(dataset_yaml, f)

# 학습 실행
os.system(f"python yolov5/train.py --img 720 --batch 16 --epochs 50 --data dataset.yaml --weights yolov5s.pt --device 0")

# ONNX 변환
best_model_path = "yolov5/runs/train/exp/weights/best.pt"
onnx_output_path = "yolov5/best_model.onnx"
os.system(f"python yolov5/export.py --weights {best_model_path} --img 720 --batch 1 --device 0 --include onnx")

print(f"ONNX 모델 저장 완료: {onnx_output_path}")