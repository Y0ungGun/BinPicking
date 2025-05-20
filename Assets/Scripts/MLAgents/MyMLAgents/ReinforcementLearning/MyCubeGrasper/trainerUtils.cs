using System.Net.Sockets;
using UnityEngine;
using System.IO;
using DSRRobotControl;
using System.Collections.Generic;
using System;
using Color = UnityEngine.Color;
using MyMLAgents.Utilities;
using UnityEngine.UIElements;
using System.IO.Abstractions;
using static Unity.Robotics.UrdfImporter.Link.Visual.Material;
using Unity.VisualScripting;
using System.Linq;
using System.Text.RegularExpressions;

namespace MyMLAgents
{
    public static class trainerUtils
    {
        //private static Camera cam = GameObject.Find("IntelCamera")?.GetComponentInChildren<Camera>();
        //private static Camera depthCamera = GameObject.Find("IntelCameraDepth")?.GetComponentInChildren<Camera>();
        private static Texture2D CroppedIMG;
        private static RenderTexture depthTexture = new RenderTexture(1280, 740, 24, RenderTextureFormat.Depth)
        {
            enableRandomWrite = false,
            name = "Depth Texture (dynamic)"
        };
        public static List<double> GetMArray(float x, float y, float z, float rx, float ry, float rz, float time, ArticulationBody[] links)
        {
            Movel Move = new Movel()
            {
                command = "Movel",
                desiredPosition = new float[] { x, y, z, rx, ry, rz },
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
                MoveArray = CommandList.ExecuteCommands(links[5], links[0]);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return MoveArray;
        }
        public static List<double> GetJArray(float j1, float j2, float j3, float j4, float j5, float j6, float time, ArticulationBody[] links)
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
                MoveArray = CommandList.ExecuteCommands(links[5], links[0]);
            }
            catch (ArgumentNullException) { }
            catch (InvalidOperationException) { }
            catch (Exception) { }

            return MoveArray;
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
        public static Texture2D GetTargetIMG2D(float[] targetBBInfo)
        {
            int x1 = (int)targetBBInfo[0];
            int y1 = 736 - (int)targetBBInfo[1];
            int x2 = (int)targetBBInfo[2];
            int y2 = 736 - (int)targetBBInfo[3];
            int X = (x1 + x2) / 2;
            int Y = (y1 + y2) / 2;
            int W = 120;
            int H = 120;
            int X0 = X - W / 2;
            int Y0 = Y - H / 2;

            Texture2D targetTxt = new Texture2D(W, H);
            Color[] targetClr = CroppedIMG.GetPixels(X0, Y0, W, H);
            targetTxt.SetPixels(targetClr);
            targetTxt.Apply();

            SaveTextureAsPNG(targetTxt, "Observation.png");

            return targetTxt;
        }
        public static float[] getTargetIMG(float[] targetBBInfo)
        {
            int x1 = (int)targetBBInfo[0];
            int y1 = 736 - (int)targetBBInfo[1];
            int x2 = (int)targetBBInfo[2];
            int y2 = 736 - (int)targetBBInfo[3];
            int X = (x1 + x2) / 2;
            int Y = (y1 + y2) / 2;
            int W = 120;
            int H = 120;
            int X0 = X - W / 2;
            int Y0 = Y - H / 2;

            Texture2D targetTxt = new Texture2D(W, H);
            Color[] targetClr = CroppedIMG.GetPixels(X0, Y0, W, H);
            targetTxt.SetPixels(targetClr);
            targetTxt.Apply();
            float[] targetPxls = new float[targetClr.Length * 3];

            SaveTextureAsPNG(targetTxt, "Observation.png");
            for (int i = 0; i < targetClr.Length; i++)
            {
                targetPxls[i * 3] = targetClr[i].r;    // Red
                targetPxls[i * 3 + 1] = targetClr[i].g; // Green
                targetPxls[i * 3 + 2] = targetClr[i].b; // Blue
            }
            return targetPxls;
        }
        public static void SaveTextureAsPNG(Texture2D texture, string fileName)
        {
            byte[] bytes = texture.EncodeToPNG();
            string filePath = Path.Combine(Application.dataPath, fileName);
            File.WriteAllBytes(filePath, bytes);
            //Debug.Log($"Saved PNG: {filePath}"); 
        }
        public static void SaveTextureAsEXR(Texture2D texture, string fileName)
        {
            byte[] bytes = texture.EncodeToEXR();
            string filePath = Path.Combine(Application.dataPath, fileName);
            File.WriteAllBytes(filePath, bytes);
            //Debug.Log($"Saved PNG: {filePath}"); 
        }
        public static Texture2D GetDepth(Camera depthCamera)
        {
            ComputeShader cs = Resources.Load<ComputeShader>("NormalizeDepth");
            RenderTexture resultTexture = new RenderTexture(1280, 740, 0, RenderTextureFormat.RFloat)
            {
                enableRandomWrite = true,
                name = "Result Texture (dynamic)"
            };
            resultTexture.Create();
            depthTexture.Create();

            depthCamera.targetTexture = depthTexture;
            depthCamera.Render();

            int kernelHandle = cs.FindKernel("CSMain");
            cs.SetTexture(kernelHandle, "Source", depthTexture); //source texture�� depthtexture�� ������.
            cs.SetTexture(kernelHandle, "Result", resultTexture);

            cs.Dispatch(kernelHandle, depthTexture.width / 8, depthTexture.height / 8, 1);

            RenderTexture.active = resultTexture;

            int cropSize = 736;
            int centerX = resultTexture.width / 2;
            int centerY = resultTexture.height / 2;
            int startX = centerX - (cropSize / 2);
            int startY = centerY - (cropSize / 2);

            Texture2D txt = new Texture2D(cropSize, cropSize, TextureFormat.RFloat, false);
            txt.ReadPixels(new Rect(startX, startY, cropSize, cropSize), 0, 0);
            txt.Apply();

            RenderTexture.active = null;
            //SaveTextureAsPNG(txt, "Depth.png");

            resultTexture.Release();
            UnityEngine.Object.Destroy(resultTexture);


            return txt;
        }
        public static float GetZ(int x, int y, Camera depthCamera)
        {
            Texture2D Depth = GetDepth(depthCamera);
            float DepthValue = 1 - Depth.GetPixel(x, y).r;
            float zValue = depthCamera.nearClipPlane / (1.0f - DepthValue * (1.0f - depthCamera.nearClipPlane / depthCamera.farClipPlane));

            UnityEngine.Object.Destroy(Depth);
            return zValue;
        }
        public static Vector3 GetWorldXYZ(float[] BBInfo, Camera depthCamera)
        {
            int x_ = (int)(BBInfo[0] + BBInfo[2]) / 2;
            int y_ = (int)(BBInfo[1] + BBInfo[3]) / 2;
            y_ = 736 - y_;
            float z_ = GetZ(x_, y_, depthCamera);
            int a = (depthCamera.pixelWidth - 736) / 2;
            int b = (depthCamera.pixelHeight - 736) / 2;
            Vector3 WorldXYZ = depthCamera.ScreenToWorldPoint(new Vector3(x_ + a, y_ - b, z_));
            return WorldXYZ;
        }

        public static Vector3 GetWorldXYZv3(float x, float y, Camera depthCamera)
        {
            int x_ = (int)x;
            int y_ = (int)y;
            y_ = 736 - y_;
            float z_ = GetZ(x_, y_, depthCamera);
            int a = (depthCamera.pixelWidth - 736) / 2;
            int b = (depthCamera.pixelHeight - 736) / 2;
            Vector3 WorldXYZ = depthCamera.ScreenToWorldPoint(new Vector3(x_ + a, y_ - b, z_));
            return WorldXYZ;
        }


        public static int GetInstanceID()
        {
            string[] args = Environment.GetCommandLineArgs();
            int instanceId = 0; 

            foreach (string arg in args)
            {
                if (arg.Contains("Player-") && arg.EndsWith(".log"))
                {
                    Match match = Regex.Match(arg, @"Player-(\d+)\.log");
                    if (match.Success)
                    {
                        instanceId = int.Parse(match.Groups[1].Value);
                        return instanceId;
                    }
                }
            }
            return instanceId;
        }
        public static float[] DetectOBJ(Component component, Camera cam)
        {
            Socket socket = component.GetComponent<Socket>();
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

            CroppedIMG = new Texture2D(cropSize, cropSize, TextureFormat.RGB24, false);
            CroppedIMG.SetPixels(fullTexture.GetPixels(startX, startY, cropSize, cropSize));
            CroppedIMG.Apply();

            byte[] image = CroppedIMG.EncodeToPNG();

            string fileName = @$"C:\Users\dudrj\unityworkspace\MultiAgent\py\images\image_{component.GetComponent<trainer2>().AgentID}.png";

            File.WriteAllBytes(fileName, image);
            //Debug.Log("Image saved to captured_image.png");
            float[] targetBB = socket.SendMsg(component);
            // isSent = true;
            return targetBB;
        }
        public static float[] DetectOBJv3(Component component, Camera cam, bool? success)
        {
            Socket socket = component.GetComponent<Socket>();
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

            CroppedIMG = new Texture2D(cropSize, cropSize, TextureFormat.RGB24, false);
            CroppedIMG.SetPixels(fullTexture.GetPixels(startX, startY, cropSize, cropSize));
            CroppedIMG.Apply();

            byte[] image = CroppedIMG.EncodeToPNG();

            string fileName = Path.Combine(Directory.GetParent(Application.dataPath).FullName,"py","images",$"image_{component.GetComponent<trainer3>().AgentID}.png");


            File.WriteAllBytes(fileName, image);
            //Debug.Log("Image saved to captured_image.png");
            float[] featureVector = socket.SendMsgv3(component, success);

            UnityEngine.Object.Destroy(fullTexture);
            renderTexture.Release();
            UnityEngine.Object.Destroy(renderTexture);
            UnityEngine.Object.Destroy(CroppedIMG);
            // isSent = true;
            return featureVector;
        }
    }

}
