from PIL import Image
import numpy as np
import os
import torch
import torch.nn as nn
import torchvision.models as models
import onnxruntime as ort

# Example 이미지 경로
EXAMPLE_IMAGE_DIR = "ref_data"

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

def load_grasp_model(device):
    grasp_model = GraspabilityModel(feature_dim=256)
    
    # 가중치 파일 경로
    feature_path = os.path.expanduser(".rot_embedding.pth")
    output_path = os.path.expanduser("./results/MyGrasp_251002/grasp_out.pth")
    
    # 가중치 로드
    if os.path.exists(feature_path):
        feature_weights = torch.load(feature_path, map_location=device)
        if isinstance(feature_weights, dict) and 'features' in feature_weights:
            grasp_model.features.load_state_dict(feature_weights['features'])
            grasp_model.fc.load_state_dict(feature_weights['fc'])
        else:
            grasp_model.load_state_dict(feature_weights, strict=False)

    if os.path.exists(output_path):
        output_weights = torch.load(output_path, map_location=device)
        grasp_model.output.load_state_dict(output_weights)
    
    grasp_model.to(device)
    grasp_model.eval()
    return grasp_model

def load_agent(device):
    # 문자열로 전달된 경우 torch.device 객체로 변환
    if isinstance(device, str):
        device = torch.device(device)

    agent_path = os.path.expanduser("./results/MyGrasp_251002/MyGrasp.onnx")
    
    providers = ['CPUExecutionProvider']
    if torch.cuda.is_available() and device.type == 'cuda':
        providers.insert(0, 'CUDAExecutionProvider')
        
    onnx_session = ort.InferenceSession(agent_path, providers=providers)

    input_info = onnx_session.get_inputs()
    output_info = onnx_session.get_outputs()

    return onnx_session

def zyz_euler_to_rotation_matrix(z1, y, z2):
    """
    ZYZ Euler angles를 rotation matrix로 변환
    
    Args:
        z1, y, z2: ZYZ Euler angles (라디안)
    
    Returns:
        3x3 rotation matrix
    """
    # Z-Y-Z Euler 회전 순서
    cos_z1, sin_z1 = np.cos(z1), np.sin(z1)
    cos_y, sin_y = np.cos(y), np.sin(y)
    cos_z2, sin_z2 = np.cos(z2), np.sin(z2)
    
    # ZYZ rotation matrix
    R = np.array([
        [cos_z1*cos_y*cos_z2 - sin_z1*sin_z2, -cos_z1*cos_y*sin_z2 - sin_z1*cos_z2, cos_z1*sin_y],
        [sin_z1*cos_y*cos_z2 + cos_z1*sin_z2, -sin_z1*cos_y*sin_z2 + cos_z1*cos_z2, sin_z1*sin_y],
        [-sin_y*cos_z2, sin_y*sin_z2, cos_y]
    ])
    
    return R

def visualize_gripper_3d(ax, center, rotation_matrix, gripper_size=0.1):
    """
    3D 그리퍼를 시각화하는 함수
    
    Args:
        ax: matplotlib 3D axis
        center: 그리퍼 중심 좌표 (x, y, z)
        rotation_matrix: 3x3 rotation matrix
        gripper_size: 그리퍼 크기
    """
    # 그리퍼의 기본 형태 정의 (두 개의 jaw)
    jaw_length = gripper_size
    jaw_width = gripper_size * 0.1
    jaw_separation = gripper_size * 0.3
    
    # 기본 그리퍼 좌표 (로컬 좌표계)
    # Jaw 1
    jaw1_start = np.array([-jaw_separation/2, -jaw_length/2, 0])
    jaw1_end = np.array([-jaw_separation/2, jaw_length/2, 0])
    
    # Jaw 2
    jaw2_start = np.array([jaw_separation/2, -jaw_length/2, 0])
    jaw2_end = np.array([jaw_separation/2, jaw_length/2, 0])
    
    # 회전 적용
    jaw1_start_rot = rotation_matrix @ jaw1_start + center
    jaw1_end_rot = rotation_matrix @ jaw1_end + center
    jaw2_start_rot = rotation_matrix @ jaw2_start + center
    jaw2_end_rot = rotation_matrix @ jaw2_end + center
    
    # 3D로 그리기
    ax.plot([jaw1_start_rot[0], jaw1_end_rot[0]], 
            [jaw1_start_rot[1], jaw1_end_rot[1]], 
            [jaw1_start_rot[2], jaw1_end_rot[2]], 
            color='black', linewidth=5, label='Gripper Jaw 1')
    
    ax.plot([jaw2_start_rot[0], jaw2_end_rot[0]], 
            [jaw2_start_rot[1], jaw2_end_rot[1]], 
            [jaw2_start_rot[2], jaw2_end_rot[2]], 
            color='black', linewidth=5, label='Gripper Jaw 2')
    
    # 중심점 표시
    ax.scatter(center[0], center[1], center[2], color='red', s=50, label='Gripper Center')
    
    # 좌표축 표시 (그리퍼 orientation)
    axis_length = gripper_size * 0.5
    
    # X축 (빨간색)
    x_axis = rotation_matrix @ np.array([axis_length, 0, 0]) + center
    ax.plot([center[0], x_axis[0]], [center[1], x_axis[1]], [center[2], x_axis[2]], 
            color='red', linewidth=2, label='X-axis')
    
    # Y축 (초록색)
    y_axis = rotation_matrix @ np.array([0, axis_length, 0]) + center
    ax.plot([center[0], y_axis[0]], [center[1], y_axis[1]], [center[2], y_axis[2]], 
            color='green', linewidth=2, label='Y-axis')
    
    # Z축 (파란색)
    z_axis = rotation_matrix @ np.array([0, 0, axis_length]) + center
    ax.plot([center[0], z_axis[0]], [center[1], z_axis[1]], [center[2], z_axis[2]], 
            color='blue', linewidth=2, label='Z-axis')

def run_grasp_inference(grasp_model, image_path, device):
    input_image = preprocess_image(image_path)
    input_tensor = torch.from_numpy(input_image).float().to(device)
    
    with torch.no_grad():
        grasp_probs, feature_vectors = grasp_model(input_tensor)
    return grasp_probs.cpu().numpy(), feature_vectors.cpu().numpy()

def run_onnx_inference(rl_session, feature_vector):
    """ONNX 모델로 deterministic continuous actions 추론"""
    if rl_session is None:
        return None

    if isinstance(feature_vector, torch.Tensor):
        feature_vector = feature_vector.cpu().numpy()

    if feature_vector.ndim == 1:
        feature_vector = np.expand_dims(feature_vector, axis=0)

    input_dict = {
        'obs_0': feature_vector.astype(np.float32)
    }

    output_names = [output.name for output in rl_session.get_outputs()]
    outputs = rl_session.run(None, input_dict)

    output_dict = dict(zip(output_names, outputs))

    deterministic_continuous_actions = output_dict.get('deterministic_continuous_actions', None)
    actions = deterministic_continuous_actions.squeeze(0) if deterministic_continuous_actions is not None else None

    return actions

# 이미지 전처리 함수 (Pillow와 NumPy 사용)
def preprocess_image(image_path):
    image = Image.open(image_path).convert("RGB")  # 이미지를 RGB로 변환
    image = image.resize((120, 120))  # 모델 입력 크기로 리사이즈
    image = np.array(image).astype(np.float32) / 255.0  # 정규화
    image = np.transpose(image, (2, 0, 1))  # (H, W, C) -> (C, H, W)
    image = np.expand_dims(image, axis=0)  # 배치 차원 추가
    return image

import matplotlib
# matplotlib.use('Agg')  # GUI 백엔드 비활성화 - 주석처리하여 화면에 띄우기
import matplotlib.pyplot as plt
import matplotlib.patches as patches
from mpl_toolkits.mplot3d import Axes3D
import math
import numpy as np

def visualize_gripper(ax, image_center, yaw_angle, gripper_size=30):
    """
    그리퍼를 시각화하는 함수
    
    Args:
        ax: matplotlib axis
        image_center: 이미지 중심 좌표 (x, y)
        yaw_angle: 그리퍼의 yaw 각도 (라디안)
        gripper_size: 그리퍼 크기
    """
    x_center, y_center = image_center
    
    # 그리퍼 jaw의 길이와 너비 (6배로 증가)
    jaw_length = gripper_size
    jaw_width = 10  # 두께 2배로 증가
    jaw_separation = 90  # 두 jaw 사이의 간격 (6배로 증가)
    
    # yaw 각도만큼 시계반대방향으로 회전 (음수 적용)
    cos_yaw = math.cos(-yaw_angle)
    sin_yaw = math.sin(-yaw_angle)
    
    # 첫 번째 jaw (왼쪽)
    jaw1_start_x = x_center - jaw_separation/2 * cos_yaw - jaw_length/2 * sin_yaw
    jaw1_start_y = y_center - jaw_separation/2 * sin_yaw + jaw_length/2 * cos_yaw
    jaw1_end_x = x_center - jaw_separation/2 * cos_yaw + jaw_length/2 * sin_yaw
    jaw1_end_y = y_center - jaw_separation/2 * sin_yaw - jaw_length/2 * cos_yaw
    
    # 두 번째 jaw (오른쪽)
    jaw2_start_x = x_center + jaw_separation/2 * cos_yaw - jaw_length/2 * sin_yaw
    jaw2_start_y = y_center + jaw_separation/2 * sin_yaw + jaw_length/2 * cos_yaw
    jaw2_end_x = x_center + jaw_separation/2 * cos_yaw + jaw_length/2 * sin_yaw
    jaw2_end_y = y_center + jaw_separation/2 * sin_yaw - jaw_length/2 * cos_yaw
    
    # jaw 그리기 (검정색으로 변경)
    ax.plot([jaw1_start_x, jaw1_end_x], [jaw1_start_y, jaw1_end_y], 
            color='black', linewidth=jaw_width, label='Gripper Jaw')
    ax.plot([jaw2_start_x, jaw2_end_x], [jaw2_start_y, jaw2_end_y], 
            color='black', linewidth=jaw_width)
    
    # 그리퍼 중심점 표시
    ax.plot(x_center, y_center, 'ro', markersize=8, label='Gripper Center')

def visualize_results(image_path, graspability, action):
    """
    이미지와 추론 결과를 2D와 3D로 시각화하는 함수
    
    Args:
        image_path: 이미지 파일 경로
        graspability: 그래스퍼빌리티 값
        action: ONNX 모델의 액션 출력 (6개 값)
    """
    # 이미지 로드
    image = Image.open(image_path).convert("RGB")
    image_array = np.array(image)
    
    # Figure 생성 (2D와 3D subplot)
    fig = plt.figure(figsize=(16, 8))
    
    # 2D 시각화 (왼쪽)
    ax1 = fig.add_subplot(121)
    ax1.imshow(image_array)
    ax1.set_title(f"2D View\nImage: {os.path.basename(image_path)}\nGraspability: {graspability[0]:.4f}")
    
    # 3D 시각화 (오른쪽)
    ax2 = fig.add_subplot(122, projection='3d')
    ax2.set_title("3D Gripper Orientation (ZYZ Euler)")
    
    if action is not None:
        # 액션 값 추출 (a3, a4, a5 = ZYZ Euler angles)
        a3, a4, a5 = action[3], action[4], action[5]
        
        # 2D 그리퍼 시각화
        image_center = (image_array.shape[1] // 2, image_array.shape[0] // 2)
        yaw_angle = a4  # 2D에서는 a4만 사용 (yaw)
        visualize_gripper(ax1, image_center, yaw_angle)
        
        # 3D 그리퍼 시각화
        # ZYZ Euler angles를 rotation matrix로 변환
        rotation_matrix = zyz_euler_to_rotation_matrix(a3, a4, a5)
        gripper_center = np.array([0, 0, 0])  # 3D 공간의 원점
        visualize_gripper_3d(ax2, gripper_center, rotation_matrix, gripper_size=1.0)
        
        # 3D 축 설정
        ax2.set_xlim([-1, 1])
        ax2.set_ylim([-1, 1])
        ax2.set_zlim([-1, 1])
        ax2.set_xlabel('X')
        ax2.set_ylabel('Y')
        ax2.set_zlabel('Z')
        
        # 텍스트 정보 표시 (2D 이미지에)
        ax1.text(10, 30, f"ZYZ Euler [a3, a4, a5]: [{a3:.3f}, {a4:.3f}, {a5:.3f}]", 
               bbox=dict(boxstyle="round,pad=0.3", facecolor="lightblue", alpha=0.7),
               fontsize=10, color='black')
        ax1.text(10, 60, f"Degrees: [{math.degrees(a3):.1f}°, {math.degrees(a4):.1f}°, {math.degrees(a5):.1f}°]", 
               bbox=dict(boxstyle="round,pad=0.3", facecolor="lightgreen", alpha=0.7),
               fontsize=10, color='black')
        
        # a3, a5가 0.1보다 큰 경우 경고 메시지 추가
        if abs(a3) > 0.1 or abs(a5) > 0.1:
            ax1.text(10, 90, "Warning: a3 or a5 > 0.1", 
                   bbox=dict(boxstyle="round,pad=0.3", facecolor="orange", alpha=0.7),
                   fontsize=10, color='black')
    else:
        ax1.text(10, 30, "No action available", 
               bbox=dict(boxstyle="round,pad=0.3", facecolor="red", alpha=0.7),
               fontsize=12, color='white')
        ax2.text(0, 0, 0, "No action available", fontsize=12)
    
    ax1.axis('off')
    plt.tight_layout()
    
    # 결과 이미지 저장하지 않고 화면에 표시
    plt.show()
    print(f"Visualization displayed for: {os.path.basename(image_path)}")

# Example 이미지 처리 및 시각화
device = torch.device('cpu')
graspability_model = load_grasp_model(device)
rl_session = load_agent(device)

for image_name in os.listdir(EXAMPLE_IMAGE_DIR):
    if image_name.lower().endswith(('.png', '.jpg', '.jpeg')):
        image_path = os.path.join(EXAMPLE_IMAGE_DIR, image_name)
        
        # 추론 실행
        graspability, feature_vector = run_grasp_inference(graspability_model, image_path, device)
        action = run_onnx_inference(rl_session, feature_vector)
        
        # 결과 출력
        print(f"Image: {image_name}")
        print(f"Graspability: {graspability}")
        if action is not None:
            print(f"Action: {action}")
            print(f"Key actions [a3, a4, a5]: [{action[3]:.3f}, {action[4]:.3f}, {action[5]:.3f}]")
        print("-" * 50)
        
        # 시각화
        visualize_results(image_path, graspability, action)