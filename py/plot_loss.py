import pandas as pd
import matplotlib.pyplot as plt
import argparse
import os

def plot_loss_log(csv_path, window=50, save_path=None):
    """
    loss_log.csv 파일을 읽어 이동평균과 함께 loss 곡선을 시각화합니다.
    """
    if not os.path.exists(csv_path):
        print(f"[오류] 파일을 찾을 수 없습니다: {csv_path}")
        return
    df = pd.read_csv(csv_path)
    # loss 관련 컬럼 자동 탐색
    loss_cols = [col for col in df.columns if 'loss' in col.lower()]
    if not loss_cols:
        print("[오류] loss 관련 컬럼이 없습니다.")
        return
    plt.figure(figsize=(12, 6))
    for col in loss_cols:
        plt.plot(df[col], label=col)
        # 이동평균도 같이 표시
        ma = df[col].rolling(window=window, min_periods=1).mean()
        plt.plot(ma, '--', label=f"{col} (MA{window})")
    plt.xlabel('Step')
    plt.ylabel('Loss')
    plt.title(f'Loss Log (window={window})')
    plt.legend()
    plt.grid(True)
    plt.tight_layout()
    if save_path:
        plt.savefig(save_path)
        print(f"그래프를 저장했습니다: {save_path}")
    plt.show()

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument('--csv', type=str, required=True, help='loss_log.csv 파일 경로')
    parser.add_argument('--window', type=int, default=50, help='이동평균 윈도우 크기')
    parser.add_argument('--save', type=str, default=None, help='그래프 저장 경로 (선택)')
    args = parser.parse_args()
    plot_loss_log(args.csv, window=args.window, save_path=args.save)
