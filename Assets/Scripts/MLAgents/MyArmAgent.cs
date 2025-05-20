using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Linq;
using DSRRobotControl;
using Random = UnityEngine.Random;
using Unity.Sentis;

public class MyArmAgent : Agent
{
    public GameObject target;
    public Transform EndEffector;
    public ArticulationBody link1;
    public ArticulationBody link2;
    public ArticulationBody link3;
    public ArticulationBody link4;
    public ArticulationBody link5;
    public ArticulationBody link6;

    private bool isActionInProgress = false; // 행동이 진행 중인지 여부
    private bool isFirstAction = true;
    private Movel Movel;
    private CommandList commandList;
    private float[] _observation;
    private float _reward;

    private Vector3 positionRangeMax;
    private Vector3 positionRangeMin;
    
    void Start()
    {
        positionRangeMax = GameObject.Find("Corner_max").transform.position;
        positionRangeMin = GameObject.Find("Corner_min").transform.position;
    }
    public bool GetisActionInProgress()
    {    return isActionInProgress; }
    public float[] GetObservation()
    {
        return _observation;
    }
    public float GetReward()
    {
        return _reward;
    }
    public override void OnEpisodeBegin()
    {
        isFirstAction = true;
        RandomizeTargetPosition();
        isActionInProgress = true;
        StartCoroutine(StartwithDelay());
    }

    private IEnumerator StartwithDelay()
    {
        StartwithJointValue();
        yield return new WaitForSeconds(0.5f);
        isActionInProgress = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 관찰 데이터 수집 (예: 로봇 위치, 목표 위치, 속도 등)
        //sensor.AddObservation(EndEffector.position);
        sensor.AddObservation(target.transform.position);
        _observation = new float[] { target.transform.position.x, target.transform.position.y, target.transform.position.z };
    }
    public void DeterministicActionReceived(ActionBuffers actions)
    {
        isActionInProgress = true;
        StartCoroutine(PerformAction(actions));
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        isActionInProgress = true;
        if (!isFirstAction)
        {
            StartCoroutine(PerformAction(actions));

        }
        else
        {
            isFirstAction =false;
        }
    }
    
    private IEnumerator PerformAction(ActionBuffers actions)
    {
        //Debug.Log($"Action: {actions.ContinuousActions[0]}, {actions.ContinuousActions[1]}, {actions.ContinuousActions[2]}");
        float[] MovelPosition = { actions.ContinuousActions[0] * 0.15f + 0.45f, actions.ContinuousActions[1] * 0.2f, actions.ContinuousActions[2] * 0.1f + 0.365f, 0, 3.141592f, 0 };
        float[] MovelVel = { 0.0f, 0.0f };
        float[] MovelAcc = { 0.0f, 0.0f };
        Movel command = new Movel() {
            command = "Movel",
            desiredPosition = MovelPosition,
            velocity = MovelVel,
            acceleration = MovelAcc,
            time = 2.0f,
        };
        List<Command> commands = new List<Command> { command };
        commandList = new CommandList();
        commandList.commands = commands;
        List<double> jointArr = commandList.ExecuteCommands(link6, link1);
        //StartCoroutine(MoveRobot(jointArr, commandList, CommandList.frames));
        for (int i = 0; i < jointArr.Count / 6; i++)
        {
            SetEachJointPositions(jointArr, i);
            yield return new WaitForSeconds(0.05f);
        }
        yield return new WaitForSeconds(0.5f);
        isActionInProgress = false;
        EvaluateReward();
        EndEpisode();
    }

    private void EvaluateReward()
    {
        float distanceToTarget = Vector3.Distance(EndEffector.position, target.transform.position);
        if (distanceToTarget < 0.1f)
        {
            SetReward(5.0f); // 목표에 도달하면 보상
            _reward = 5.0f;
        }
        else
        {
            SetReward(-distanceToTarget); // 목표에 가까워지지 않으면 페널티
            _reward = -distanceToTarget;
        }
    }

    private void SetEachJointPositions(List<double> jointArr, int index)
    {
        for (int i = 0; i < 6; i++)
        {
            if (double.IsNaN(jointArr[6 * index + i]))
            {
                Debug.LogError($"Error: jointArr[{6 * index + i}] is NaN. Program will terminate.");
                throw new InvalidOperationException("The solution is not Valid");
            }
        }

        ArticulationReducedSpace joint1 = new ArticulationReducedSpace((float)jointArr[6 * index + 0]);
        ArticulationReducedSpace joint2 = new ArticulationReducedSpace((float)jointArr[6 * index + 1]);
        ArticulationReducedSpace joint3 = new ArticulationReducedSpace((float)jointArr[6 * index + 2]);
        ArticulationReducedSpace joint4 = new ArticulationReducedSpace((float)jointArr[6 * index + 3]);
        ArticulationReducedSpace joint5 = new ArticulationReducedSpace((float)jointArr[6 * index + 4]);
        ArticulationReducedSpace joint6 = new ArticulationReducedSpace((float)jointArr[6 * index + 5]);

        ArticulationDrive drive1 = link1.xDrive;
        ArticulationDrive drive2 = link2.xDrive;
        ArticulationDrive drive3 = link3.xDrive;
        ArticulationDrive drive4 = link4.xDrive;
        ArticulationDrive drive5 = link5.xDrive;
        ArticulationDrive drive6 = link6.xDrive;

        drive1.target = (float)jointArr[6 * index + 0] * 180 / Mathf.PI;
        drive2.target = (float)jointArr[6 * index + 1] * 180 / Mathf.PI;
        drive3.target = (float)jointArr[6 * index + 2] * 180 / Mathf.PI;
        drive4.target = (float)jointArr[6 * index + 3] * 180 / Mathf.PI;
        drive5.target = (float)jointArr[6 * index + 4] * 180 / Mathf.PI;
        drive6.target = (float)jointArr[6 * index + 5] * 180 / Mathf.PI;

        link1.xDrive = drive1;
        link2.xDrive = drive2;
        link3.xDrive = drive3;
        link4.xDrive = drive4;
        link5.xDrive = drive5;
        link6.xDrive = drive6;

        link1.jointPosition = joint1;
        link2.jointPosition = joint2;
        link3.jointPosition = joint3;
        link4.jointPosition = joint4;
        link5.jointPosition = joint5;
        link6.jointPosition = joint6;
    }

    private void StartwithJointValue()
    {
        float j1 = 0;
        float j2 = 0;
        float j3 = 90;
        float j4 = 0;
        float j5 = 90;
        float j6 = 0;

        ArticulationReducedSpace joint1 = new ArticulationReducedSpace(j1 * 3.141592f / 180);
        ArticulationReducedSpace joint2 = new ArticulationReducedSpace(j2 * 3.141592f / 180);
        ArticulationReducedSpace joint3 = new ArticulationReducedSpace(j3 * 3.141592f / 180);
        ArticulationReducedSpace joint4 = new ArticulationReducedSpace(j4 * 3.141592f / 180);
        ArticulationReducedSpace joint5 = new ArticulationReducedSpace(j5 * 3.141592f / 180);
        ArticulationReducedSpace joint6 = new ArticulationReducedSpace(j6 * 3.141592f / 180);

        ArticulationDrive drive1 = link1.xDrive;
        ArticulationDrive drive2 = link2.xDrive;
        ArticulationDrive drive3 = link3.xDrive;
        ArticulationDrive drive4 = link4.xDrive;
        ArticulationDrive drive5 = link5.xDrive;
        ArticulationDrive drive6 = link6.xDrive;

        drive1.target = j1;
        drive2.target = j2;
        drive3.target = j3;
        drive4.target = j4;
        drive5.target = j5;
        drive6.target = j6;

        link1.xDrive = drive1;
        link2.xDrive = drive2;
        link3.xDrive = drive3;
        link4.xDrive = drive4;
        link5.xDrive = drive5;
        link6.xDrive = drive6;

        link1.jointPosition = joint1;
        link2.jointPosition = joint2;
        link3.jointPosition = joint3;
        link4.jointPosition = joint4;
        link5.jointPosition = joint5;
        link6.jointPosition = joint6;
    }
    public void RandomizeTargetPosition()
    {
        float randomX = Random.Range(positionRangeMin.x, positionRangeMax.x);
        float randomY = Random.Range(positionRangeMin.y, positionRangeMax.y);
        float randomZ = Random.Range(positionRangeMin.z, positionRangeMax.z);

        target.transform.position = new Vector3(randomX, randomY, randomZ);
    }
}
