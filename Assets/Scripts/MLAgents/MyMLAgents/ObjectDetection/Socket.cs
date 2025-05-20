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
        private GameObject target;
        private Camera cam;
        private GameObject Objects;
        private void Start()
        {
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
                writer.WriteLine(msg); // �޽��� "1"�� ����
                writer.Flush();
                //Debug.Log("Messge sent");

                int responseLength = reader.ReadInt32();
                List<float[]> detections = new List<float[]>();
                for (int i = 0; i < responseLength; i++)
                {
                    float[] detection = new float[6];
                    for (int j = 0; j < 6; j++)
                    {
                        detection[j] = reader.ReadSingle();  // �� float ���� ����
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

                int featureLength = reader.ReadInt32();
                if (featureLength <= 0 || featureLength > 1024)
                {
                    Debug.LogWarning("Invalid feature vector length received: " + featureLength);
                    return null;
                }

                float[] featureVector = new float[featureLength + 2];
                for (int i = 0; i < featureLength+2; i++)
                {
                    featureVector[i] = reader.ReadSingle();
                }
                return featureVector;
            }
        }
    }
}
