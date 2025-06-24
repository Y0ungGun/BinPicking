using System.IO;

public static class RewardLogger
{
    private static string logPath = "reward_log.csv";
    private static bool headerWritten = false;
    private static int step = 0;

    public static void LogReward(float reward1, float reward2)
    {
        if (!headerWritten && !File.Exists(logPath))
        {
            File.AppendAllText(logPath, "Step,Reward1,Reward2\n");
            headerWritten = true;
        }
        string line = $"{step},{reward1},{reward2}\n";
        File.AppendAllText(logPath, line);
        step++;
    }
}
