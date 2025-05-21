import json
import matplotlib.pyplot as plt

with open('results/MyGrasp_250520_001/run_logs/timers.json', 'r') as f:
    data = json.load(f)

cum_reward = data['gauges']['MyGrasp.Environment.CumulativeReward.mean']['value']
ep_length = data['gauges']['MyGrasp.Environment.EpisodeLength.mean']['value']
reward_per_step = cum_reward / ep_length

print(f"평균 Cumulative Reward: {cum_reward}")
print(f"평균 Episode Length: {ep_length}")
print(f"평균 Reward per Step: {reward_per_step}")