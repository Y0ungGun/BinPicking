using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using DSRRobotControl;
using Unity.Robotics.UrdfImporter;


namespace GripperControl
{
    public class OpenGripper : MonoBehaviour
    {
        public ArticulationBody LinkLefty;
        public ArticulationBody LinkRighty;
        public ArticulationBody JawLefty;
        public ArticulationBody JawRighty;
        public ArticulationBody LinkLeftyInner;
        public ArticulationBody LinkRightyInner;

        public float velocityRatio = 100;

        private List<double> joints = new List<double>();
        private float velocity;
        private float acceleration;

        public static int moveIndex = 0;
        private void Start()
        {
            velocity = (30 * Mathf.PI / 180) * (velocityRatio / 100);
            acceleration = velocity / 4;
        }

        public void OnButtonClick()
        {
            ArticulationReducedSpace start = LinkLefty.jointPosition;

            List<double> joints = new List<double>();
            float dtheta = Mathf.Abs(0 - start[0]);
            float T1 = velocity / acceleration;
            if (dtheta <= T1 * velocity)
            {
                joints = JointProfile1(start[0], 0, velocity, acceleration);
            }
            else
            {
                joints = JointProfile2(start[0], 0, velocity, acceleration);
            }

            StartCoroutine(MoveRobot(joints));
        }
        private IEnumerator MoveRobot(List<double> jointArr)
        {
            moveIndex = 0;
            while (moveIndex < jointArr.Count)
            {
                SetEachJointPositions(jointArr, moveIndex);
                moveIndex++;
                yield return new WaitForSeconds(0.05f);
            }
            Debug.Log("GRIPPER OPENED.");
        }

        private void SetEachJointPositions(List<double> jointArr, int index)
        {
            ArticulationReducedSpace joint1 = new ArticulationReducedSpace((float)jointArr[index]);
            ArticulationReducedSpace joint2 = new ArticulationReducedSpace(-(float)jointArr[index]);

            LinkLefty.jointPosition = joint1;
            LinkRighty.jointPosition = joint2;
            JawLefty.jointPosition = joint2;
            JawRighty.jointPosition = joint1;
            LinkLeftyInner.jointPosition = joint1;
            LinkRightyInner.jointPosition = joint2;
        }

        public static List<double> JointProfile1(float start, float end, float vel, float acc)
        {
            List<double> joints = new List<double>();
            float dtheta = Mathf.Abs(end - start);
            float T1 = vel / acc;
            float T2 = Mathf.Sqrt(dtheta / acc);
            float T3 = 2 * T2;

            int i;
            for (i = 0; 0.05f * i <= T2; i++)
            {
                float t = 0.05f * i;
                float r = (0.5f * acc * t * t) / dtheta;
                double result = DLerp.dLerp(start, end, r);
                joints.Add(result);
            }
            for (; 0.05f * i <= T3; i++)
            {
                float t = 0.05f * i;
                float r = (0.5f * acc * T2 * T2 + (acc * T2 * (t - T2) - 0.5f * acc * (t - T2) * (t - T2))) / dtheta;
                double result = DLerp.dLerp(start, end, r);
                joints.Add(result);
            }
            return joints;
        }

        public static List<double> JointProfile2(float start, float end, float vel, float acc)
        {
            List<double> joints = new List<double>();
            float dtheta = Mathf.Abs(end - start);

            float T1 = vel / acc;
            float T2 = dtheta / vel;
            float T3 = T1 + T2;

            float remainder = T3 % 0.05f;

            int i;
            for (i = 0; 0.05f * i <= T1; i++)
            {
                float t = 0.05f * i;
                float r = (0.5f * acc * t * t) / dtheta;
                double result = DLerp.dLerp(start, end, r);
                joints.Add(result);
            }
            for (; 0.05f * i <= T2; i++)
            {
                float t = 0.05f * i;
                float r = (0.5f * acc * T1 * T1 + vel * (t - T1)) / dtheta;
                double result = DLerp.dLerp(start, end, r);
                joints.Add(result);
            }
            for (; 0.05f * i <= T3; i++)
            {
                float t = 0.05f * i;
                float r = (0.5f * acc * T1 * T1 + vel * (T2 - T1) + (vel * (t - T2) - 0.5f * acc * (t - T2) * (t - T2))) / dtheta;
                double result = DLerp.dLerp(start, end, r);
                joints.Add(result);
            }

            if (remainder > 0)
            {
                joints.Add(end);
            }

            return joints;
        }

    }
}
