using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Linq;
using DSRRobotControl;
using Random = UnityEngine.Random;
using MyMLAgents.Utilities;

public class MyImageGraspBrain : Agent
{
    public Transform EndEffector;
    public ArticulationBody[] links;
    public ArticulationBody[] grips;

    private GameObject target;
    private GameObject[] nontargets;

    private GameObject Objects;
    private float Episode = 0;
    private bool isActionInProgress = false;
    private bool ReadyToObserve = false;
    private float[] _observation;
    private float _reward;

    private CloseTargetGripper closeTargetGripper;
    private GetCollision LeftCollision;
    private GetCollision RightCollision;


    private Vector3 positionRangeMax;
    private Vector3 positionRangeMin;
    private Camera cam;
    private GameObject[] objectTypes;

    private float _w = 0.1f;
    private float r_dist;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        objectTypes = new GameObject[]{
                GameObject.CreatePrimitive(PrimitiveType.Cube),
                GameObject.CreatePrimitive(PrimitiveType.Cylinder),
                GameObject.CreatePrimitive(PrimitiveType.Capsule)
            };
        foreach (var type in objectTypes)
        {
            type.transform.position = new Vector3(6.15f, 0.17f, -3f);
        }
        cam = GameObject.Find("IntelCamera")?.GetComponentInChildren<Camera>();
        closeTargetGripper = GameObject.Find("GripperControl").GetComponent<CloseTargetGripper>();
        positionRangeMax = GameObject.Find("Corner_max").transform.position;
        positionRangeMin = GameObject.Find("Corner_min").transform.position;
    }
    public bool GetisActionInProgress()
    { return isActionInProgress; }
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
    public override void OnEpisodeBegin()
    {
        Episode = Episode + 1.0f;
        SetInitial();
        RandomSpawn();
        StartCoroutine(StartwithDelay());
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        if (ReadyToObserve)
        {
            try
            {
                float[] floatIMG = Utils.GetTargetIMG(cam, target.GetComponent<MeshRenderer>());
                Vector3 normalizedPosition = new Vector3((target.transform.position.x - 4.35f), (target.transform.position.y), (target.transform.position.z - 0.33f) / 2.0f);
                int totalLength = floatIMG.Length + 3 + 1;
                _observation = new float[totalLength];
                Array.Copy(floatIMG, 0, _observation, 0, floatIMG.Length);
                _observation[floatIMG.Length] = target.transform.position.x;
                _observation[floatIMG.Length + 1] = target.transform.position.y;
                _observation[floatIMG.Length + 2] = target.transform.position.z;
                _observation[floatIMG.Length + 3] = 0f;
            }
            catch (ArgumentException e)
            {
                Debug.LogError($"[ERROR] GetPixels ½ÇÆÐ: {e.Message}");
            }
            ReadyToObserve = false;
        }

    }
    private IEnumerator StartwithDelay()
    {
        Utils.MoveToInitialPosition(transform);
        yield return new WaitForSeconds(4.0f);
        Objects = GameObject.Find("Objects");
        target = Utils.GetTarget(Objects);
        nontargets = Utils.GetNonTargets(Objects);
        Utils.FreezeObjects(Objects);
        Utils.UnFreezeObjects(Objects);
        ReadyToObserve = true;
        yield return new WaitForSeconds(1.0f);
        isActionInProgress = false;
    }
    public void DeterministicActionReceived(ActionBuffers actions)
    {
        isActionInProgress = true;
        StartCoroutine(PerformAction(actions));
    }
    void RandomSpawn()
    {
        Objects = GameObject.Find("Objects");
        ClearObjects();
        SpawnObject(true);
        int n = Random.Range(0, 11);
        for (int i = 0; i < n; i++)
        {
            SpawnObject(false);
        }
    }
    void SpawnObject(bool isTarget)
    {
        GameObject objPrefab = objectTypes[Random.Range(0, objectTypes.Length)];
        GameObject newObj = Instantiate(objPrefab);
        newObj.transform.parent = Objects.transform;

        float randomScaleX = Random.Range(0.2f, 0.5f);
        float randomScaleY = Random.Range(0.2f, 0.5f);
        float randomScaleZ = Random.Range(0.2f, 0.5f);
        newObj.transform.localScale = new Vector3(randomScaleX, randomScaleY, randomScaleZ);
        newObj.transform.position = Utils.GetRandomPosition(positionRangeMin, positionRangeMax);
        newObj.transform.rotation = Utils.GetRandomOrientation();

        MeshRenderer renderer = newObj.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material.color = Random.ColorHSV();
        }

        Rigidbody rb = newObj.AddComponent<Rigidbody>();
        rb.useGravity = true;

        if (isTarget)
        {
            newObj.name = "Target";
        }
        else
        {
            newObj.name = "NonTarget";
        }
    }

    void ClearObjects()
    {
        foreach (Transform child in Objects.transform)
        {
            Destroy(child.gameObject);
        }
    }
    private IEnumerator PerformAction(ActionBuffers actions)
    {
        float x = target.transform.position.x + _w * actions.ContinuousActions[0];
        float y = target.transform.position.z + _w * actions.ContinuousActions[1];
        float z = target.transform.position.y + 0.2f + 0.05f * _w * actions.ContinuousActions[2];
        x = 0.1f * x;
        y = 0.1f * y;
        z = 0.1f * z;
        //float x = actions.ContinuousActions[0] * 0.06f + 0.435f;
        //float y = actions.ContinuousActions[1] * 0.12f + 0.033f;
        //float z = actions.ContinuousActions[2] * 0.03f + 0.075f;
        float rx = actions.ContinuousActions[3] * 1.5f;
        float ry = actions.ContinuousActions[4] * 0.75f + 3.14f;
        float rz = actions.ContinuousActions[5] * 0.75f + 1.57f;

        // Move To Target
        List<double> MoveTarget = Utils.GetMArray(x, y, z, rx, ry, rz, 2.0f, links[0], links[5]);
        for (int i = 0; i < MoveTarget.Count / 6; i++)
        {
            try
            {
                Utils.SetEachJointPositions(MoveTarget, i, links);
            }
            catch
            {
                continue;
            }
            yield return new WaitForSeconds(0.05f);
        }
        yield return new WaitForSeconds(0.1f);

        // Move Downward
        Vector3 XYZDown = Utils.LocalMovement(x, y, z, rx, ry, rz, true, EndEffector);
        List<double> MoveDown = Utils.GetMArray(XYZDown.x, XYZDown.y, XYZDown.z, rx, ry, rz, 1.0f, links[0], links[5]);
        for (int j = 0; j < MoveDown.Count / 6; j++)
        {
            try
            {
                Utils.SetEachJointPositions(MoveDown, j, links);
            }
            catch
            {
                continue;
            }
            yield return new WaitForSeconds(0.05f);
        }
        yield return new WaitForSeconds(0.1f);
        r_dist = EvaluateDistance();

        // Close Gripper
        closeTargetGripper.ButtonClicked = true;
        yield return new WaitForSeconds(3.5f);

        // Move Upward
        Vector3 XYZUp = Utils.LocalMovement(x, y, z, rx, ry, rz, false, EndEffector);
        List<double> MoveUp = Utils.GetJArray(0, 0, 1.57f, 0, 1.57f, 0, 2.0f, links[0], links[5]);
        for (int j = 0; j < MoveUp.Count / 6; j++)
        {
            try
            {
                Utils.SetEachJointPositions(MoveUp, j, links);
            }
            catch
            {
                continue;
            }
            yield return new WaitForSeconds(0.05f);
        }
        yield return new WaitForSeconds(0.2f);
        isActionInProgress = false;
        EvaluateReward();
        EndEpisode();
    }
    private float EvaluateDistance()
    {
        float horizontalDistance = Vector2.Distance(new Vector2(EndEffector.position.x, EndEffector.position.z), new Vector2(target.transform.position.x, target.transform.position.z));
        float verticalDistance = Mathf.Abs(EndEffector.position.y - target.transform.position.y);
        float distnaceToTarget = Vector3.Distance(EndEffector.position, target.transform.position);
        if (horizontalDistance < 0.1f && verticalDistance < 0.1f)
        {
            _reward += 3.0f;
        }

        return distnaceToTarget;
    }
    private void EvaluateReward()
    {
        _reward += -r_dist;


        if (target.transform.position.y >= 0.3f)
        {
            _reward += 2.0f;
        }

        SetReward(_reward);
    }
    private void SetInitial()
    {
        _reward = 0;
        isActionInProgress = true;
        closeTargetGripper.ButtonClicked = false;
        ReadyToObserve = false;
    }
}
