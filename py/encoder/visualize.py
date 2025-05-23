import numpy as np
import matplotlib.pyplot as plt
import sys
import os

def visualize_multiple_metrics(npz_paths):
    metrics = []
    labels = []
    colors = ['b', 'orange', 'g', 'r', 'c', 'm', 'y', 'k']

    # 곡선 그래프
    fig, axs = plt.subplots(2, 2, figsize=(14, 10))
    for idx, npz_path in enumerate(npz_paths):
        data = np.load(npz_path)
        loss = data["loss"]
        ssim = data["ssim"]
        mae = data["mae"]
        psnr = data["psnr"]
        epoch_time = data["epoch_time"]
        epochs = np.arange(1, len(loss) + 1)
        label = os.path.basename(os.path.dirname(npz_path)) or os.path.basename(npz_path)
        color = colors[idx % len(colors)]

        axs[0, 0].plot(epochs, loss, label=label, color=color)
        axs[0, 1].plot(epochs, ssim, label=label, color=color)
        axs[1, 0].plot(epochs, mae, label=label, color=color)
        axs[1, 1].plot(epochs, psnr, label=label, color=color)

        metrics.append({
            "label": label,
            "final_loss": loss[-1],
            "final_ssim": ssim[-1],
            "final_mae": mae[-1],
            "final_psnr": psnr[-1],
            "total_time": np.sum(epoch_time),
        })
        labels.append(label)

    axs[0, 0].set_title("Loss")
    axs[0, 1].set_title("SSIM")
    axs[1, 0].set_title("MAE")
    axs[1, 1].set_title("PSNR")
    for ax in axs.flat:
        ax.set_xlabel("Epoch")
        ax.legend()
        ax.grid(True)
    plt.tight_layout()
    plt.show()

    # Draw separate bar plots for each metric due to scale differences
    metrics_names = ['Loss', 'SSIM', 'MAE', 'PSNR', 'Time (min)']
    metrics_values = [
        [m["final_loss"] for m in metrics],
        [m["final_ssim"] for m in metrics],
        [m["final_mae"] for m in metrics],
        [m["final_psnr"] for m in metrics],
        [m["total_time"]/60 for m in metrics],  # in minutes
    ]

    fig, axs = plt.subplots(1, 5, figsize=(22, 5))
    for i, (name, values) in enumerate(zip(metrics_names, metrics_values)):
        axs[i].bar(labels, values, color=colors[:len(labels)])
        axs[i].set_title(name)
        axs[i].set_xticklabels(labels, rotation=20)
        axs[i].grid(True, axis='y')

    plt.tight_layout()
    plt.show()

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("사용법: python visualize.py <metrics1.npz> [metrics2.npz] ...")
    else:
        visualize_multiple_metrics(sys.argv[1:])