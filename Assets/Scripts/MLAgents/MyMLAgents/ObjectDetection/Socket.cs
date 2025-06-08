using UnityEngine;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Text;
using System.Collections.Generic;
using MyMLAgents.Utilities;
using System.Linq;
using UnityEngine.UIElements;

namespace MyMLAgents
{
    public class Socket : MonoBehaviour
    {
        public string serverIP = "127.0.0.1";  // ���� IP
        public int serverPort = 7779;
        private Transform Connector;
        private MeshRenderer Con;
        private Material successMat;
        private Material failMat;
        private GameObject target;
        private Camera cam;
        private GameObject Objects;
        private void Start()
        {
            Connector = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Connector");
            Con = Connector.GetComponent<MeshRenderer>();
            successMat = Resources.Load<Material>("Success");
            failMat = Resources.Load<Material>("Fail");
            cam = transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "IntelCamera")?.GetComponent<Camera>();
            Objects = transform.parent.Find("Objects")?.gameObject;
        }

        public float[] SendMsg(Component component)
        {
            using (TcpClient client = new TcpClient(serverIP, serverPort))
            using (NetworkStream stream = client.GetStream())
            using (StreamWriter writer = new StreamWriter(stream))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                string msg = "-1";
                var comp = component.GetComponent<trainer2>();
                if (comp != null)
                {
                    msg = comp.AgentID.ToString();
                }
                writer.WriteLine(msg);
                writer.Flush();
                //Debug.Log("Messge sent");

                int responseLength = reader.ReadInt32();
                List<float[]> detections = new List<float[]>();
                for (int i = 0; i < responseLength; i++)
                {
                    float[] detection = new float[6];
                    for (int j = 0; j < 6; j++)
                    {
                        detection[j] = reader.ReadSingle(); 
                    }
                    detections.Add(detection);
                }
                target = Utils.GetRandom(Objects);
                //target = GameObject.Find("Target");
                int id = HandleResponse.FindTargetBoundingBoxIndex(detections, cam, target);
                HandleResponse.CreateBoundingBoxPNG(detections[id], cam);
                //Debug.Log("target BB: " + string.Join(", ", detections[id]));
                return detections[id];
            }
        }

        public float[] SendMsgv3(Component component, bool? success)
        {
            using (TcpClient client = new TcpClient(serverIP, serverPort))
            using (NetworkStream stream = client.GetStream())
            using (StreamWriter writer = new StreamWriter(stream))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                string msg = "-1";
                var comp = component.GetComponent<trainer3>();
                if (comp != null)
                {
                    if (success.HasValue)
                        msg = $"{comp.AgentID},{(success.Value ? 1 : 0)}";
                    else
                        msg = $"{comp.AgentID}";
                }
                writer.WriteLine(msg); 
                writer.Flush();
                //Debug.Log("Messge sent");
                if (success == true)
                    Con.material = successMat;
                else if (success == false)
                    Con.material = failMat;
                int featureLength = reader.ReadInt32();
                if (featureLength <= 0 || featureLength > 1024)
                {
                    Debug.LogWarning("Invalid feature vector length received: " + featureLength);
                    return null;
                }

                float[] featureVector = new float[featureLength + 2];
                //for (int i = 0; i < featureLength+2; i++)
                //{
                //    featureVector[i] = reader.ReadSingle();
                //}

                for (int i = 0; i < featureLength + 2; i++)
                {
                    try
                    {
                        featureVector[i] = reader.ReadSingle();
                    }
                    catch (EndOfStreamException e)
                    {
                        // 예외 발생 시 stream의 전체 내용을 로그로 남김
                        try
                        {
                            if (stream.CanSeek)
                            {
                                stream.Position = 0;
                                byte[] allBytes = new byte[stream.Length];
                                stream.Read(allBytes, 0, allBytes.Length);
                                Debug.Log("Stream dump (hex): " + BitConverter.ToString(allBytes));
                            }
                            else
                            {
                                List<byte> buffer = new List<byte>();
                                byte[] temp = new byte[1024];
                                int bytesRead;
                                while ((bytesRead = stream.Read(temp, 0, temp.Length)) > 0)
                                {
                                    buffer.AddRange(temp.Take(bytesRead));
                                }
                                Debug.Log("Stream dump (partial, hex): " + BitConverter.ToString(buffer.ToArray()));
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Log("Failed to dump stream: " + ex);
                        }
                        Debug.LogError($"EndOfStreamException during feature parsing at index {i}: {e}");
                        break; // 반복문 종료
                    }
                    catch (IOException e)
                    {
                        Debug.LogError($"IOException during feature parsing at index {i}: {e}");
                        break; // 반복문 종료
                    }
                }
                return featureVector;
            }
        }
    }
}
