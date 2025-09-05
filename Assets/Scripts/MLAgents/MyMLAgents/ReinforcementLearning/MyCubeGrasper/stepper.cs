using UnityEngine;

public static class stepper
{
    public static int Totalstep = 0;
    public static void AddStep()
    {
        Totalstep += 1;
    }
    public static int GetStep()
    {
        return Totalstep;
    }
}