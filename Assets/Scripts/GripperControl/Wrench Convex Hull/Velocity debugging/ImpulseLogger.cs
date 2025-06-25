using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ImpulseLogger : MonoBehaviour
{
    public string csvFileName = "CollisionImpulseLog.csv"; // 저장할 CSV 파일 이름
    private StreamWriter writer; // CSV 파일 작성기
    private HashSet<float> loggedTimes; // 이미 기록된 시간을 저장하는 Set
    // Start is called before the first frame update
    void Start()
    {
        writer = new StreamWriter(csvFileName, false); // 파일 덮어쓰기 모드
        writer.WriteLine("Time,ImpulseX,ImpulseY,ImpulseZ,Magnitude,normalX,normalY,normalZ,NormalMag,YPose");

        // 기록된 시간을 저장할 HashSet 초기화
        loggedTimes = new HashSet<float>();
    }
    
    void OnCollisionStay(Collision collision)
    {
        if (writer != null && collision.gameObject.name.Contains("Jaw"))
        {
            float currentTime = Mathf.Round(Time.time * 100f) / 100f; // 시간을 소수점 둘째 자리까지 반올림

            // 이미 기록된 시간인지 확인
            if (!loggedTimes.Contains(currentTime))
            {
                // 충돌 동안의 impulse 가져오기
                Vector3 impulse = collision.impulse;

                // impulse 데이터 기록 (각 축의 값과 크기)
                writer.WriteLine($"{currentTime},{impulse.x},{impulse.y},{impulse.z},{impulse.magnitude},{collision.contacts[0].normal.x},{collision.contacts[0].normal.y},{collision.contacts[0].normal.z},{collision.contacts[0].normal.magnitude},{transform.position.y}");

                // 현재 시간 기록
                loggedTimes.Add(currentTime);
            }
        }
    }

    private void OnApplicationQuit()
    {
        // 프로그램 종료 시 CSV 파일 닫기
        if (writer != null)
        {
            writer.Close();
        }
    }
}
