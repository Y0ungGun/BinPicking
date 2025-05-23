import os
import glob
import torch
import torch.nn as nn
from torch.utils.data import Dataset, DataLoader
from torchvision import transforms
from PIL import Image
from torchvision.utils import save_image
from skimage.metrics import structural_similarity as ssim
import numpy as np
from torchvision.models import efficientnet_b0, EfficientNet_B0_Weights
import time

# 1. 데이터셋 정의
class OnlineImageDataset(Dataset):
    def __init__(self, img_dir, img_size=120, num_imgs=None):
        img_dir = "online_data"
        img_files = glob.glob(os.path.join(img_dir, "*.png"))
        print(f"Found {len(img_files)} images in {img_dir}:")
        if num_imgs is not None:
            img_files = img_files[:num_imgs]
        self.img_files = img_files
        self.transform = transforms.Compose([
            transforms.Resize((img_size, img_size)),
            transforms.ToTensor(),
        ])
    def __len__(self):
        return len(self.img_files)
    def __getitem__(self, idx):
        img_path = self.img_files[idx]
        if not os.path.exists(img_path):
            raise FileNotFoundError(f"Image file not found: {img_path}")
        img = Image.open(img_path).convert("RGB")
        return self.transform(img)

# 2. AutoEncoder 정의 (Encoder는 EfficientNet-b0 사용, Decoder만 학습)

class Encoder(nn.Module):
    def __init__(self, feature_dim=128):
        super().__init__()
        efficientnet = efficientnet_b0(weights=EfficientNet_B0_Weights.DEFAULT)
        self.features = efficientnet.features  # Feature extractor
        self.pool = nn.AdaptiveAvgPool2d((1, 1))
        self.flatten = nn.Flatten()
        self.fc = nn.Linear(1280, feature_dim)  # EfficientNet-b0 마지막 채널 수는 1280
        # EfficientNet features는 freeze
        for param in self.features.parameters():
            param.requires_grad = False

    def forward(self, x):
        x = self.features(x)
        x = self.pool(x)
        x = self.flatten(x)
        latent = torch.relu(self.fc(x))
        return latent

class Decoder(nn.Module):
    def __init__(self, feature_dim=128, img_size=120):
        super().__init__()
        self.fc = nn.Linear(feature_dim, 1280)
        self.img_size = img_size
        self.deconv = nn.Sequential(
            nn.Unflatten(1, (1280, 1, 1)),
            nn.ConvTranspose2d(1280, 256, 4),
            nn.ReLU(),
            nn.ConvTranspose2d(256, 64, 4, stride=2),
            nn.ReLU(),
            nn.ConvTranspose2d(64, 3, 4, stride=2),
            nn.Sigmoid(),
            nn.Upsample(size=(img_size, img_size), mode='bilinear')
        )

    def forward(self, z):
        x = torch.relu(self.fc(z))
        x = self.deconv(x)
        return x

class AutoEncoder(nn.Module):
    def __init__(self, feature_dim=128, img_size=120):
        super().__init__()
        self.encoder = Encoder(feature_dim)
        self.decoder = Decoder(feature_dim, img_size)

    def forward(self, x):
        with torch.no_grad():
            z = self.encoder(x)
        out = self.decoder(z)
        return out

def collect_metrics(model, imgs, recon):
    # imgs, recon: torch.Tensor, shape (B, C, H, W), range [0,1]
    criterion = nn.MSELoss(reduction='none')
    mae_criterion = nn.L1Loss(reduction='none')
    batch_size = imgs.size(0)
    losses = []
    ssims = []
    maes = []
    psnrs = []
    for i in range(batch_size):
        loss = criterion(recon[i], imgs[i]).mean().item()
        mae = mae_criterion(recon[i], imgs[i]).mean().item()
        img1 = imgs[i].detach().cpu().numpy().transpose(1, 2, 0)
        img2 = recon[i].detach().cpu().numpy().transpose(1, 2, 0)
        ssim_val = ssim(img1, img2, data_range=1.0, channel_axis=2)
        # PSNR 계산
        mse = np.mean((img1 - img2) ** 2)
        psnr = 20 * np.log10(1.0) - 10 * np.log10(mse) if mse > 0 else 100
        losses.append(loss)
        ssims.append(ssim_val)
        maes.append(mae)
        psnrs.append(psnr)
    return losses, ssims, maes, psnrs

def train_autoencoder_with_metrics(img_dir, epochs=10, batch_size=128, lr=1e-3, img_size=120, num_imgs=1000, device='cuda'):
    dataset = OnlineImageDataset(img_dir, img_size, num_imgs=num_imgs)
    loader = DataLoader(dataset, batch_size=batch_size, shuffle=True)
    model = AutoEncoder(img_size=img_size).to(device)
    # encoder_weights_path = "encoder.pth"
    # if os.path.exists(encoder_weights_path):
    #     print(f"Loading encoder weights from {encoder_weights_path}")
    #     model.encoder.load_state_dict(torch.load(encoder_weights_path, map_location=device))
    optimizer = torch.optim.Adam(model.parameters(), lr=lr)
    criterion = nn.MSELoss()

    sample_dir = os.path.join(os.path.dirname(__file__), "en")
    os.makedirs(sample_dir, exist_ok=True)

    epoch_times = [] 
    all_maes = []
    all_psnrs = []

    for epoch in range(epochs):
        start_epoch = time.time()
        model.train()
        total_loss = 0
        total_ssim = 0
        total_mae = 0
        total_psnr = 0
        count = 0
        for batch_idx, imgs in enumerate(loader):
            imgs = imgs.to(device)
            recon = model(imgs)
            loss = criterion(recon, imgs)
            optimizer.zero_grad()
            loss.backward()
            optimizer.step()
            total_loss += loss.item() * imgs.size(0)

            # Collect metrics for this batch
            batch_losses, batch_ssims, batch_maes, batch_psnrs = collect_metrics(model, imgs, recon)
            total_ssim += sum(batch_ssims)
            total_mae += sum(batch_maes)
            total_psnr += sum(batch_psnrs)
            count += len(batch_ssims)

            if (epoch + 1) % 100 == 0 and batch_idx == 0:
                rand_idx = np.random.randint(0, imgs.size(0))
                orig_img = imgs[rand_idx].detach().cpu()
                recon_img = recon[rand_idx].detach().cpu()
                orig_img_pil = transforms.ToPILImage()(orig_img)
                recon_img_pil = transforms.ToPILImage()(recon_img)
                w, h = orig_img_pil.width, orig_img_pil.height
                combined = Image.new("RGB", (w * 2, h))
                combined.paste(orig_img_pil, (0, 0))
                combined.paste(recon_img_pil, (w, 0))
                save_path = os.path.join(sample_dir, f"compareIMG_{epoch+1}.png")
                combined.save(save_path)

        avg_loss = total_loss / len(dataset)
        avg_ssim = total_ssim / count
        avg_mae = total_mae / count
        avg_psnr = total_psnr / count
        all_losses.append(avg_loss)
        all_ssims.append(avg_ssim)
        all_maes.append(avg_mae)
        all_psnrs.append(avg_psnr)
        end_epoch = time.time()
        epoch_time = end_epoch - start_epoch
        epoch_times.append(epoch_time)
        print(f"Epoch {epoch+1}/{epochs} Loss: {avg_loss:.4f} SSIM: {avg_ssim:.4f} MAE: {avg_mae:.4f} PSNR: {avg_psnr:.2f} Time: {epoch_time:.2f}s")

    torch.save(model.encoder.state_dict(), "encoder_en.pth")
    print("Encoder weights saved to encoder.pth")

    # Save metrics to file
    np.savez("metrics_en.npz",
             loss=np.array(all_losses),
             ssim=np.array(all_ssims),
             mae=np.array(all_maes),
             psnr=np.array(all_psnrs),
             epoch_time=np.array(epoch_times))
    print("Loss, SSIM, MAE, PSNR, and epoch times saved to metrics_en.npz")

if __name__ == "__main__":
    all_losses = []
    all_ssims = []
    import torch
    print("CUDA 사용 가능 여부:", torch.cuda.is_available())
    print("사용 중인 GPU 이름:", torch.cuda.get_device_name(0))
    print("현재 사용 중인 장치:", torch.cuda.current_device())

    start_time = time.time()
    train_autoencoder_with_metrics(
        img_dir="online_data",
        epochs=1000,
        batch_size=256,
        lr=1e-3,
        img_size=120,
        num_imgs=6000,
        device='cuda' if torch.cuda.is_available() else 'cpu'
    )
    end_time = time.time()
    print(f"Training time: {(end_time - start_time)/60:.2f} minutes")