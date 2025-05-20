using UnityEngine;
using System;
using Unity.Robotics.UrdfImporter;

public class ControlTest : MonoBehaviour
{
    public ArticulationBody link1;
    public ArticulationBody link2;
    public ArticulationBody link3;
    public ArticulationBody link4;
    public ArticulationBody link5;
    public ArticulationBody link6;

    private float nextActionTime = 0f; // 다음 동작 시간
    private float interval = 3f; // 3초 간격
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time >= nextActionTime)
        {
            System.Random random = new System.Random(); // Random 클래스 인스턴스 생성
            double[] randomValues = new double[6]; // 6개의 랜덤 값을 저장할 배열 생성

            for (int i = 0; i < 6; i++)
            {
                randomValues[i] = random.NextDouble(); // 0 이상 1 미만의 랜덤 값 생성
                Console.WriteLine($"Random Value {i + 1}: {randomValues[i]}");
            }
            float j1 = (float)randomValues[0] * 2.618f;
            float j2 = (float)randomValues[1] * 2.618f;
            float j3 = (float)randomValues[2] * 2.618f;
            float j4 = (float)randomValues[3] * 2.618f;
            float j5 = (float)randomValues[4] * 2.618f;
            float j6 = (float)randomValues[5] * 2.618f;


            ActionProcess(j1, j2, j3, j4, j5, j6);
            nextActionTime = Time.time + interval; // 다음 동작 시간 설정
        }
    }
    private void ActionProcess(float j1, float j2, float j3, float j4, float j5, float j6)
    {
        ArticulationDrive drive1 = link1.xDrive;
        ArticulationDrive drive2 = link2.xDrive;
        ArticulationDrive drive3 = link3.xDrive;
        ArticulationDrive drive4 = link4.xDrive;
        ArticulationDrive drive5 = link5.xDrive;
        ArticulationDrive drive6 = link6.xDrive;

        drive1.target = j1 * 180 / Mathf.PI;
        drive2.target = j2 * 180 / Mathf.PI;
        drive3.target = j3 * 180 / Mathf.PI;
        drive4.target = j4 * 180 / Mathf.PI;
        drive5.target = j5 * 180 / Mathf.PI;
        drive6.target = j6 * 180 / Mathf.PI;

        link1.xDrive = drive1;
        link2.xDrive = drive2;
        link3.xDrive = drive3;
        link4.xDrive = drive4;
        link5.xDrive = drive5;
        link6.xDrive = drive6;
    }
}
