import pandas as pd
import matplotlib.pyplot as plt
import argparse
import os

def plot_each_reward(exp_path, file_name, window_size):
    """gws와 success reward의 이동 평균을 플롯합니다."""
    file_path = os.path.join(exp_path, file_name)
    try:
        # header=None으로 헤더가 없음을 명시하고, names로 열 이름을 직접 지정합니다.
        df = pd.read_csv(file_path, header=None, names=['timestep', 'gws', 'success'])

        # --- 데이터 정제 ---
        # 'gws'와 'success' 열을 숫자로 변환하고, 변환할 수 없는 값은 NaN으로 처리합니다.
        df['gws'] = pd.to_numeric(df['gws'], errors='coerce')
        df['success'] = pd.to_numeric(df['success'], errors='coerce')

        # NaN 값을 포함한 행을 제거하여 순수한 숫자 데이터만 남깁니다.
        df.dropna(subset=['gws', 'success'], inplace=True)
        df.reset_index(drop=True, inplace=True)

    except FileNotFoundError:
        print(f"오류: '{file_path}'에서 파일을 찾을 수 없습니다.")
        print("경로와 파일명을 올바르게 설정했는지 확인해주세요.")
        return

    # 데이터가 비어있는 경우 처리
    if df.empty:
        print(f"경고: '{file_path}'에 유효한 데이터가 없습니다.")
        return

    x_axis = range(len(df))
    df['gws_ma'] = df['gws'].rolling(window=window_size, min_periods=1).mean()
    df['success_ma'] = df['success'].rolling(window=window_size, min_periods=1).mean()

    # 최종 값 계산
    final_gws = df['gws_ma'].iloc[-1]
    final_success = df['success_ma'].iloc[-1]

    plt.figure(figsize=(15, 6))

    # GWS Reward with Moving Average
    plt.subplot(1, 2, 1)
    gws_label = f'GWS MA (Final: {final_gws:.4f})\nWindow={window_size}'
    plt.plot(x_axis, df['gws_ma'], label=gws_label, color='blue')
    plt.xlabel('Data Index')
    plt.ylabel('GWS Reward')
    plt.title('GWS Reward (Moving Average)')
    plt.grid(True)
    plt.legend()

    # Success Reward with Moving Average
    plt.subplot(1, 2, 2)
    success_label = f'Success Rate MA (Final: {final_success:.4f})\nWindow={window_size}'
    plt.plot(x_axis, df['success_ma'], label=success_label, color='green')
    plt.xlabel('Data Index')
    plt.ylabel('Success Rate')
    plt.title('Success Rate (Moving Average)')
    plt.grid(True)
    plt.legend()

    plt.suptitle(f'Individual Reward Trends ({exp_path})')
    plt.tight_layout(rect=[0, 0.03, 1, 0.95])
    plt.show()

def plot_total_reward(exp_path):
    """CumulativeReward와 EpisodeLength를 기반으로 전체 평균 보상을 플롯합니다."""
    reward_file = os.path.join(exp_path, "CumulativeReward.csv")
    length_file = os.path.join(exp_path, "EpisodeLength.csv")

    try:
        reward_df = pd.read_csv(reward_file)
        length_df = pd.read_csv(length_file)
    except FileNotFoundError as e:
        print(f"오류: 파일을 찾을 수 없습니다. ({e.filename})")
        print(f"'{exp_path}' 경로에 필요한 파일이 있는지 확인해주세요.")
        return

    merged = pd.merge(reward_df, length_df, on="Step", suffixes=('_reward', '_length'))
    merged['RewardPerStep'] = merged['Value_reward'] / merged['Value_length']

    plt.figure(figsize=(10, 5))
    plt.plot(merged['Step'], merged['RewardPerStep'])
    plt.xlabel('Step')
    plt.ylabel('CumulativeReward / EpisodeLength')
    plt.title(f'Overall Average Success Rate Over Steps ({exp_path})')
    plt.grid(True)
    plt.show()

if __name__ == "__main__":
    # --- 설정 ---
    # 각 모드에 맞는 기본 실험 폴더 경로를 지정해주세요.
    # --path나 --exp_name 인자가 없을 경우 이 경로가 사용됩니다.
    DEFAULT_EACH_EXP_PATH = "results/MyGrasp_250625/"
    DEFAULT_TOTAL_EXP_PATH = "results/MyGrasp_250614/"

    # --- 인자 파서 ---
    parser = argparse.ArgumentParser(description='ML-Agents 학습 보상 그래프를 플롯합니다.')
    parser.add_argument('plot_type', type=str, choices=['total', 'each'],
                        help='플롯 종류 선택: "total" (전체 평균 보상), "each" (개별 보상 추세)')
    parser.add_argument('--path', type=str, help='(선택) 사용할 실험 폴더의 전체 경로를 직접 지정합니다.')
    parser.add_argument('--exp_name', type=str, help='(선택) 실험 이름(예: 240625)을 지정하여 경로를 생성합니다. (results/MyGrasp_{exp_name})')
    parser.add_argument('--window', type=int, default=100, help='("each" 모드용) 이동 평균 윈도우 크기')

    args = parser.parse_args()

    # --- 경로 결정 로직 (우선순위: path > exp_name > default) ---
    exp_path = None
    if args.path:
        exp_path = args.path
    elif args.exp_name:
        exp_path = f"results/MyGrasp_{args.exp_name}/"

    # --- 메인 로직 ---
    if args.plot_type == 'each':
        # 경로가 지정되지 않았으면 기본 경로 사용
        if not exp_path:
            exp_path = DEFAULT_EACH_EXP_PATH
        plot_each_reward(exp_path, "reward_log.csv", args.window)
    elif args.plot_type == 'total':
        # 경로가 지정되지 않았으면 기본 경로 사용
        if not exp_path:
            exp_path = DEFAULT_TOTAL_EXP_PATH
        plot_total_reward(exp_path)

