using UnityEngine;

public class TimeScale : MonoBehaviour
{
    public float timeScale = 1.0f; // 기본 시간 흐름 속도
    void Start()
    {
        Time.timeScale = timeScale; // 시간 흐름을 2배로
    }
}
