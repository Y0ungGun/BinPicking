import torch
import torch.nn as nn
import torch.nn.functional as F

class GradReverse(torch.autograd.Function):
    @staticmethod
    def forward(ctx, x, lambd):
        ctx.lambd = lambd
        return x.view_as(x)
    @staticmethod
    def backward(ctx, grad_output):
        return grad_output.neg() * ctx.lambd, None

def grad_reverse(x, lambd=1.0):
    return GradReverse.apply(x, lambd)


import torchvision.models as models

class Encoder(nn.Module):
    def __init__(self, feature_dim=256):
        super().__init__()
        resnet = models.resnet18(weights=models.ResNet18_Weights.IMAGENET1K_V1)
        resnet.conv1 = nn.Conv2d(3, 64, kernel_size=3, stride=1, padding=1, bias=False)
        resnet.maxpool = nn.Identity()
        self.features = nn.Sequential(*list(resnet.children())[:-1])  # (B, 512, 4, 4)
        self.flatten = nn.Flatten()
        self.fc = nn.Linear(512, feature_dim)
        
    def forward(self, x):
        x = self.features(x)
        x = self.flatten(x)
        feature_vec = self.fc(x)
        return feature_vec

class Decoder(nn.Module):
    def __init__(self, feature_dim=256, img_size=120):
        super().__init__()
        self.fc = nn.Linear(feature_dim, 512)
        self.deconv = nn.Sequential(
            nn.Unflatten(1, (512, 1, 1)),
            nn.ConvTranspose2d(512, 256, 4), nn.ReLU(),
            nn.ConvTranspose2d(256, 128, 4, 2, 1), nn.ReLU(),
            nn.ConvTranspose2d(128, 64, 4, 2, 1), nn.ReLU(),
            nn.ConvTranspose2d(64, 3, 4, 2, 1), nn.Sigmoid(),
            nn.AdaptiveAvgPool2d((img_size, img_size))
        )
    def forward(self, x):
        x = self.fc(x)
        x = self.deconv(x)
        return x

class DomainClassifier(nn.Module):
    def __init__(self, feature_dim=256):
        super().__init__()
        self.classifier = nn.Sequential(
            nn.Linear(feature_dim, 100), nn.ReLU(),
            nn.Linear(100, 2)
        )
    def forward(self, x, lambd=1.0):
        x = grad_reverse(x, lambd)
        return self.classifier(x)

class DANN_AE(nn.Module):
    def __init__(self, feature_dim=256, img_size=120):
        super().__init__()
        self.encoder = Encoder(feature_dim=feature_dim)
        self.decoder = Decoder(feature_dim=feature_dim, img_size=img_size)
        self.domain_classifier = DomainClassifier(feature_dim=feature_dim)
    def forward(self, x, lambd=1.0):
        feat = self.encoder(x)
        recon = self.decoder(feat)
        domain_pred = self.domain_classifier(feat, lambd)
        return recon, domain_pred
