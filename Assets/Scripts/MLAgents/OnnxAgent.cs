using UnityEngine;
using Unity.Sentis;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using System.Collections;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;

public class OnnxAgent : MonoBehaviour
{
    public ModelAsset modelAsset;
    public int SpaceSize;

    private Model runtimeModel; // ONNX 모델 파일을 할당
    private Tensor inputTensor;
    private Worker worker;
    private MyArmAgent m_ArmAgent;
    private VectorSensor sensor;
    private IEnumerator modelEnumerator;
    private List<float> rewards;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Academy.Instance.AutomaticSteppingEnabled = false;
        runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, BackendType.CPU);
        m_ArmAgent = GetComponentsInChildren<MyArmAgent>()[0];
        sensor = new VectorSensor(observationSize: SpaceSize);

        m_ArmAgent.OnEpisodeBegin();
    }

    // Update is called once per frame
    void Update()
    {
        if (!m_ArmAgent.GetisActionInProgress())
        {
            m_ArmAgent.CollectObservations(sensor);
            inputTensor = ConvertSensorToTensor();
            worker.Schedule(inputTensor);
            inputTensor.Dispose();
            Tensor<float> outputTensor = worker.PeekOutput() as Tensor<float>;
            ActionBuffers Action = ConvertToActionBuffer(outputTensor);
            outputTensor.Dispose();
            m_ArmAgent.DeterministicActionReceived(Action);
            // m_ArmAgent.OnActionReceived(Action);
            float currentReward = m_ArmAgent.GetReward();
            if (currentReward == 5.0f)
            {
                Debug.Log("Succes!");
            }
            else
            {
                Debug.Log("Fail");
            }
        }
    }   
    public Tensor ConvertSensorToTensor()
    {
        float[] _data = m_ArmAgent.GetObservation();
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
}
