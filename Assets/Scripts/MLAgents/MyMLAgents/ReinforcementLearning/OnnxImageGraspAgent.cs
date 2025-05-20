using UnityEngine;
using Unity.Sentis;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using System.Collections;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

namespace MyMLAgents
{
    public class OnnxImageGraspAgent : MonoBehaviour
    {
        public ModelAsset modelAsset;
        public int SpaceSize;

        private Model runtimeModel; // ONNX 모델 파일을 할당
        private Tensor inputTensor;
        private Worker worker;
        private MyImageGraspBrain m_ImageGraspBrain;
        private VectorSensor sensor;
        private int width = 256;
        private int height = 256;
        private int channel = 3;

        void Start()
        {
            Academy.Instance.AutomaticSteppingEnabled = false;
            runtimeModel = ModelLoader.Load(modelAsset);
            worker = new Worker(runtimeModel, BackendType.CPU);
            m_ImageGraspBrain = GetComponentsInChildren<MyImageGraspBrain>()[0];
            sensor = new VectorSensor(observationSize: SpaceSize);

            m_ImageGraspBrain.OnEpisodeBegin();
        }


        void Update()
        {
            if (!m_ImageGraspBrain.GetisActionInProgress())
            {
                m_ImageGraspBrain.CollectObservations(sensor);
                ScheduleWorker();
                Tensor<float> outputTensor = worker.PeekOutput() as Tensor<float>;
                ActionBuffers Action = ConvertToActionBuffer(outputTensor);
                outputTensor.Dispose();
                m_ImageGraspBrain.DeterministicActionReceived(Action);
            }
        }
        public void ScheduleWorker()
        {
            float[] _data = m_ImageGraspBrain.GetObservation();
            float[] imgData = _data.Take(width * height * channel).ToArray();
            float[] vectorData = _data.Skip(width * height * channel).ToArray();

            TensorShape imgShape = new TensorShape(1, channel, width, height);
            TensorShape vecShape = new TensorShape(1, vectorData.Length);
            Tensor imageTensor = new Tensor<float>(imgShape, imgData);
            Tensor vectorTensor = new Tensor<float>(vecShape, vectorData);

            worker.SetInput(0, imageTensor);
            worker.SetInput(1, vectorTensor);

            worker.Schedule();
            
            imageTensor.Dispose();
            vectorTensor.Dispose();
        }
        public Tensor ConvertSensorToTensor()
        {
            float[] _data = m_ImageGraspBrain.GetObservation();
            float[] imgData = _data.Take(width * height * channel).ToArray();
            float[] vectorData = _data.Skip(width * height * channel).ToArray();

            TensorShape imgShape = new TensorShape(1, channel, width, height);
            TensorShape vecShape = new TensorShape(1, vectorData.Length);
            Tensor imageTensor = new Tensor<float> (imgShape, imgData);
            Tensor vectorTensor = new Tensor<float> (vecShape, vectorData);

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
        }
    }
}
