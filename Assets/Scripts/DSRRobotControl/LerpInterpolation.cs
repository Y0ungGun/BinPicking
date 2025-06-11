using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra.Double;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;

namespace DSRRobotControl
{
    public class LerpInterpolation
    {
        public static List<Vector3> PoseInterpolation(Vector3 start, Vector3 end, float vel, float acc, float time)
        {
            List<Vector3> pose = new List<Vector3>();
            float dist = Vector3.Distance(start, end);
            if (dist == 0)
            {
                for (int dummy = 0; dummy <= time / 0.1f; dummy++)
                {
                    pose.Add(start);
                }
            }
            float T1 = vel / acc;
            float T2 = dist / vel;
            float T3 = T1 + T2;

            float remainder = T3 % 0.1f;

            int i;
            for (i = 0; 0.1f * i <= T1; i++)
            {
                float t = 0.1f * i;
                float r = (0.5f * acc * t * t) / dist;
                Vector3 result = Vector3.Lerp(start, end, r);
                pose.Add(result);
            }
            for (; 0.1f * i <= T2; i++)
            {
                float t = 0.1f * i;
                float r = (0.5f * acc * T1 * T1 + vel * (t - T1)) / dist;
                Vector3 result = Vector3.Lerp(start, end, r);
                pose.Add(result);
            }
            for (; 0.1f * i <= T3; i++)
            {
                float t = 0.1f * i;
                float r = (0.5f * acc * T1 * T1 + vel * (T2 - T1) + (vel * (t - T2) - 0.5f * acc * (t - T2) * (t - T2))) / dist;
                Vector3 result = Vector3.Lerp(start, end, r);
                pose.Add(result);
            }

            if (remainder > 0)
            {
                pose.Add(end);
            }

            return pose;
        }
        public static List<Vector3> PoseInterpolation2(Vector3 start, Vector3 end, float vel, float acc, float time)
        {
            List<Vector3> pose = new List<Vector3>();
            float dist = Vector3.Distance(start, end);
            float T1 = vel / acc;
            if (dist == 0)
            {
                for (int dummy = 0; dummy <= time / 0.1f; dummy++)
                {
                    pose.Add(start);
                }
                return pose;
            }

            if (dist <= T1 * vel)
            {
                pose = PoseProfile1(start, end, vel, acc);
            }
            else
            {
                pose = PoseProfile2(start, end, vel, acc);
            }
            return pose;
        }

        public static List<Quaternion> QuatInterpolation(Quaternion start, Quaternion end, float vel, float acc, float time)
        {
            List<Quaternion> quaternions = new List<Quaternion>();
            float theta = Quaternion.Angle(start, end) * Mathf.PI / 180;

            if (theta == 0)
            {
                for (int dummy = 0; dummy <= time / 0.1f + 1; dummy++)
                {
                    quaternions.Add(start);
                }
            }
            float T1 = vel / acc;
            float T2 = theta / vel;
            float T3 = T1 + T2;
            float remainder = T3 % 0.1f;

            int i;
            for (i = 0; 0.1f * i <= T1; i++)
            {
                float t = 0.1f * i;
                float r = (0.5f * acc * t * t) / theta;
                Quaternion result = Quaternion.Slerp(start, end, r);
                quaternions.Add(result);
            }
            for (; 0.1f * i <= T2; i++)
            {
                float t = 0.1f * i;
                float r = (0.5f * acc * T1 * T1 + vel * (t - T1)) / theta;
                Quaternion reult = Quaternion.Slerp(start, end, r);
                quaternions.Add(reult);
            }
            for (; 0.1f * i <= T3; i++)
            {
                float t = 0.1f * i;
                float r = (0.5f * acc * T1 * T1 + vel * (T2 - T1) + (vel * (t - T2) - 0.5f * acc * (t - T2) * (t - T2))) / theta;
                Quaternion result = Quaternion.Slerp(start, end, r);
                quaternions.Add(result);
            }

            if (remainder > 0)
            {
                quaternions.Add(end);
            }

            return quaternions;
        }
        public static List<Quaternion> QuatInterpolation2(Quaternion start, Quaternion end, float vel, float acc, float time)
        {
            List<Quaternion> quaternions = new List<Quaternion>();
            float theta = Quaternion.Angle(start, end) * Mathf.PI / 180;
            float T1 = vel / acc;

            if (theta == 0)
            {
                for (int dummy = 0; dummy <= time / 0.1f + 1; dummy++)
                {
                    quaternions.Add(start);
                }
                return quaternions;
            }

            if (theta <= T1 * vel)
            {
                quaternions = QuatProfile1(start, end, vel, acc);
            }
            else
            {
                quaternions = QuatProfile2(start, end, vel, acc);
            }
            return quaternions;
        }
        
        public static List<double>JointInterpolation(float start, float end, float vel, float acc, float time)
        {
            List<double> joints = new List<double>();
            float dtheta = Mathf.Abs(end - start);
            float T1 = vel / acc;

            if (dtheta == 0)
            {
                for (int dummy = 0; dummy <= time / 0.1f + 1; dummy++)
                {
                    joints.Add(start);
                }
                return joints;
            }

            if (dtheta <= T1 * vel)
            {
                joints = JointProfile1(start, end, vel, acc);
            }
            else
            {
                joints = JointProfile2(start, end, vel, acc); 
            }
            return joints;
        }
        public static List<double> JointProfile1(float start, float end, float vel, float acc)
        {
            List<double> joints = new List<double>();
            float dtheta = Mathf.Abs(end - start);
            float T1 = vel / acc;
            float T2 = Mathf.Sqrt(dtheta / acc);
            float T3 = 2 * T2; 
        
            int i;
            for (i = 0; 0.1f * i <= T2; i++)
            {
                float t = 0.1f * i;
                float r = (0.5f * acc * t * t) / dtheta;
                double result = DLerp.dLerp(start, end, r);
                joints.Add(result);
            }
            for (; 0.1f * i <= T3; i++)
            {
                float t = 0.1f * i;
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

            float remainder = T3 % 0.1f;

            int i;
            for (i = 0; 0.1f * i <= T1; i++)
            {
                float t = 0.1f * i;
                float r = (0.5f * acc * t * t) / dtheta;
                double result = DLerp.dLerp(start, end, r);
                joints.Add(result);
            }
            for (; 0.1f * i <= T2; i++)
            {
                float t = 0.1f * i;
                float r = (0.5f * acc * T1 * T1 + vel * (t - T1)) / dtheta;
                double result = DLerp.dLerp(start, end, r);
                joints.Add(result);
            }
            for (; 0.1f * i <= T3; i++)
            {
                float t = 0.1f * i;
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

        public static List<Quaternion> QuatProfile1(Quaternion start, Quaternion end, float vel, float acc)
        {
            List<Quaternion> quats = new List<Quaternion>();
            float theta = Quaternion.Angle(start, end) * Mathf.PI / 180;
            float T1 = vel / acc;
            float T2 = Mathf.Sqrt(theta / acc);
            float T3 = 2 * T2;

            int i;
            for (i = 0; 0.1f * i <= T2; i++)
            {
                float t = 0.1f * i;
                float r = (0.5f * acc * t * t) / theta;
                Quaternion result = Quaternion.Slerp(start, end, r);
                quats.Add(result);
            }
            for (; 0.1f * i <= T3; i++)
            {
                float t = 0.1f * i;
                float r = (0.5f * acc * T2 * T2 + (acc * T2 * (t - T2) - 0.5f * acc * (t - T2) * (t - T2))) / theta;
                Quaternion result = Quaternion.Slerp(start, end, r);
                quats.Add(result);
            }
            return quats;
        }

        public static List<Quaternion> QuatProfile2(Quaternion start, Quaternion end, float vel, float acc)
        {
            List<Quaternion> quats = new List<Quaternion>();
            float theta = Quaternion.Angle(start, end) * Mathf.PI / 180;

            float T1 = vel / acc;
            float T2 = theta / vel;
            float T3 = T1 + T2;

            float remainder = T3 % 0.1f;

            int i;
            for (i = 0; 0.1f * i <= T1; i++)
            {
                float t = 0.1f * i;
                float r = (0.5f * acc * t * t) / theta;
                Quaternion result = Quaternion.Slerp(start, end, r);
                quats.Add(result);
            }
            for (; 0.1f * i <= T2; i++)
            {
                float t = 0.1f * i;
                float r = (0.5f * acc * T1 * T1 + vel * (t - T1)) / theta;
                Quaternion result = Quaternion.Slerp(start, end, r);
                quats.Add(result);
            }
            for (; 0.1f * i <= T3; i++)
            {
                float t = 0.1f * i;
                float r = (0.5f * acc * T1 * T1 + vel * (T2 - T1) + (vel * (t - T2) - 0.5f * acc * (t - T2) * (t - T2))) / theta;
                Quaternion result = Quaternion.Slerp(start, end, r);
                quats.Add(result);
            }

            if (remainder > 0)
            {
                quats.Add(end);
            }

            return quats;
        }

        public static List<Vector3> PoseProfile1(Vector3 start, Vector3 end, float vel, float acc)
        {
            List<Vector3> pose = new List<Vector3>();
            float dist = Vector3.Distance(start, end);
            float T1 = vel / acc;
            float T2 = Mathf.Sqrt(dist / acc);
            float T3 = 2 * T2;

            int i;
            for (i = 0; 0.1f * i <= T2; i++)
            {
                float t = 0.1f * i;
                float r = (0.5f * acc * t * t) / dist;
                Vector3 result = Vector3.Lerp(start, end, r);
                pose.Add(result);
            }
            for (; 0.1f * i <= T3; i++)
            {
                float t = 0.1f * i;
                float r = (0.5f * acc * T2 * T2 + (acc * T2 * (t - T2) - 0.5f * acc * (t - T2) * (t - T2))) / dist;
                Vector3 result = Vector3.Lerp(start, end, r);
                pose.Add(result);
            }
            return pose;
        }

        public static List<Vector3> PoseProfile2(Vector3 start, Vector3 end, float vel, float acc)
        {
            List<Vector3> pose = new List<Vector3>();
            float dist = Vector3.Distance(start, end);

            float T1 = vel / acc;
            float T2 = dist / vel;
            float T3 = T1 + T2;

            float remainder = T3 % 0.1f;

            int i;
            for (i = 0; 0.1f * i <= T1; i++)
            {
                float t = 0.1f * i;
                float r = (0.5f * acc * t * t) / dist;
                Vector3 result = Vector3.Lerp(start, end, r);
                pose.Add(result);
            }
            for (; 0.1f * i <= T2; i++)
            {
                float t = 0.1f * i;
                float r = (0.5f * acc * T1 * T1 + vel * (t - T1)) / dist;
                Vector3 result = Vector3.Lerp(start, end, r);
                pose.Add(result);
            }
            for (; 0.1f * i <= T3; i++)
            {
                float t = 0.1f * i;
                float r = (0.5f * acc * T1 * T1 + vel * (T2 - T1) + (vel * (t - T2) - 0.5f * acc * (t - T2) * (t - T2))) / dist;
                Vector3 result = Vector3.Lerp(start, end, r);
                pose.Add(result);
            }

            if (remainder > 0)
            {
                pose.Add(end);
            }

            return pose;
        }
    }

}
