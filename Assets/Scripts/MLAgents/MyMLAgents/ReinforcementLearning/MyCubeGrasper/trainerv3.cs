using System;
using UnityEngine;
using Unity.MLAgents;
using Random = UnityEngine.Random;
using System.Collections;
using MyMLAgents.Utilities;
using static UnityEngine.GraphicsBuffer;
using Unity.VisualScripting;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;
using Unity.MLAgents.Actuators;
using System.Linq;
using TMPro;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;
using UnityEditor;
using GripperGWS;

namespace MyMLAgents
{
    public class trainer3 : Agent
    {
        public Transform EndEffector;

        public int AgentID;

        private Camera cam;
        private Camera depthCamera;
        private GameObject Objects;
        private GameObject target;
        private Camera agentCamera;
        private RenderTexture renderTxt;
        private CloseTargetGripper closeTargetGripper;
        private ArticulationBody[] links;
        private ArticulationBody[] grips;
        private CubeSpawn cs;
        private WrenchConvexHull GWS;


        private Vector3 XYZDown;

        private float[] targetBBInfo;
        private float Episode = 0;
        private float EpisodeReward = 0;
        private int EpisodeLength;
        private int counter = 0;
        private int idx = 0;
        private float _w = 0.25f;
        private float _reward;
        private float x_ = 0;
        private float y_ = 0;
        private bool ReadyToObserve = false;    
        private bool isActionInProgress = true;
        private bool isMovingTarget = false;
        private bool isMovingDown = false;
        private bool isMovingUp = false;    
        private bool isGrasping = false;
        private bool? _success = null;

        private float x;
        private float y;
        private float z;
        private float rx;
        private float ry;
        private float rz;

        private List<double> TargetArray = new List<double>();
        private List<double> DownArray = new List<double>();
        private List<double> UpArray = new List<double>();
        void Start()
        {
            int.TryParse(transform.parent.gameObject.name.Substring(5), out AgentID);
            links = Utils.GetLinks(transform);
            grips = Utils.GetGrips(transform);
            Random.InitState(Guid.NewGuid().GetHashCode());
            cam = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "IntelCamera")?.GetComponent<Camera>();
            depthCamera = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "IntelCameraDepth")?.GetComponent<Camera>();
            closeTargetGripper = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "GripperControl")?.GetComponent<CloseTargetGripper>();
            agentCamera = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "TargetCam")?.GetComponentInChildren<Camera>();
            GWS = GetComponent<WrenchConvexHull>();
            cs = gameObject.GetComponent<CubeSpawn>();  


            renderTxt = new RenderTexture(120, 120, 24);
            agentCamera.targetTexture = renderTxt;
        }
        public override void OnEpisodeBegin()
        {
            Episode = Episode + 1f;
            SetInitial();
            cs.SpawnCubes();
            StartCoroutine(StartwithDelayCoroutine());
            //StartwithDelay();
        }
        private IEnumerator StartwithDelayCoroutine()
        {
            Utils.MoveToInitialPosition(transform);
            Objects = transform.parent.Find("Objects")?.gameObject;
            EpisodeLength = Objects.transform.childCount;

            yield return new WaitForSeconds(0.1f);

            Utils.FreezeObjects(Objects);
            Utils.UnFreezeObjects(Objects);

            cs.DeleteOutlier(Objects);
            ReadyToObserve = true; isActionInProgress = false;
        }
        private void StartwithDelay()
        {
            Utils.MoveToInitialPosition(transform);
            Objects = transform.parent.Find("Objects")?.gameObject;
            EpisodeLength = Objects.transform.childCount;
            Utils.FreezeObjects(Objects);
            Utils.UnFreezeObjects(Objects);
            cs.DeleteOutlier(Objects);
            ReadyToObserve = true; isActionInProgress = false;
        }
        public override void CollectObservations(VectorSensor sensor)
        {
            if (ReadyToObserve)
            {

                cs.DeleteOutlier(Objects);
                _reward = 0f;

                isActionInProgress = true;
                float[] featureVector = trainerUtils.DetectOBJv3(this, cam, _success);
                if (featureVector != null && featureVector.Length == 258)
                {
                    float[] obsVec = new float[256];
                    Array.Copy(featureVector, 0, obsVec, 0, 256);
                    sensor.AddObservation(obsVec);

                    x_ = featureVector[256];
                    y_ = featureVector[257];
                }
                else
                {
                    Debug.LogWarning("Feature vector is null or has wrong length.");
                    EndEpisode();
                }
                ReadyToObserve = false;
            }

        }
        private void FixedUpdate()
        {
            counter++;
            if (counter >= 5) // 0.02�� �� 5 = 0.1��
            {
                counter = 0;
                if (isMovingTarget)
                {
                    try
                    {
                        if (idx >= TargetArray.Count / 6)
                        {
                            throw new Exception();
                        }
                        trainerUtils.SetEachJointPositions(TargetArray, idx, links);
                        idx++;
                    }
                    catch
                    {
                        DownArray = trainerUtils.GetMArray(XYZDown.x, XYZDown.y, XYZDown.z, rx, ry, rz, 1.0f, links);
                        isMovingTarget = false;
                        isMovingDown = true;
                        idx = 0;
                    }
                }
                if (isMovingDown)
                {
                    try
                    {
                        if (idx >= DownArray.Count / 6)
                        {
                            throw new Exception();
                        }
                        trainerUtils.SetEachJointPositions(DownArray, idx, links);
                        idx++;
                    }
                    catch
                    {
                        isMovingDown = false;
                        isGrasping = true;
                        idx = 0;
                    }
                }
                if (isGrasping)
                {
                    if (idx >= 30)
                    {
                        UpArray = trainerUtils.GetJArray(0, 0, 1.05f, 0, 2.1f, 0, 2.0f, links);
                        isGrasping = false;
                        isMovingUp = true;
                        idx = 0;
                    }
                    else
                    {
                        closeTargetGripper.ButtonClicked = true;
                        idx++;
                    }
                }
                if (isMovingUp)
                {
                    try
                    {
                        if (idx >= UpArray.Count / 6)
                        {
                            throw new Exception();
                        }
                        trainerUtils.SetEachJointPositions(UpArray, idx, links);
                        idx++;
                    }
                    catch
                    {
                        isMovingUp = false;
                        EvaluateReward();
                        closeTargetGripper.ButtonClicked = false;
                        Utils.MoveToInitialPosition(transform);
                        isActionInProgress = false;

                        if (Objects.transform.childCount == 1)
                        {
                            Debug.Log($"Reward for Episode{Episode}: {EpisodeReward}");
                            EndEpisode();
                        }
                        ReadyToObserve = true;

                        idx = 0;
                    }
                }
            }
        }
        public override void OnActionReceived(ActionBuffers actions)
        {
            isActionInProgress = true;
            CalJoints(actions);
            isMovingTarget = true;
        }
        private void CalJoints(ActionBuffers actions)
        {
            int x_offset = (int)(AgentID / 8) * 20;
            int z_offset = -(AgentID % 8) * 15;
            Vector3 TargetPosition = trainerUtils.GetWorldXYZv3(x_, y_, depthCamera);

            target = Utils.FindTarget(Objects, TargetPosition.x, TargetPosition.z);
            //Debug.Log($"WorldPosition: x:{TargetPosition.x}, y: {TargetPosition.y}, z: {TargetPosition.z}");
            //Debug.Log($"Received Action: {actions.ContinuousActions[0]}, {actions.ContinuousActions[1]}, {actions.ContinuousActions[2]}, {actions.ContinuousActions[3]}, {actions.ContinuousActions[4]}, {actions.ContinuousActions[5]}");
            target.name = "target";
            target.AddComponent<TargetContact>();
            GWS.SetTargetContact(target);
            GWS.targetContact.SetCollector(GWS.wrenchManager);

            x = TargetPosition.x + x_offset + _w * actions.ContinuousActions[0];
            y = TargetPosition.z + z_offset + _w * actions.ContinuousActions[1];
            z = TargetPosition.y + 0.2f + 0.2f * _w * actions.ContinuousActions[2];
            x = 0.1f * x;
            y = 0.1f * y;
            z = 0.1f * z;
            rx = actions.ContinuousActions[3] * 1.5f;
            ry = actions.ContinuousActions[4] * 0.75f + 3.14f;
            rz = actions.ContinuousActions[5] * 0.75f + 1.57f;

            XYZDown = Utils.LocalMovement(x, y, z, rx, ry, rz, true, EndEffector);

            TargetArray = trainerUtils.GetMArray(x, y, z, rx, ry, rz, 2.0f, links);
        }

        private void EvaluateReward()
        {
            float _reward_eps = 0f;
            float _reward_suc = 0f;

            if (target != null && target.transform != null)
            {
                _reward_eps = GWS.GetEpsilon();
                //_reward_eps = GWS.GetEpsilon();
                var targetContact = target.GetComponent<TargetContact>();

                if (targetContact != null && targetContact.isContact)
                {
                    _reward_suc = 1f;
                    _success = true;
                    EpisodeReward++;
                }
                else
                {
                    _reward_suc = 0f;
                    _success = false;
                }

                Destroy(target);
                cs.DeleteOutlier(Objects);
                GWS.ClearWrench();
            }
            else
            {
                _success = false;
                Debug.LogWarning("EvaluateReward: target is null or already destroyed.");
            }
            RewardLogger.LogReward(_reward_eps, _reward_suc);
            

            _reward = _reward_eps + _reward_suc;
            SetReward(_reward);
        }
        private void SetInitial()
        {
            EpisodeReward = 0;
            _reward = 0;
            isActionInProgress = true;
            closeTargetGripper.ButtonClicked = false;
            ReadyToObserve = false;
        }
        public bool GetisActionInProgress()
        { return isActionInProgress; }
    }

}
