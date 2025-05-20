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
using System.IO;

public class MyGraspBrain : Agent
{
    public GameObject Objects;
    public Transform EndEffector;
    public ArticulationBody link1;
    public ArticulationBody link2;
    public ArticulationBody link3;
    public ArticulationBody link4;
    public ArticulationBody link5;
    public ArticulationBody link6;
    public ArticulationBody grip1;
    public ArticulationBody grip2;
    public ArticulationBody grip3;
    public ArticulationBody grip4;
    public ArticulationBody JawLeft;
    public ArticulationBody JawRight;

    private GameObject target;
    private GameObject[] nontargets;

    private float Episode = 0;
    private bool isActionInProgress = false;
    // private bool isFirstAction = true;
    private bool skipEpisode = false;
    private bool isCollided = false;
    private float[] _observation;
    private float _reward;
    private CloseTargetGripper closeTargetGripper;
    private GetCollision LeftCollision;
    private GetCollision RightCollision;

    private Movel Movel;
    private CommandList commandList;

    private Vector3 positionRangeMax;
    private Vector3 positionRangeMin;
    void Start()
    {
        GetObjects();
        closeTargetGripper = GameObject.Find("GripperControl").GetComponent<CloseTargetGripper>();
        positionRangeMax = GameObject.Find("Corner_max").transform.position;
        positionRangeMin = GameObject.Find("Corner_min").transform.position;
        LeftCollision = JawLeft.GetComponent<GetCollision>();
        RightCollision = JawRight.GetComponent<GetCollision>();
        LeftCollision.OnCollisionEnterEvent += PanaltyCollision;
        RightCollision.OnCollisionEnterEvent += PanaltyCollision;
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
    public float GetEpisode()
    {
        return Episode;
    }
    private void PanaltyCollision(Collision collision)
    {
        if (collision.gameObject.name != "Target" && !isCollided)
        {
            Debug.Log("Collision Occured");
            _reward += -5f;
            isCollided = true;
        }
    }
    public override void OnEpisodeBegin()
    {
        Episode = Episode + 1.0f;
        _reward = 0;
        isCollided = false;
        // isFirstAction = true;
        isActionInProgress = true;
        closeTargetGripper.ButtonClicked = false;
        skipEpisode = false;
        RandomizeObjectsPosition();
        StartCoroutine(StartwithDelay());
    }

    private IEnumerator StartwithDelay()
    {
        StartwithJointValue();
        yield return new WaitForSeconds(1.0f);
        isActionInProgress = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 관찰 데이터 수집 (예: 로봇 위치, 목표 위치, 속도 등)
        //sensor.AddObservation(EndEffector.position);
        // sensor.AddObservation(target.transform.position);
        _observation = new float[] { target.transform.position.x, target.transform.position.y, target.transform.position.z };
    }
    public void DeterministicActionReceived(ActionBuffers actions, Action callback)
    {
        isActionInProgress = true;
        StartCoroutine(PerformAction(actions, callback));
    }
    /*
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
    }*/
    
    private IEnumerator PerformAction(ActionBuffers actions, Action callback)
    {
        float x = actions.ContinuousActions[0] * 0.15f + 0.45f;
        float y = actions.ContinuousActions[1] * 0.2f;
        float z = actions.ContinuousActions[2] * 0.04f + 0.29f;
        float rx = actions.ContinuousActions[3] * 1.5f;

        // Move To Target
        List<double> Move = GetMoveArray(x, y, z, rx, 2.0f);
        for (int i = 0; i < Move.Count / 6; i++)
        {
            SetEachJointPositions(Move, i);
            yield return new WaitForSeconds(0.05f);
        }
        yield return new WaitForSeconds(0.1f);

        // Move Downward
        List<double> MoveDown = GetMoveArray(x, y, z - 0.02f, rx, 1.0f);
        for (int j = 0; j < MoveDown.Count / 6; j++)
        {
            SetEachJointPositions(MoveDown, j);
            yield return new WaitForSeconds(0.05f);
        }
        yield return new WaitForSeconds(0.1f);
        EvaluateDistance();
        // Close Gripper
        closeTargetGripper.ButtonClicked = true;
        yield return new WaitForSeconds(3.5f);

        // Move Upward
        List<double> MoveUp = GetMoveArray(x, y, z + 0.02f, rx, 1.0f);
        for (int j = 0; j < MoveUp.Count / 6; j++)
        {
            SetEachJointPositions(MoveUp, j);
            yield return new WaitForSeconds(0.05f);
        }
        yield return new WaitForSeconds(0.2f);
        isActionInProgress = false;
        EvaluateReward();
        callback?.Invoke();
        EndEpisode();
    }
    private void EvaluateDistance()
    {
        float horizontalDistance = Vector2.Distance(new Vector2(EndEffector.position.x, EndEffector.position.z), new Vector2(target.transform.position.x, target.transform.position.z));
        float verticalDistance = Mathf.Abs(EndEffector.position.y - target.transform.position.y);
        if (horizontalDistance < 0.1f && verticalDistance < 0.1f)
        {
            _reward += 3.0f;
        }
    }
    private void EvaluateReward()
    {
        float distanceToTarget = Vector3.Distance(EndEffector.position, target.transform.position);

        _reward += -distanceToTarget;

        
        if (target.transform.position.y >= 0.3f)
        {
            _reward += 2.0f;
        }
        if(!skipEpisode) 
        {
            SetReward(_reward);
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

        //link1.jointPosition = joint1;
        //link2.jointPosition = joint2;
        //link3.jointPosition = joint3;
        //link4.jointPosition = joint4;
        //link5.jointPosition = joint5;
        //link6.jointPosition = joint6;
    }

    private void StartwithJointValue()
    {
        float j1 = 0;
        float j2 = 0;
        float j3 = 90;
        float j4 = 0;
        float j5 = 90;
        float j6 = 0;
        float g = 0;

        ArticulationReducedSpace joint1 = new ArticulationReducedSpace(j1 * 3.141592f / 180);
        ArticulationReducedSpace joint2 = new ArticulationReducedSpace(j2 * 3.141592f / 180);
        ArticulationReducedSpace joint3 = new ArticulationReducedSpace(j3 * 3.141592f / 180);
        ArticulationReducedSpace joint4 = new ArticulationReducedSpace(j4 * 3.141592f / 180);
        ArticulationReducedSpace joint5 = new ArticulationReducedSpace(j5 * 3.141592f / 180);
        ArticulationReducedSpace joint6 = new ArticulationReducedSpace(j6 * 3.141592f / 180);
        ArticulationReducedSpace gr = new ArticulationReducedSpace(g * 3.141592f / 180);

        ArticulationDrive drive1 = link1.xDrive;
        ArticulationDrive drive2 = link2.xDrive;
        ArticulationDrive drive3 = link3.xDrive;
        ArticulationDrive drive4 = link4.xDrive;
        ArticulationDrive drive5 = link5.xDrive;
        ArticulationDrive drive6 = link6.xDrive;
        ArticulationDrive drive7 = grip1.xDrive;

        drive1.target = j1;
        drive2.target = j2;
        drive3.target = j3;
        drive4.target = j4;
        drive5.target = j5;
        drive6.target = j6;
        drive7.target = g;

        link1.xDrive = drive1;
        link2.xDrive = drive2;
        link3.xDrive = drive3;
        link4.xDrive = drive4;
        link5.xDrive = drive5;
        link6.xDrive = drive6;
        grip1.xDrive = drive7;
        grip2.xDrive = drive7;
        grip3.xDrive = drive7;
        grip4.xDrive = drive7;
        JawLeft.xDrive = drive7;
        JawRight.xDrive = drive7;

        link1.jointPosition = joint1;
        link2.jointPosition = joint2;
        link3.jointPosition = joint3;
        link4.jointPosition = joint4;
        link5.jointPosition = joint5;
        link6.jointPosition = joint6;
        grip1.jointPosition = gr;
        grip2.jointPosition = gr;
        grip3.jointPosition = gr;
        grip4.jointPosition = gr;
        JawLeft.jointPosition = gr;
        JawRight.jointPosition = gr;
    }
    public void RandomizeObjectsPosition()
    {
        target.transform.position = GetRandomPosition();
        target.transform.rotation = GetRandomOrientation();
        foreach (GameObject nontarget in nontargets)
        {
            nontarget.transform.position = GetRandomPosition();
            nontarget.transform.rotation = GetRandomOrientation();
        }
    }

    private List<double> GetMoveArray(float x, float y, float z, float rx, float time)
    {
        Movel Move = new Movel()
        {
            command = "Movel",
            desiredPosition = new float[] { x, y, z, rx, 3.141592f, 0 },
            velocity = new float[] { 0.0f, 0.0f },
            acceleration = new float[] { 0.0f, 0.0f },
            time = time,
        };
        List<Command> Commands = new List<Command> { Move };
        CommandList CommandList = new CommandList();
        CommandList.commands = Commands;

        List<double> MoveArray = new List<double>();

        try
        {
            // ExecuteCommands 메서드 실행
            MoveArray = CommandList.ExecuteCommands(link6, link1);
        }
        catch (ArgumentNullException)
        {
            // Null 값과 관련된 예외 처리
            skipEpisode = true;
        }
        catch (InvalidOperationException)
        {
            // 메서드 로직과 관련된 예외 처리
            skipEpisode = true;
        }
        catch (Exception)
        {
            // 그 외 모든 예외 처리
            skipEpisode = true;
        }
        return MoveArray;
    } 
    private Vector3 GetRandomPosition()
    {
        float x = Random.Range(positionRangeMin.x, positionRangeMax.x);
        float y = Random.Range(positionRangeMin.y, positionRangeMax.y);
        float z = Random.Range(positionRangeMin.z, positionRangeMax.z);

        return new Vector3(x, y, z);
    }
    private Quaternion GetRandomOrientation()
    {
        float randomRotationX = Random.Range(0f, 360f); 
        float randomRotationY = Random.Range(0f, 360f); 
        float randomRotationZ = Random.Range(0f, 360f);

        return Quaternion.Euler(randomRotationX, randomRotationY, randomRotationZ);
    }
    private void GetObjects()
    {
        Transform[] allChildren = Objects.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            if (child.name == "Target")
            {
                target = child.gameObject;
                break;
            }
        }

        nontargets = System.Array.FindAll(
            System.Array.ConvertAll(allChildren, t => t.gameObject),
            obj => obj.name == "NonTarget"
        );
    }
}
