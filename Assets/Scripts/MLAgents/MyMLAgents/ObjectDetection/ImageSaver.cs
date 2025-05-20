using UnityEngine;
using Random = UnityEngine.Random;
using MyMLAgents.Utilities;
using System.IO;

namespace MyMLAgents
{
    public class ImageSaver : MonoBehaviour
    {
        private GameObject Objects;
        private GameObject[] objectTypes;
        private Vector3 positionRangeMax;
        private Vector3 positionRangeMin;
        private float nextTime2 = 1f;
        private float interval2 = 5f;
        private float spawnTime;
        private Camera cam;
        private bool isSent = false;
        private Socket socket;
        private CubeSpawn cs;
        void Start()
        {
            socket = GetComponent<Socket>();    
            Objects = GameObject.Find("Objects");
            objectTypes = new GameObject[]
            {
                GameObject.CreatePrimitive(PrimitiveType.Cube),
                GameObject.CreatePrimitive(PrimitiveType.Cylinder),
                GameObject.CreatePrimitive(PrimitiveType.Capsule)
            };
            positionRangeMax = GameObject.Find("Corner_max").transform.position;
            positionRangeMin = GameObject.Find("Corner_min").transform.position;
            cam = GameObject.Find("IntelCamera")?.GetComponentInChildren<Camera>();
            Utils.MoveToInitialPosition(transform);
        }


        void Update()
        {
            if (Time.time >= nextTime2)
            {
                cs.SpawnCubes();
                spawnTime = Time.time;
                nextTime2 = Time.time + interval2;
                isSent = false;
            }
            if (Time.time >= spawnTime + 2f && !isSent)
            {
                Utils.FreezeObjects(Objects);
                SaveIMG();
            }
        }

        private void SaveIMG()
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
            isSent = true;
            socket.SendMsg(this);
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

    }



}