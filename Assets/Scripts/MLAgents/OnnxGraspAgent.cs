using UnityEngine;
using Unity.Sentis;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using System.Collections;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;
using System;
using System.IO;

public class OnnxGraspAgent : MonoBehaviour
{
    public ModelAsset modelAsset;
    public int SpaceSize;

    private Model runtimeModel; // ONNX 모델 파일을 할당
    private Tensor inputTensor;
    private Worker worker;
    private MyGraspBrain m_GraspBrain;
    private VectorSensor sensor;
    private IEnumerator modelEnumerator;
    private List<float> rewards;

    private string MoveDebugPath;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MoveDebugPath = $"C://Users/dudrj/unityworkspace/DSR_ML/Assets/Log/SuccessLog{DateTime.Now.ToString("yyMMddHHmm")}.csv";
        string input = string.Format("Episode, Success/Fail\n");
        File.AppendAllText(MoveDebugPath, input);
        Academy.Instance.AutomaticSteppingEnabled = false;
        runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, BackendType.CPU);
        m_GraspBrain = GetComponentsInChildren<MyGraspBrain>()[0];
        sensor = new VectorSensor(observationSize: SpaceSize);

        m_GraspBrain.OnEpisodeBegin();
    }

    // Update is called once per frame
    void Update()
    {
        if (!m_GraspBrain.GetisActionInProgress())
        {
            m_GraspBrain.CollectObservations(sensor);
            inputTensor = ConvertSensorToTensor();
            worker.Schedule(inputTensor);
            inputTensor.Dispose();
            Tensor<float> outputTensor = worker.PeekOutput() as Tensor<float>;
            ActionBuffers Action = ConvertToActionBuffer(outputTensor);
            outputTensor.Dispose();
            m_GraspBrain.DeterministicActionReceived(Action, GetRewardAfterAction);
        }
    }   
    public Tensor ConvertSensorToTensor()
    {
        float[] _data = m_GraspBrain.GetObservation();
        int VectorSize = _data.Length;
        TensorShape _shape = new TensorShape(1, VectorSize);

        Tensor<float> inputT = new Tensor<float>(_shape, _data);

        return inputT;
    }
    public ActionBuffers ConvertToActionBuffer(Tensor<float> outputT)
    {
        if (outputT == null)
        {
            Debug.LogError("Output Tensor is null.");
            return default;
        }

        float[] actionArray = outputT.DownloadToArray();
        return new ActionBuffers(
            new ActionSegment<float>(actionArray), // Continuous actions
            new ActionSegment<int>(new int[0])    // Discrete actions (비어 있을 경우)
        );
        // return actionArray;
    }

    private void GetRewardAfterAction()
    {
        float currentReward = m_GraspBrain.GetReward();
        Debug.Log($"Reward: {currentReward}");
        if (currentReward > 4.0f)
        {
            string input = string.Format($"{(int)m_GraspBrain.GetEpisode()}, {2}\n");
            File.AppendAllText(MoveDebugPath, input);
        }
        else if (currentReward > 1.0f)
        {
            string input = string.Format($"{(int)m_GraspBrain.GetEpisode()}, {1}\n");
            File.AppendAllText(MoveDebugPath, input);
        }
        else
        {
            string input = string.Format($"{(int)m_GraspBrain.GetEpisode()}, {0}\n");
            File.AppendAllText(MoveDebugPath, input);
        }
    }
}
