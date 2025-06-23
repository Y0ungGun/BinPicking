using UnityEngine;
using MyMLAgents.Utilities;
using System.Linq;
using UnityEngine.UIElements;

namespace MyMLAgents
{
    public class CubeSpawn : MonoBehaviour
    {
        public Destroyer Dest;
        public GameObject Objects;
        public GameObject[] objectTypes;
        private Vector3 positionRangeMax;
        private Vector3 positionRangeMin;

        public void Awake()
        {
            Dest = GameObject.Find("Destroyer").GetComponent<Destroyer>();  
            Objects = transform.parent.Find("Objects")?.gameObject;
            positionRangeMax = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Corner_max")?.position ?? Vector3.zero;
            positionRangeMin = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Corner_min")?.position ?? Vector3.zero;
            objectTypes = new GameObject[]
            {
                GameObject.CreatePrimitive(PrimitiveType.Cube),
                GameObject.CreatePrimitive(PrimitiveType.Cylinder),
                GameObject.CreatePrimitive(PrimitiveType.Capsule)
            };
        }

        public void SpawnCubes()
        {
            Dest.ClearObjects(Objects);
            //SpawnObject(true);
            int n = Random.Range(10, 40); // 10, 40
            for (int i = 0; i < n; i++)
            {
                SpawnObject(false);
            }
        }

        public void SpawnObject(bool isTarget)
        {
            GameObject objPrefab = objectTypes[0];
            GameObject newObj = Object.Instantiate(objPrefab);
            newObj.transform.parent = Objects.transform;

            float randomScaleX = Random.Range(0.2f, 0.5f);
            float randomScaleY = Random.Range(0.2f, 0.5f);
            float randomScaleZ = Random.Range(0.2f, 0.5f);
            newObj.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            newObj.transform.position = Utils.GetRandomPosition(positionRangeMin, positionRangeMax);
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

                // ¹üÀ§ ¹þ¾î³µ´ÂÁö È®ÀÎ
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
