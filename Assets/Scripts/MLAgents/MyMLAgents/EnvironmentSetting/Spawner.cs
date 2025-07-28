using UnityEngine;
using MyMLAgents.Utilities;
using System.Linq;
using UnityEngine.UIElements;

namespace MyMLAgents
{
    public class Spawner : MonoBehaviour
    {
        public Destroyer Dest;
        public GameObject Objects;
        public GameObject[] objectPrefabs; // 인스펙터에서 프리팹을 할당할 배열
        private Vector3 spawnRangeMax;
        private Vector3 spawnRangeMin;
        private Vector3 positionRangeMax;
        private Vector3 positionRangeMin;

        public void Awake()
        {
            Dest = GameObject.Find("Destroyer").GetComponent<Destroyer>();  
            Objects = transform.parent.Find("Objects")?.gameObject;
            spawnRangeMax = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Spawn_max")?.position ?? Vector3.zero;
            spawnRangeMin = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Spawn_min")?.position ?? Vector3.zero;
            positionRangeMax = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Corner_max")?.position ?? Vector3.zero;
            positionRangeMin = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Corner_min")?.position ?? Vector3.zero;
        }

        public void SpawnObjects()
        {
            Dest.ClearObjects(Objects);
            //SpawnObject(true);
            int n = Random.Range(60, 80); // 10, 40
            for (int i = 0; i < n; i++)
            {
                SpawnObject(false);
            }
        }

        public void SpawnObject(bool isTarget)
        {
            if (objectPrefabs == null || objectPrefabs.Length == 0)
            {
                Debug.LogError("Object prefabs are not assigned in the Spawner.");
                return;
            }
            // objectPrefabs 배열에서 무작위로 프리팹을 선택합니다.
            GameObject objPrefab = objectPrefabs[Random.Range(0, objectPrefabs.Length)];
            GameObject newObj = Object.Instantiate(objPrefab);
            newObj.transform.parent = Objects.transform;
            
            //newObj.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            newObj.transform.position = Utils.GetRandomPosition(spawnRangeMin, spawnRangeMax);
            newObj.transform.rotation = Utils.GetRandomOrientation();

            MeshRenderer renderer = newObj.GetComponent<MeshRenderer>();
            Color[] colors = { new Color(0.84f, 0.258f, 0.336f), new Color(0.93f, 0.785f, 0.273f), new Color(0.086f, 0.45f, 0.35f), new Color(0.074f, 0.551f, 0.852f), new Color(0.574f, 0.336f, 0.742f) }; // Purple

            Color randomColor = colors[Random.Range(0, colors.Length)];
            
            renderer.material.color = randomColor;

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

        public void DeleteOutlier(GameObject Objects)
        {
            foreach (Transform child in Objects.transform)
            {
                Vector3 pos = child.position;

                // ���� ������� Ȯ��
                if (!IsWithinRange(pos))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private bool IsWithinRange(Vector3 pos)
        {
            return pos.x >= positionRangeMin.x && pos.x <= positionRangeMax.x &&
               pos.z >= positionRangeMin.z && pos.z <= positionRangeMax.z;
        }
    }


}
