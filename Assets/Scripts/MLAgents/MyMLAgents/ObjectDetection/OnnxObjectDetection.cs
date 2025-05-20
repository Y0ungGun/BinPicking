using UnityEngine;
using Unity.Sentis;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine.UI;
using System.Collections;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using UnityEngine.Timeline;
using static Unity.Sentis.Model;
using Random = UnityEngine.Random;
using MyMLAgents.Utilities;
using Unity.VisualScripting;

namespace MyMLAgents
{
    public class OnnxObjectDetection : MonoBehaviour
    {
        public ModelAsset modelAsset;

        private DrawBoundingBoxes drawBoundingBoxesScript;
        private Model model;
        private Worker worker;
        private int width = 736;
        private int height = 736;
        private Texture2D redTexture;
        private Texture2D greenTexture;
        private Texture2D blueTexture;
        private Rect rect;
        private int ID;
        private Camera cam;
        private float nextTime2 = 1f;
        private float interval2 = 10f;
        private GameObject Objects;
        private GameObject[] objectTypes;
        private Vector3 positionRangeMax;
        private Vector3 positionRangeMin;
        void Start()
        {
            Objects = GameObject.Find("Objects");
            objectTypes = new GameObject[]
            {
                GameObject.CreatePrimitive(PrimitiveType.Cube),
                GameObject.CreatePrimitive(PrimitiveType.Cylinder),
                GameObject.CreatePrimitive(PrimitiveType.Capsule)
            };
            positionRangeMax = GameObject.Find("Corner_max").transform.position;
            positionRangeMin = GameObject.Find("Corner_min").transform.position;
            Utils.MoveToInitialPosition(transform);
            redTexture = new Texture2D(2, 2);
            greenTexture = new Texture2D(2, 2);
            blueTexture = new Texture2D(2, 2);
            Color red = Color.red;
            Color green = Color.green;
            Color blue = Color.blue;
            for (int y = 0; y < redTexture.height; y++)
            {
                for (int x = 0; x < redTexture.width; x++)
                {
                    redTexture.SetPixel(x, y, red);
                    blueTexture.SetPixel(x, y, blue);
                    greenTexture.SetPixel(x, y, green);
                }
            }
            redTexture.Apply();
            blueTexture.Apply();
            greenTexture.Apply();
            cam = GameObject.Find("IntelCamera")?.GetComponentInChildren<Camera>();
            Academy.Instance.AutomaticSteppingEnabled = false;
            model = ModelLoader.Load(modelAsset);
            worker = new Worker(model, BackendType.CPU);
            RandomSpawn();
        }

        void Update()
        {
            if (Time.time >= nextTime2)
            {
                GetTexture();
                //RunInference(GetTexture());
                nextTime2 = Time.time + interval2;
            }
        }

        void RandomSpawn()
        {
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
        private Texture2D GetTexture()
        {
            RenderTexture renderTexture = new RenderTexture(1280, 740, 16);
            cam.targetTexture = renderTexture;
            cam.Render();

            Texture2D fullTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            RenderTexture.active = renderTexture;
            fullTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            fullTexture.Apply();
            RenderTexture.active = null;

            int cropSize = 736;
            int centerX = renderTexture.width / 2;
            int centerY = renderTexture.height / 2;
            int startX = centerX - (cropSize / 2);
            int startY = centerY - (cropSize / 2);

            Texture2D croppedTexture = new Texture2D(cropSize, cropSize, TextureFormat.RGB24, false);
            croppedTexture.SetPixels(fullTexture.GetPixels(startX, startY, cropSize, cropSize));
            croppedTexture.Apply();

            byte[] image = croppedTexture.EncodeToPNG();
            File.WriteAllBytes("image.png", image);
            Debug.Log("Image saved to captured_image.png");
            return croppedTexture;
        }

        private Tensor PreprocessInput(Texture2D image)
        {
            // 이미지 크기 조정 및 정규화
            var resizedImage = ResizeImage(image, width, height);

            Color[] pixels = resizedImage.GetPixels();
            float[] inputData = new float[pixels.Length * 3];

            for (int i = 0; i < pixels.Length; i++)
            {
                inputData[i * 3] = pixels[i].r;   // Red 채널
                inputData[i * 3 + 1] = pixels[i].g; // Green 채널
                inputData[i * 3 + 2] = pixels[i].b; // Blue 채널
            }

            return new Tensor<float>(new TensorShape(1, 3, width, height), inputData);
        }
        private Texture2D ResizeImage(Texture2D source, int width, int height)
        {
            RenderTexture rt = new RenderTexture(width, height, 24);
            Graphics.Blit(source, rt);
            Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply();
            RenderTexture.active = null;
            return result;
        }

        void RunInference(Texture2D image)
        {
            Tensor inputTensor = PreprocessInput(image);
            worker.SetInput(0, inputTensor);
            worker.Schedule();

            Tensor<float> outputTensor = worker.PeekOutput() as Tensor<float>;
            outputTensor.CompleteAllPendingOperations();
            OutputNMS(outputTensor);

            inputTensor.Dispose();
            outputTensor.Dispose();
        }
        float Sig(float x)
        {
            return 1.0f / (1.0f + Mathf.Exp(-x));
        }
        private void OutputNMS(Tensor<float> outputTensor)
        {
            List<Tensor<float>> NMSResult = new List<Tensor<float>>();
            NMSResult = NonMaxSuppression.NMS(outputTensor);

            for (int i = 0; i < NMSResult.Count; i++)
            {
                float[] tensorData = NMSResult[i].DownloadToArray();
                Debug.Log($"NMS Result {i}: " + string.Join(", ", tensorData));
            }
        }
        private void ProcessOutput(Tensor<float> output)
        {
            float[] data = output.DownloadToArray();
            // 데이터를 파싱하여 Bounding Box, Confidence Score 추출
            for (int i = 0; i < data.Length; i += 8)
            {
                float conf = Sig(data[i + 4]);
                Debug.Log($"Raw Conf: {data[i + 4]}, Sigmoid Conf: {conf}");
                if (conf > 0.8f)
                {
                    float x = data[i];     // Center X
                    float y = data[i + 1]; // Center Y
                    float w = data[i + 2]; // Width
                    float h = data[i + 3]; // Height
                    float[] classProbabilities = new float[3];
                    for (int j = 0; j < classProbabilities.Length; j++)
                    {
                        classProbabilities[j] = data[i + 5 + j];
                    }
                    int classID = Array.IndexOf(classProbabilities, classProbabilities.Max());
                    float classProb = classProbabilities[classID];

                    if (classProb > 0.80f)
                    {
                        //AddBoundingBox(200, 150, 80, 90, "cube", Color.red);
                        // 로그 출력 (선택 사항)
                        Debug.Log($"Detected Object: Class {classID} at ({x}, {y}) with {conf * 100}% confidence");
                    }
                }
            }
        }
        public void AddBoundingBox(float x, float y, float w, float h, string label, Color color)
        {
            DrawBoundingBoxes.BoundingBoxXYWH newBox = new DrawBoundingBoxes.BoundingBoxXYWH
            {
                X = x,
                Y = y,
                Width = w,
                Height = h,
                label = label,
                color = color
            };

            drawBoundingBoxesScript.boundingBoxes.Add(newBox);
        }
    }
}

