using UnityEngine;

public class TimeScale : MonoBehaviour
{
    public float timeScale = 1.0f; // �⺻ �ð� �帧 �ӵ�
    void Start()
    {
        Time.timeScale = timeScale; // �ð� �帧�� 2���
    }
}
