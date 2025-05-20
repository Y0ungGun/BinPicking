import cv2
import os
from glob import glob
import shutil
import yaml
from tqdm import tqdm


# 데이터셋 경로 설정
data_root = "D:/ObjectDetection"
images_dir = os.path.join(data_root, "FullImages")
labels_dir = os.path.join(data_root, "YOLOAnnotations")

# 크롭된 이미지 저장 폴더
cropped_images_dir = os.path.join(data_root, "images/val")
cropped_labels_dir = os.path.join(data_root, "labels/val")

#os.makedirs(cropped_images_dir, exist_ok=True)
#os.makedirs(cropped_labels_dir, exist_ok=True)

def crop_center(image_path, label_path, output_image_path, output_label_path):
    """ 이미지를 720x720 중앙 기준으로 크롭하고, YOLO 라벨도 변환 """
    img = cv2.imread(image_path)
    if img is None:
        return

    height, width, _ = img.shape
    crop_size = 720

    # 중앙 기준으로 크롭 좌표 설정
    x1 = (width - crop_size) // 2
    y1 = 0  # 상단 고정
    x2 = x1 + crop_size
    y2 = y1 + crop_size

    cropped_img = img[y1:y2, x1:x2]

    # 이미지 저장
    cv2.imwrite(output_image_path, cropped_img)

    # 라벨 변환 (YOLO 형식)
    if os.path.exists(label_path):
        with open(label_path, "r") as f:
            lines = f.readlines()

        new_lines = []
        for line in lines:
            cls, x, y, w, h = map(float, line.strip().split())

            # 절대 좌표로 변환
            x = x * width  # 정규화된 x 좌표 -> 절대 x 좌표
            y = y * height  # 정규화된 y 좌표 -> 절대 y 좌표
            w = w * width  # 정규화된 너비 -> 절대 너비
            h = h * height  # 정규화된 높이 -> 절대 높이

            # 크롭된 좌표계로 이동
            x -= x1
            y -= y1

            # 크롭된 이미지 영역 내에 객체가 포함되는지 확인
            # 객체가 크롭된 이미지 내에 있을 경우에만 처리
            if 0 <= x <= crop_size and 0 <= y <= crop_size:
                # YOLO 형식에 맞게 다시 정규화
                x /= crop_size
                y /= crop_size
                w /= crop_size
                h /= crop_size
                new_lines.append(f"{int(cls)} {x:.6f} {y:.6f} {w:.6f} {h:.6f}\n")

        # new_lines가 비어있지 않으면 파일을 작성
        if new_lines:
            with open(output_label_path, "w") as f:
                f.writelines(new_lines)
        else:
            print(f"⚠️ {output_label_path} - 크롭된 이미지에 객체가 없어서 라벨이 작성되지 않았습니다.")
    else:
        print(f"⚠️ {label_path} 파일이 존재하지 않습니다.")

# 3000장 크롭 수행
for i in tqdm(range(3001, 3301)):
    image_path = os.path.join(images_dir, f"fullImage{i}.png")
    label_path = os.path.join(labels_dir, f"image{i}.txt")
    output_image_path = os.path.join(cropped_images_dir, f"image{i}.png")
    output_label_path = os.path.join(cropped_labels_dir, f"image{i}.txt")

    crop_center(image_path, label_path, output_image_path, output_label_path)

print("✅ 크롭 완료!")
