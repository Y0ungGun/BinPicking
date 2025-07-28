import cv2
import nms
import numpy as np
import onnxruntime as ort
import torch
import torchvision.models as models
import torch.nn as nn
import torch
import os


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
    

def visualize_graspability(image_path, output_path="graspability_result.png", patch_size=120):
    """
    이미지에서 YOLO로 검출된 각 Bounding Box를 기준으로 Crop하여 N개의 오브젝트에 대해 graspability prediction을 진행하고,
    각 결과값을 bounding box와 함께 덧그려 하나의 이미지로 시각화합니다.
    """
    image = cv2.imread(image_path)
    if image is None:
        print(f"이미지를 불러올 수 없습니다: {image_path}")
        return
    img_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)

    detections = run_inference(img_rgb)
    patches = []
    bboxes = []
    for det in detections[0]:
        x1, y1, x2, y2, conf, class_id = det[:6]
        x1, y1, x2, y2 = int(x1), int(y1), int(x2), int(y2)
        xc = (x1 + x2) // 2
        yc = (y1 + y2) // 2
        w, h = patch_size, patch_size
        px1 = max(0, xc - w // 2)
        py1 = max(0, yc - h // 2)
        px2 = min(image.shape[1], xc + w // 2)
        py2 = min(image.shape[0], yc + h // 2)
        if px2 <= px1 or py2 <= py1:
            continue
        patch = image[py1:py2, px1:px2].copy()
        if patch.size == 0:
            continue
        patches.append(patch)
        bboxes.append((px1, py1, px2, py2))

    if not patches:
        print("검출된 객체가 없습니다.")
        return

    # Graspability prediction
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    grasp_model = GraspabilityModel().to(device)
    grasp_model.eval()
    # (필요시 가중치 로드)
    # 입력~출력 직전까지의 가중치 로드
    feature_ckpt_path = os.path.join(os.path.dirname(__file__), "grasp_model.pth")
    if os.path.exists(feature_ckpt_path):
        state_dict = torch.load(feature_ckpt_path, map_location=device)
        # 출력층 파라미터는 제외하고 로드
        state_dict = {k: v for k, v in state_dict.items() if not k.startswith("output.")}
        grasp_model.load_state_dict(state_dict, strict=False)
        print(f"Feature extractor weights loaded from {feature_ckpt_path}")

    # 출력층 가중치 로드
    output_ckpt_path = os.path.join(os.path.dirname(__file__), "results", "MyGrasp_250625", "grasp_out.pth")
    if os.path.exists(output_ckpt_path):
        output_state = torch.load(output_ckpt_path, map_location=device)
        grasp_model.output.load_state_dict(output_state)
        print(f"Output layer weights loaded from {output_ckpt_path}")

    # Patch 전처리 및 배치
    patch_tensors = []
    for patch in patches:
        patch = cv2.cvtColor(patch, cv2.COLOR_BGR2RGB)
        patch = cv2.resize(patch, (patch_size, patch_size))
        patch = torch.from_numpy(patch).permute(2, 0, 1).float() / 255.0
        patch_tensors.append(patch)
    batch = torch.stack(patch_tensors).to(device)

    with torch.no_grad():
        grasp_probs, _ = grasp_model(batch)
    grasp_probs = grasp_probs.cpu().numpy()

    # 원본 이미지에 bounding box와 graspability 값 덧그리기
    vis_img = image.copy()
    for i, (bbox, prob) in enumerate(zip(bboxes, grasp_probs)):
        px1, py1, px2, py2 = bbox
        color = (0, 165, 255) if prob > 0.7 else (0, 255, 0) if prob > 0.4 else (255, 0, 0)
        cv2.rectangle(vis_img, (px1, py1), (px2, py2), color, 2)
        label = f"G:{prob:.3f}"
        cv2.putText(vis_img, label, (px1, py1-10), cv2.FONT_HERSHEY_SIMPLEX, 0.7, color, 2)

    cv2.imwrite(output_path, vis_img)
    print(f"Graspability 결과 이미지를 {output_path}로 저장했습니다.")



onnx_model_path = "best.onnx"

session = ort.InferenceSession(onnx_model_path, providers=['CUDAExecutionProvider']) 
def preprocess_image(image):
    img = cv2.resize(image, (736, 736))
    img = img.astype(np.float32) / 255.0
    img = np.transpose(img, (2, 0, 1))
    return img[np.newaxis, :]

def run_inference(img):
    img_array = preprocess_image(img)
    input_name = session.get_inputs()[0].name
    output_name = session.get_outputs()[0].name
    raw_output = session.run([output_name], {input_name: img_array.astype(np.float32)})
    output_data = raw_output[0].squeeze(0)
    
    conf_thres = 0.9
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


def visualize_cropped_objects(image_path, output_path="patch_objects.png", patch_size=120):
    """
    이미지에서 YOLO로 검출된 각 Bounding Box를 기준으로 Crop하여 N개의 오브젝트를 패치 형태로 시각화하고 하나의 PNG로 저장합니다.
    """
    image = cv2.imread(image_path)
    if image is None:
        print(f"이미지를 불러올 수 없습니다: {image_path}")
        return
    img_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)

    detections = run_inference(img_rgb)
    patches = []
    for det in detections[0]:
        x1, y1, x2, y2, conf, class_id = det[:6]
        x1, y1, x2, y2 = int(x1), int(y1), int(x2), int(y2)
        # 중심 기준 patch crop
        xc = (x1 + x2) // 2
        yc = (y1 + y2) // 2
        w, h = patch_size, patch_size
        px1 = max(0, xc - w // 2)
        py1 = max(0, yc - h // 2)
        px2 = min(image.shape[1], xc + w // 2)
        py2 = min(image.shape[0], yc + h // 2)
        patch = image[py1:py2, px1:px2].copy()
        patches.append(patch)

    if not patches:
        print("검출된 객체가 없습니다.")
        return

    # 패치들을 2D 그리드로 이어붙이기
    margin = 10
    patch_h = patch_size
    patch_w = patch_size
    n = len(patches)
    # 그리드 행/열 계산 (최대한 정사각형에 가깝게)
    grid_cols = int(np.ceil(np.sqrt(n)))
    grid_rows = int(np.ceil(n / grid_cols))
    canvas_w = grid_cols * patch_w + (grid_cols-1)*margin
    canvas_h = grid_rows * patch_h + (grid_rows-1)*margin
    canvas = np.ones((canvas_h, canvas_w, 3), dtype=np.uint8) * 255
    for idx, patch in enumerate(patches):
        patch = cv2.resize(patch, (patch_w, patch_h))
        row = idx // grid_cols
        col = idx % grid_cols
        x_offset = col * (patch_w + margin)
        y_offset = row * (patch_h + margin)
        canvas[y_offset:y_offset+patch_h, x_offset:x_offset+patch_w] = patch

    cv2.imwrite(output_path, canvas)
    print(f"패치 이미지 {n}개를 {output_path}로 저장했습니다. (그리드: {grid_rows}x{grid_cols})")

if __name__ == "__main__":
    img_path = "image3.png"
    output_path = "YoloImage.png"

    visualize_cropped_objects(img_path, output_path)
    visualize_graspability(img_path, output_path="GraspabilityImage.png", patch_size=120)