using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ImpulseLogger : MonoBehaviour
{
    public string csvFileName = "CollisionImpulseLog.csv"; // ������ CSV ���� �̸�
    private StreamWriter writer; // CSV ���� �ۼ���
    private HashSet<float> loggedTimes; // �̹� ��ϵ� �ð��� �����ϴ� Set
    // Start is called before the first frame update
    void Start()
    {
        writer = new StreamWriter(csvFileName, false); // ���� ����� ���
        writer.WriteLine("Time,ImpulseX,ImpulseY,ImpulseZ,Magnitude,normalX,normalY,normalZ,NormalMag,YPose");

        // ��ϵ� �ð��� ������ HashSet �ʱ�ȭ
        loggedTimes = new HashSet<float>();
    }
    
    void OnCollisionStay(Collision collision)
    {
        if (writer != null && collision.gameObject.name.Contains("Jaw"))
        {
            float currentTime = Mathf.Round(Time.time * 100f) / 100f; // �ð��� �Ҽ��� ��° �ڸ����� �ݿø�

            // �̹� ��ϵ� �ð����� Ȯ��
            if (!loggedTimes.Contains(currentTime))
            {
                // �浹 ������ impulse ��������
                Vector3 impulse = collision.impulse;

                // impulse ������ ��� (�� ���� ���� ũ��)
                writer.WriteLine($"{currentTime},{impulse.x},{impulse.y},{impulse.z},{impulse.magnitude},{collision.contacts[0].normal.x},{collision.contacts[0].normal.y},{collision.contacts[0].normal.z},{collision.contacts[0].normal.magnitude},{transform.position.y}");

                // ���� �ð� ���
                loggedTimes.Add(currentTime);
            }
        }
    }

    private void OnApplicationQuit()
    {
        // ���α׷� ���� �� CSV ���� �ݱ�
        if (writer != null)
        {
            writer.Close();
        }
    }
}
