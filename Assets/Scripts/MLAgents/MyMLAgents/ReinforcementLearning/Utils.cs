using DSRRobotControl;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;
using Color = UnityEngine.Color;
using Newtonsoft.Json;
using System.Linq;
using UnityEngine.SocialPlatforms;

namespace MyMLAgents.Utilities
{
    public static class Utils
    {
        public static GameObject FindTarget(GameObject Objects, float x, float z)
        {
            Transform[] allChildren = Objects.GetComponentsInChildren<Transform>();
            GameObject closest = null;
            float minDist = float.MaxValue;

            foreach (Transform child in allChildren)
            {
                // 자기 자신(Objects)나 비활성화된 오브젝트는 제외
                if (child == Objects.transform || !child.gameObject.activeInHierarchy)
                    continue;

                Vector3 pos = child.position;
                float dist = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(x, z));
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = child.gameObject;
                }
            }
            return closest;
        }
        public static void RandomizeObjectsPosition(GameObject target, GameObject[] nontargets, Vector3 PosMin, Vector3 PosMax)
        {
            target.transform.position = GetRandomPosition(PosMin, PosMax);
            target.transform.rotation = GetRandomOrientation();
            foreach (GameObject nontarget in nontargets)
            {
                nontarget.transform.position = GetRandomPosition(PosMin, PosMax);
                nontarget.transform.rotation = GetRandomOrientation();
            }
        }

        public static GameObject GetTarget(GameObject Objects)
        {
            Transform[] allChildren = Objects.GetComponentsInChildren<Transform>();
            foreach (Transform child in allChildren)
            {
                if (child.name == "Target")
                {
                    return child.gameObject;
                }
            }
            throw new Exception("Target not found in the specified GameObject.");
        }
        public static GameObject GetRandom(GameObject Objects)
        {
            Transform[] allChildren = Objects.GetComponentsInChildren<Transform>();
            if (allChildren.Length == 1)
            {
                return null;
            }
            return allChildren[1].gameObject;   
        }
        public static GameObject[] GetNonTargets(GameObject Objects)
        {
            Transform[] allChildren = Objects.GetComponentsInChildren<Transform>();

            return Array.FindAll(Array.ConvertAll(allChildren, t => t.gameObject), obj => obj.name == "NonTarget");
        }
        public static int GetTargetType(GameObject obj)
        {
            if (obj.GetComponent<BoxCollider>() != null)
            {
                return 0; // Cube
            }
            else if (obj.GetComponent<CapsuleCollider>() != null)
            {
                return 2; // Capsule
            }
            else if (obj.GetComponent<MeshFilter>() != null)
            {
                Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;
                if (mesh != null)
                {
                    if (mesh.name.Contains("Cylinder"))
                        return 1; // Cylinder
                }
            }
            return -1; // 알 수 없는 경우
        }

        public static Rect GetBoundingBoxInViewport(Camera cam, MeshRenderer renderer)
        {
            Bounds bounds = renderer.bounds;

            // Bounding Box의 8개 꼭짓점 좌표 구하기
            Vector3[] points = new Vector3[8];
            points[0] = bounds.min;
            points[1] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
            points[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
            points[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
            points[4] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
            points[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
            points[6] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
            points[7] = bounds.max;

            // Viewport 좌표로 변환 (0~1 사이 값)
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            foreach (Vector3 point in points)
            {
                Vector3 viewportPoint = cam.WorldToViewportPoint(point);
                min = Vector2.Min(min, viewportPoint);
                max = Vector2.Max(max, viewportPoint);
            }

            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }
        public static float[] GetIMG(RenderTexture renderTexture)
        {
            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height);
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;

            SaveTextureAsPNG(texture, $"Observation_.png");

            Color[] pixels = texture.GetPixels();
            float[] floatPixels = new float[pixels.Length * 3]; // RGB 값만 저장 (R, G, B)

            for (int i = 0; i < pixels.Length; i++)
            {
                floatPixels[i * 3] = pixels[i].r;    // Red
                floatPixels[i * 3 + 1] = pixels[i].g; // Green
                floatPixels[i * 3 + 2] = pixels[i].b; // Blue
            }

            return floatPixels;
        }
        public static void GetIMGs(Camera cam, MeshRenderer renderer, int num, GameObject[] Objects, int classId)
        {
            string basePath = @"D:\ObjectDetection";
            string fullImagesPath = Path.Combine(basePath, "FullImages");
            string cropImagesPath = Path.Combine(basePath, "CropImages");
            string resizeImagesPath = Path.Combine(basePath, "ResizeImages");
            string YOLOPath = Path.Combine(basePath, "YOLOAnnotations");
            string COCOPath = Path.Combine(basePath, "COCOAnnotations");

            // RenderTexture 설정
            RenderTexture renderTexture = new RenderTexture(1280, 720, 16);
            cam.targetTexture = renderTexture;
            cam.Render();

            // RenderTexture → Texture2D 변환
            Texture2D fullTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            RenderTexture.active = renderTexture;
            fullTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            fullTexture.Apply();
            RenderTexture.active = null;
            SaveTextureAsPNG(fullTexture, Path.Combine(fullImagesPath, $"fullImage{num}.png"));
            // Viewport Bounding Box 가져오기
            Rect viewportRect = GetBoundingBoxInViewport(cam, renderer);

            // Viewport (0~1)을 Pixel 좌표로 변환
            int x = Mathf.FloorToInt(viewportRect.x * fullTexture.width);
            int y = Mathf.FloorToInt(viewportRect.y * fullTexture.height);
            int width = Mathf.FloorToInt(viewportRect.width * fullTexture.width);
            int height = Mathf.FloorToInt(viewportRect.height * fullTexture.height);

            // Texture Crop 수행
            Texture2D croppedTexture = new Texture2D(width + 5, height + 5);
            Color[] croppedpixels = fullTexture.GetPixels(x - 5, y - 5, width + 5, height + 5);
            croppedTexture.SetPixels(croppedpixels);
            croppedTexture.Apply();
            SaveTextureAsPNG(croppedTexture, Path.Combine(cropImagesPath, $"cropImage{num}.png"));
            Texture2D resizedTexture = ResizeTexture(croppedTexture, 256, 256);
            SaveTextureAsPNG(resizedTexture, Path.Combine(resizeImagesPath, $"resizeImage{num}.png"));
        }
        public static void GetODLearningData(Camera cam, int num, GameObject Objects, GameObject[] objectTypes)
        {
            string basePath = @"D:\ObjectDetection";
            string fullImagesPath = Path.Combine(basePath, "FullImages");
            string YOLOPath = Path.Combine(basePath, "YOLOAnnotations");
            string COCOPath = Path.Combine(basePath, "COCOAnnotations");

            RenderTexture renderTexture = new RenderTexture(1280, 720, 16);
            cam.targetTexture = renderTexture;
            cam.Render();

            Texture2D fullTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            RenderTexture.active = renderTexture;
            fullTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            fullTexture.Apply();
            RenderTexture.active = null;
            SaveTextureAsPNG(fullTexture, Path.Combine(fullImagesPath, $"fullImage{num}.png"));

            SaveYoloFormat2(cam, YOLOPath, num, Objects, objectTypes);
            SaveCOCOFormat2(cam, COCOPath, num, Objects, objectTypes);
        }
        public static void SaveYoloFormat2(Camera cam, string labelsPath, int num, GameObject Objects, GameObject[] objectTypes)
        {
            string filePath = Path.Combine(labelsPath, $"image{num}.txt");

            foreach (Transform obj in Objects.transform)
            {
                Rect viewport = GetBoundingBoxInViewport(cam, obj.GetComponent<MeshRenderer>());
                float centerX = viewport.x + viewport.width / 2f;
                float centerY = viewport.y + viewport.height / 2f;
                float width = viewport.width;
                float height = viewport.height;

                int classID = GetClassID(objectTypes, obj);

                string yoloData = $"{classID} {centerX:F6} {centerY:F6} {width:F6} {height:F6}\n";
                File.AppendAllText(filePath, yoloData);
            }
        }
        private static int GetClassID(GameObject[] objectTypes, Transform obj)
        {
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                Mesh mesh = meshFilter.mesh;
                for (int i = 0; i < objectTypes.Length; i++)
                {
                    string objectTypeName = objectTypes[i].GetComponent<MeshFilter>().sharedMesh.name;

                    if (mesh.name.Contains(objectTypeName))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }
        public static void SaveYoloFormat(string labelsPath, int num, int classId, Rect viewportRect)
        {
            string filePath = Path.Combine(labelsPath, $"image{num}.txt");
            float centerX = viewportRect.x + viewportRect.width / 2f;
            float centerY = viewportRect.y + viewportRect.height / 2f;
            float width = viewportRect.width;
            float height = viewportRect.height;

            string yoloData = $"{classId} {centerX:F6} {centerY:F6} {width:F6} {height:F6}";
            File.WriteAllText(filePath, yoloData);
        }
        public static void SaveCOCOFormat2(Camera cam, string cocoPath, int num, GameObject Objects, GameObject[] objectTypes)
        {
            string filePath = Path.Combine(cocoPath, $"coco_annotations{num}.json");

            Dictionary<string, object> cocoData = new Dictionary<string, object>();
            List<object> images = new List<object>();
            List<object> annotations = new List<object>();
            int annotationId = 0;  // annotation ID (각 객체마다 증가)
            foreach (Transform obj in Objects.transform)
            {
                // BoundingBox (여기서는 예시로 가정)
                Rect viewport = GetBoundingBoxInViewport(cam, obj.GetComponent<MeshRenderer>());
                float centerX = viewport.x + viewport.width / 2f;
                float centerY = viewport.y + viewport.height / 2f;
                float width = viewport.width;
                float height = viewport.height;

                // Class ID (MeshRenderer의 종류에 따라 분류)
                int classId = GetClassID(objectTypes, obj);

                annotations.Add(new
                {
                    id = annotationId,
                    image_id = 0,  // 하나의 이미지에 대해 여러 객체 정보 추가
                    category_id = classId,
                    bbox = new float[] { centerX, centerY, width, height },  // bbox는 [x_center, y_center, width, height]
                    area = width * height,
                    iscrowd = 0
                });

                annotationId++;  // annotation ID 증가
            }
            cocoData["images"] = images;
            cocoData["annotations"] = annotations;
            cocoData["categories"] = new List<object>
            {
                new { id = 0, name = "cube" },
                new { id = 1, name = "cylinder" },
                new { id = 2, name = "capsule" }
            };

            // JSON 파일로 저장
            string json = JsonConvert.SerializeObject(cocoData, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        public static void SaveCocoFormat(string cocoPath, int num, int classId, int x, int y, int width, int height, int imgWidth, int imgHeight)
        {
            string filePath = Path.Combine(cocoPath, $"coco_annotations{num}.json");

            // COCO 구조 정의
            Dictionary<string, object> cocoData = new Dictionary<string, object>();
            List<object> images = new List<object>();
            List<object> annotations = new List<object>();

            images.Add(new
            {
                id = num,
                file_name = $"fullImage{num}.png",
                width = imgWidth,
                height = imgHeight
            });

            annotations.Add(new
            {
                id = num,
                image_id = num,
                category_id = classId,
                bbox = new int[] { x, y, width, height },
                area = width * height,
                iscrowd = 0
            });

            cocoData["images"] = images;
            cocoData["annotations"] = annotations;
            cocoData["categories"] = new List<object>
            {
                new { id = 0, name = "cube" },
                new { id = 1, name = "cylinder" },
                new { id = 2, name = "capsule" }
            };

            string json = JsonConvert.SerializeObject(cocoData, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        public static float[] GetTargetIMG(Camera cam, MeshRenderer renderer)
        {
            // RenderTexture 설정
            RenderTexture renderTexture = new RenderTexture(1280, 720, 16);
            cam.targetTexture = renderTexture;
            cam.Render();

            // RenderTexture → Texture2D 변환
            Texture2D fullTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            RenderTexture.active = renderTexture;
            fullTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            fullTexture.Apply();
            RenderTexture.active = null;
            SaveTextureAsPNG(fullTexture, $"FullImage.png");
            // Viewport Bounding Box 가져오기
            Rect viewportRect = GetBoundingBoxInViewport(cam, renderer);

            // Viewport (0~1)을 Pixel 좌표로 변환
            int x = Mathf.FloorToInt(viewportRect.x * fullTexture.width);
            int y = Mathf.FloorToInt(viewportRect.y * fullTexture.height);
            int width = Mathf.FloorToInt(viewportRect.width * fullTexture.width);
            int height = Mathf.FloorToInt(viewportRect.height * fullTexture.height);

            // Texture Crop 수행
            Texture2D croppedTexture = new Texture2D(width+5, height+5);
            Color[] croppedpixels = fullTexture.GetPixels(x-5, y-5, width+5, height+5);
            croppedTexture.SetPixels(croppedpixels);
            croppedTexture.Apply();
            SaveTextureAsPNG(croppedTexture, $"CroppedImage.png");
            Texture2D resizedTexture = ResizeTexture(croppedTexture, 256, 256);
            SaveTextureAsPNG(resizedTexture, $"ResizedImage.png");

            Color[] pixels = resizedTexture.GetPixels();
            resizedTexture.SetPixels(pixels);
            resizedTexture.Apply();
            float[] floatPixels = new float[pixels.Length * 3]; // RGB 값만 저장 (R, G, B)

            for (int i = 0; i < pixels.Length; i++)
            {
                floatPixels[i * 3] = pixels[i].r;    // Red
                floatPixels[i * 3 + 1] = pixels[i].g; // Green
                floatPixels[i * 3 + 2] = pixels[i].b; // Blue
            }

            return floatPixels;
        }
        public static Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
        {
            RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);

            Texture2D result = new Texture2D(newWidth, newHeight);
            result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            result.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return result;
        }

        public static void FreezeObjects(GameObject Objects)
        {
            Rigidbody[] rbs = Objects.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rbs)
            {
                rb.isKinematic = true;
            }
        }
        public static void UnFreezeObjects(GameObject Objects)
        {
            Rigidbody[] rbs = Objects.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rbs)
            {
                rb.isKinematic = false;
            }
        }
        public static void SetEachJointPositions(List<double> jointArr, int index, ArticulationBody[] links)
        {
            for (int i = 0; i < 6; i++)
            {
                if (double.IsNaN(jointArr[6 * index + i]))
                {
                    Debug.LogError($"Error: jointArr[{6 * index + i}] is NaN. Program will terminate.");
                    throw new InvalidOperationException("The solution is not Valid");
                }
            }

            ArticulationReducedSpace joint1 = new ArticulationReducedSpace((float)jointArr[6 * index + 0]);
            ArticulationReducedSpace joint2 = new ArticulationReducedSpace((float)jointArr[6 * index + 1]);
            ArticulationReducedSpace joint3 = new ArticulationReducedSpace((float)jointArr[6 * index + 2]);
            ArticulationReducedSpace joint4 = new ArticulationReducedSpace((float)jointArr[6 * index + 3]);
            ArticulationReducedSpace joint5 = new ArticulationReducedSpace((float)jointArr[6 * index + 4]);
            ArticulationReducedSpace joint6 = new ArticulationReducedSpace((float)jointArr[6 * index + 5]);

            ArticulationDrive drive1 = links[0].xDrive;
            ArticulationDrive drive2 = links[1].xDrive;
            ArticulationDrive drive3 = links[2].xDrive;
            ArticulationDrive drive4 = links[3].xDrive;
            ArticulationDrive drive5 = links[4].xDrive;
            ArticulationDrive drive6 = links[5].xDrive;

            drive1.target = (float)jointArr[6 * index + 0] * 180 / Mathf.PI;
            drive2.target = (float)jointArr[6 * index + 1] * 180 / Mathf.PI;
            drive3.target = (float)jointArr[6 * index + 2] * 180 / Mathf.PI;
            drive4.target = (float)jointArr[6 * index + 3] * 180 / Mathf.PI;
            drive5.target = (float)jointArr[6 * index + 4] * 180 / Mathf.PI;
            drive6.target = (float)jointArr[6 * index + 5] * 180 / Mathf.PI;

            links[0].xDrive = drive1;
            links[1].xDrive = drive2;
            links[2].xDrive = drive3;
            links[3].xDrive = drive4;
            links[4].xDrive = drive5;
            links[5].xDrive = drive6;
        }
        public static void MoveToInitialPosition(Transform transform)
        {
            ArticulationBody[] links = GetLinks(transform);
            ArticulationBody[] grips = GetGrips(transform);

            float j1 = 0;
            float j2 = 0;
            float j3 = 60;
            float j4 = 0;
            float j5 = 120;
            float j6 = 0;
            float g = 0;

            ArticulationReducedSpace joint1 = new ArticulationReducedSpace(j1 * 3.141592f / 180);
            ArticulationReducedSpace joint2 = new ArticulationReducedSpace(j2 * 3.141592f / 180);
            ArticulationReducedSpace joint3 = new ArticulationReducedSpace(j3 * 3.141592f / 180);
            ArticulationReducedSpace joint4 = new ArticulationReducedSpace(j4 * 3.141592f / 180);
            ArticulationReducedSpace joint5 = new ArticulationReducedSpace(j5 * 3.141592f / 180);
            ArticulationReducedSpace joint6 = new ArticulationReducedSpace(j6 * 3.141592f / 180);
            ArticulationReducedSpace gr = new ArticulationReducedSpace(g * 3.141592f / 180);

            ArticulationDrive drive1 = links[0].xDrive;
            ArticulationDrive drive2 = links[1].xDrive;
            ArticulationDrive drive3 = links[2].xDrive;
            ArticulationDrive drive4 = links[3].xDrive;
            ArticulationDrive drive5 = links[4].xDrive;
            ArticulationDrive drive6 = links[5].xDrive;
            ArticulationDrive drive7 = grips[0].xDrive;

            drive1.target = j1;
            drive2.target = j2;
            drive3.target = j3;
            drive4.target = j4;
            drive5.target = j5;
            drive6.target = j6;
            drive7.target = g;

            links[0].xDrive = drive1;
            links[1].xDrive = drive2;
            links[2].xDrive = drive3;
            links[3].xDrive = drive4;
            links[4].xDrive = drive5;
            links[5].xDrive = drive6;
            grips[0].xDrive = drive7;
            grips[1].xDrive = drive7;
            grips[2].xDrive = drive7;
            grips[3].xDrive = drive7;
            grips[4].xDrive = drive7;
            grips[5].xDrive = drive7;

            links[0].jointPosition = joint1;
            links[1].jointPosition = joint2;
            links[2].jointPosition = joint3;
            links[3].jointPosition = joint4;
            links[4].jointPosition = joint5;
            links[5].jointPosition = joint6;
            grips[0].jointPosition = gr;
            grips[1].jointPosition = gr;
            grips[2].jointPosition = gr;
            grips[3].jointPosition = gr;
            grips[4].jointPosition = gr;
            grips[5].jointPosition = gr;
        }
        public static Vector3 LocalMovement(float x, float y, float z, float rx, float ry, float rz, bool isDown, Transform Endeffecter)
        {
            Vector3 XYZ = new Vector3(x, y, z);
            Vector3 RPY = new Vector3(rx, ry, rz);
            Quaternion localQuat = Euler2Quat.ZYZ2Quat(RPY);
            //localQuat = Endeffecter.localRotation;
            Vector3 localOffset = new Vector3(0, 0, 0);
            if (isDown)
            {
                localOffset.z = +0.02f;
                //localOffset.y = -0.02f;
                //localOffset.x = -0.02f;
            }
            else
            {
                localOffset.y = +0.04f;
            }
            return XYZ + localQuat * localOffset;
        }
        public static List<double> GetMArray(float x, float y, float z, float rx, float ry, float rz, float time, ArticulationBody link1, ArticulationBody link6)
        {
            Movel Move = new Movel()
            {
                command = "Movel",
                desiredPosition = new float[] { x, y-15f, z, rx, ry, rz },
                velocity = new float[] { 0.0f, 0.0f },
                acceleration = new float[] { 0.0f, 0.0f },
                time = time,
                reference = 1
            };
            List<Command> Commands = new List<Command> { Move };
            CommandList CommandList = new CommandList();
            CommandList.commands = Commands;
            List<double> MoveArray = new List<double>();

            try
            {
                MoveArray = CommandList.ExecuteCommands(link6, link1);
            }
            catch (ArgumentNullException) { }
            catch (InvalidOperationException) { }
            catch (Exception) { }

            return MoveArray;
        }
        
        public static List<double> GetJArray(float j1, float j2, float j3, float j4, float j5, float j6, float time, ArticulationBody link1, ArticulationBody link6)
        {
            Movej Move = new Movej()
            {
                command = "Movej",
                desiredPosition = new float[] { j1, j2, j3, j4, j5, j6 },
                velocity = new float[] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f },
                acceleration = new float[] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f },
                time = time
            };
            List<Command> Commands = new List<Command> { Move };
            CommandList CommandList = new CommandList();
            CommandList.commands = Commands;
            List<double> MoveArray = new List<double>();

            try
            {
                MoveArray = CommandList.ExecuteCommands(link6, link1);
            }
            catch (ArgumentNullException) { }
            catch (InvalidOperationException) { }
            catch (Exception) { }

            return MoveArray;
        }
        public static Vector3 GetRandomPosition(Vector3 positionRangeMin, Vector3 positionRangeMax)
        {
            float x = Random.Range(positionRangeMin.x + 0.2f, positionRangeMax.x - 0.2f);
            float y = Random.Range(positionRangeMin.y + 0.2f, positionRangeMax.y - 0.8f);
            float z = Random.Range(positionRangeMin.z + 0.2f, positionRangeMax.z - 0.2f);

            return new Vector3(x, y, z);
        }

        public static Quaternion GetRandomOrientation()
        {
            float randomRotationX = Random.Range(0f, 360f);
            float randomRotationY = Random.Range(0f, 360f);
            float randomRotationZ = Random.Range(0f, 360f);

            return Quaternion.Euler(randomRotationX, randomRotationY, randomRotationZ);
        }

        public static void SaveTextureAsPNG(Texture2D texture, string fileName)
        {
            byte[] bytes = texture.EncodeToPNG();
            string filePath = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllBytes(filePath, bytes);
            //Debug.Log($"Saved PNG: {filePath}");
        }
        
        public static ArticulationBody[] GetLinks(Transform transform)
        {
            ArticulationBody[] links = new ArticulationBody[6];

            links[0] = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "link1")?.GetComponent<ArticulationBody>();
            links[1] = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "link2")?.GetComponent<ArticulationBody>();
            links[2] = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "link3")?.GetComponent<ArticulationBody>();
            links[3] = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "link4")?.GetComponent<ArticulationBody>();
            links[4] = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "link5")?.GetComponent<ArticulationBody>();
            links[5] = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "link6")?.GetComponent<ArticulationBody>();

            return links;
        }

        public static ArticulationBody[] GetGrips(Transform transform)
        {
            ArticulationBody[] grips = new ArticulationBody[6];

            grips[0] = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "LinkLeftInner")?.GetComponent<ArticulationBody>();
            grips[1] = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "LinkLeftOuter")?.GetComponent<ArticulationBody>();
            grips[2] = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "LinkRightInner")?.GetComponent<ArticulationBody>();
            grips[3] = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "LinkRightOuter")?.GetComponent<ArticulationBody>();
            grips[4] = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "JawLeft")?.GetComponent<ArticulationBody>();
            grips[5] = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "JawRight")?.GetComponent<ArticulationBody>();

            return grips;
        }
    }
}