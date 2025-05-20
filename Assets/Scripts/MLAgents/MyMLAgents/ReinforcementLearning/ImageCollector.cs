using MyMLAgents.Utilities;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using System.Collections;
using System.IO;
using System;
using Random = UnityEngine.Random;
namespace MLAgents
{
    public class ImageCollector : MonoBehaviour
    {
        public ArticulationBody[] links;
        public ArticulationBody[] grips;
        private GameObject Target;
        private GameObject[] NonTargets;
        private int num;
        private float nextTime = 0f;
        private float interval = 5f;
        private Camera cam;

        private Vector3 positionRangeMax;
        private Vector3 positionRangeMin;
        private GameObject Objects;
        private GameObject[] objectTypes;
        void Start()
        {
            Time.timeScale = 3.0f;
            Utils.MoveToInitialPosition(transform);
            Objects = GameObject.Find("Objects");
            objectTypes = new GameObject[]
            {
                GameObject.CreatePrimitive(PrimitiveType.Cube),
                GameObject.CreatePrimitive(PrimitiveType.Cylinder),
                GameObject.CreatePrimitive(PrimitiveType.Capsule)
            };
            num = GetNextFileNumber("D:/ObjectDetection/FullImages");
            Debug.Log($"NUM={num}");
            cam = GameObject.Find("IntelCamera")?.GetComponentInChildren<Camera>();
            positionRangeMax = GameObject.Find("Corner_max").transform.position;
            positionRangeMin = GameObject.Find("Corner_min").transform.position;

        }

        // Update is called once per frame
        void Update()
        {
            if (Time.time >= nextTime)
            {
                StartCoroutine(ImageCollect());
                nextTime = Time.time + interval;
            }
        }

        private IEnumerator ImageCollect()
        {
            RandomSpawn();
            yield return new WaitForSeconds(2.0f);
            Utils.FreezeObjects(Objects);
            //Target = GameObject.Find("Target");
            // NonTargets = Utils.GetNonTargets(Objects);
            Utils.GetODLearningData(cam, num, Objects, objectTypes);
            yield return new WaitForSeconds(1.0f);
            Debug.Log("Image collected.");
            num += 1;
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
        int GetNextFileNumber(string directory)
        {
            string[] files = Directory.GetFiles(directory);

            int maxNum = -1;

            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file); // 파일명만 추출
                if (fileName.StartsWith("fullImage"))
                {
                    try
                    {
                        // "resizeimage" 뒤의 번호를 추출
                        string numberPart = fileName.Substring("fullImage".Length);
                        int num = int.Parse(numberPart);
                        maxNum = Math.Max(maxNum, num); // 가장 큰 번호를 찾음
                    }
                    catch (FormatException)
                    {
                        // 번호가 아닌 경우 예외 처리
                        Debug.LogWarning("파일 이름에서 번호를 추출할 수 없습니다: " + fileName);
                    }
                }
            }

            // 새로 저장할 파일 번호는 가장 큰 번호에 1을 더한 값
            return maxNum + 1;
        }
    }



}
