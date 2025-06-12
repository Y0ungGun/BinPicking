using UnityEngine;

public class Debugging : MonoBehaviour
{
    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }
    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        System.IO.File.AppendAllText("unity_debug_log.txt", logString + "\n" + stackTrace + "\n");
    }
}
