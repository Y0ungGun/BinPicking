import os
import numpy as np
import torch
import torch.nn as nn
import torch.optim as optim
import torchvision.transforms as transforms
import matplotlib.pyplot as plt
import torch.nn.functional as F
from torch.utils.data import Dataset, DataLoader, random_split
from PIL import Image
from torchvision.models import VGG16_Weights
from torch.optim.lr_scheduler import ReduceLROnPlateau
import torchvision.models as models
import random

def train(model, train_loader, optimizer, criterion):
    model.train()
    running_loss = 0.0
    for inputs in train_loader:  # 이미지와 레이블을 받음
        inputs = inputs.to(device)
        
        optimizer.zero_grad()
        _, outputs = model(inputs)
        
        loss = criterion(outputs, inputs)  # 출력과 입력 간의 차이를 최소화
        loss.backward()
        optimizer.step()
        
        running_loss += loss.item()
    return running_loss / len(train_loader)

def test(model, test_loader, criterion, epoch):
    model.eval()
    running_loss = 0.0
    with torch.no_grad():
        for inputs in test_loader:  # 이미지와 레이블을 받음
            inputs = inputs.to(device)
            
            _, outputs = model(inputs)
            loss = criterion(outputs, inputs)
            
            running_loss += loss.item()

            # 10번마다 원본 이미지와 reconstruction 이미지 저장
            if (epoch+1) % 1 == 0:
                save_images(inputs, outputs, epoch+1)
                
    return running_loss / len(test_loader)

def save_images(inputs, outputs, epoch):
    inputs = inputs.cpu().numpy()
    outputs = outputs.cpu().numpy()
    print(inputs.shape)
    # random_idx = random.randint(0, inputs.shape[0] - 1)
    # fig, axes = plt.subplots(1, 2)
    # axes[0].imshow(np.transpose(inputs[random_idx], (1, 2, 0)))  # 원본 이미지
    # axes[0].set_title('Original')
    # axes[1].imshow(np.transpose(outputs[0], (1, 2, 0)))  # reconstruction 이미지
    # axes[1].set_title('Reconstructed')
    
    # 이미지 저장
    plt.savefig(f"reconstructed_epoch_{epoch}.png")
    plt.close()


class CustomImageDataset(Dataset):
    def __init__(self, image_dir, transform=None):
        self.image_dir = image_dir
        self.transform = transform
        self.image_names = [f for f in os.listdir(image_dir) if f.endswith('.jpg') or f.endswith('.png')]  # 이미지 파일 확장자 확인

    def __len__(self):
        return len(self.image_names)

    def __getitem__(self, idx):
        img_name = os.path.join(self.image_dir, self.image_names[idx])
        image = Image.open(img_name).convert('RGB')
        
        if self.transform:
            image = self.transform(image)
        
        return image

class PerceptualLoss(nn.Module):
    def __init__(self, feature_layer=29):  # VGG16의 마지막 Conv 레이어
        super(PerceptualLoss, self).__init__()
        
        # VGG16 모델 로드 (사전 학습된 가중치 사용)
        vgg = models.vgg16(weights=VGG16_Weights.DEFAULT).features
        
        # 원하는 중간 계층까지 자르기 (여기서는 '29번째' 레이어까지)
        self.vgg = nn.Sequential(*vgg[:feature_layer]).eval()
        
        # VGG16은 Gradients가 필요 없으므로, 학습되지 않도록 설정
        for param in self.vgg.parameters():
            param.requires_grad = False
    
    def forward(self, x, y):
        # x, y는 모델 출력과 실제 이미지
        x_feat = self.vgg(x)
        y_feat = self.vgg(y)
        
        # L2 Norm을 사용하여 특징 맵 간의 차이 계산
        loss = F.mse_loss(x_feat, y_feat)
        return loss

class AutoEncoder(nn.Module):
    def __init__(self):
        super(AutoEncoder, self).__init__()
        
        # Encoder
        self.encoder = nn.Sequential(
            nn.Conv2d(3, 32, kernel_size=4, stride=2, padding=1),  # (128, 128, 32)
            nn.ReLU(),
            nn.BatchNorm2d(32),
            nn.Conv2d(32, 64, kernel_size=4, stride=2, padding=1),  # (64, 64, 64)
            nn.ReLU(),
            nn.BatchNorm2d(64),
            nn.Conv2d(64, 128, kernel_size=4, stride=2, padding=1), # (32, 32, 128)
            nn.ReLU(),
            nn.BatchNorm2d(128),
            nn.Conv2d(128, 256, kernel_size=4, stride=2, padding=1),# (16, 16, 256)
            nn.ReLU(),
            nn.BatchNorm2d(256),
            nn.Flatten(), # (16*16*256) = 65536
            nn.Linear(65536, 1024), # Feature Vector (1024)
            nn.Dropout(0.5)
        )
        
        # Decoder
        self.decoder = nn.Sequential(
            nn.Linear(1024, 65536),
            nn.ReLU(),
            nn.BatchNorm1d(65536),
            nn.Dropout(0.5),
            nn.Unflatten(1, (256, 16, 16)),
            nn.ConvTranspose2d(256, 128, kernel_size=4, stride=2, padding=1), # (32, 32, 128)
            nn.ReLU(),
            nn.BatchNorm2d(128),
            nn.ConvTranspose2d(128, 64, kernel_size=4, stride=2, padding=1), # (64, 64, 64)
            nn.ReLU(),
            nn.BatchNorm2d(64),
            nn.ConvTranspose2d(64, 32, kernel_size=4, stride=2, padding=1),  # (128, 128, 32)
            nn.ReLU(),
            nn.BatchNorm2d(32),
            nn.ConvTranspose2d(32, 3, kernel_size=4, stride=2, padding=1),   # (256, 256, 3)
            nn.Sigmoid() # Normalize to [0, 1]
        )
    
    def forward(self, x):
        encoded = self.encoder(x)
        decoded = self.decoder(encoded)
        return encoded, decoded

transform = transforms.Compose([
    transforms.Resize((256, 256)),  # 이미지 크기 조정
    transforms.ToTensor(),          # 텐서로 변환
])

image_dir = 'D:/ImageData/ResizeImages1212'  # 이미지 파일 경로
dataset = CustomImageDataset(image_dir, transform)
dataloader = DataLoader(dataset, batch_size=32, shuffle=True)

train_size = int(0.8 * len(dataset))  # 80%는 훈련 데이터
test_size = len(dataset) - train_size  # 나머지 20%는 테스트 데이터
train_dataset, test_dataset = random_split(dataset, [train_size, test_size])

# DataLoader 생성 (배치 처리)
train_loader = DataLoader(train_dataset, batch_size=32, shuffle=True)
test_loader = DataLoader(test_dataset, batch_size=32, shuffle=False)

device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
print(torch.cuda.is_available())
# 모델 초기화
model = AutoEncoder().to(device)
criterion = nn.MSELoss()
## criterion = PerceptualLoss().to(device)
optimizer = optim.Adam(model.parameters(), lr=1e-3)
scheduler = ReduceLROnPlateau(optimizer, 'min', patience=5, factor=0.1)
print(next(model.parameters()).device)

num_epochs = 1000
for epoch in range(num_epochs):
    test_loss = test(model, test_loader, criterion, epoch)
    train_loss = train(model, train_loader, optimizer, criterion)
    test_loss = test(model, test_loader, criterion, epoch)
    
    scheduler.step(test_loss)
    
    print(f"Epoch [{epoch+1}/{num_epochs}]")
    print(f"Train Loss: {train_loss:.4f}, Test Loss: {test_loss:.4f}")