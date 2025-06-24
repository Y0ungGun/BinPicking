import pandas as pd
import matplotlib.pyplot as plt

# 1. CSV 파일 불러오기
reward_df = pd.read_csv("py/results/MyGrasp_250614/CumulativeReward.csv")
length_df = pd.read_csv("py/results/MyGrasp_250614/EpisodeLength.csv")

# 2. step 기준으로 merge (혹은 wall_time 기준)
merged = pd.merge(reward_df, length_df, on="Step", suffixes=('_reward', '_length'))

# 3. 값 계산
merged['RewardPerStep'] = merged['Value_reward'] / merged['Value_length']

# 4. Plot
plt.figure(figsize=(10,5))
plt.plot(merged['Step'], merged['RewardPerStep'])
plt.xlabel('Step')
plt.ylabel('CumulativeReward / EpisodeLength')
plt.title('Success Rate Over Steps')
plt.grid(True)
plt.show()