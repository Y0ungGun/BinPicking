using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using Unity.VisualScripting;
using MathNet.Numerics.LinearAlgebra.Double;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;
using Matrix = MathNet.Numerics.LinearAlgebra.Matrix<double>;
using System.Linq;
using System;

namespace DSRRobotControl
{
    public class Movel : Command
    {
        /// <summary>
        /// Lerp Method: Linear Interpolation in Cartesian Space with IK.
        /// </summary>
        /// <param name="jointArr">The list containing interpolated joint values. (Deg)</param>
        /// <param name="EndEffector">The end effector of the robot.</param>
        /// <param name="link1">The first link of the robot to control its joints.</param>
        public override void ExecuteCommand(List<double> jointArr, ArticulationBody EndEffector, ArticulationBody link1)
        {
            string pth = Path.Combine(Application.dataPath, $"Log/IKLog{DateTime.Now:yyMMdd_HHmm}.csv");
            // 또는 Assets 폴더 기준 상위 폴더로 이동하려면:
            // Debug.Log("MoveL Executed.");
            List<double> currentP = GetCurrentP(link1, EndEffector, jointArr);
            Vector3 POSE0 = new Vector3((float)currentP[0], (float)currentP[1], (float)currentP[2]);
            Vector3 RPY0 = new Vector3((float)currentP[3], (float)currentP[4], (float)currentP[5]);
            Quaternion quat0 = RPY2Quat.rpy2Quat(RPY0);


            // Debug.Log("Pose0: " + string.Join(", ", Pos0) + string.Join(", ", ZYX0));
            Vector3 desiredPos = new Vector3(desiredPosition[0], desiredPosition[1], desiredPosition[2]);
            Vector3 desiredZYZ = new Vector3(desiredPosition[3], desiredPosition[4], desiredPosition[5]);
            Quaternion desiredQuat = Euler2Quat.ZYZ2Quat(desiredZYZ);
            
            if (reference == 1)
            {
                Vector3 localoffset = new Vector3(0, 0, -0.20625f);
                Vector3 worldoffset = desiredQuat * localoffset;

                desiredPos = desiredPos + worldoffset;
            }

            float dist = Vector3.Distance(POSE0, desiredPos); 
            float theta = Quaternion.Angle(quat0, desiredQuat) * Mathf.PI/180;

            if (time != 0.0)
            {
                UpdateParam1(dist, theta);
            }
            else
            {
                UpdateParam2(dist, theta);
            }

            List<Vector3> poses = LerpInterpolation.PoseInterpolation2(POSE0, desiredPos, velocity[0], acceleration[0], time);
            List<Quaternion> quaternions = LerpInterpolation.QuatInterpolation2(quat0, desiredQuat, velocity[1], acceleration[1], time);

            List<double> IKResult = new List<double>();

            List<double> currentJ = GetCurrentJ(link1, jointArr);
            Vector currentJoint = DenseVector.OfArray(new double[] { currentJ[0], currentJ[1], currentJ[2], currentJ[3], currentJ[4], currentJ[5] });
            InverseKinematics IK0 = new InverseKinematics();
            int solspace = IK0.getSingleSolutionSpace(currentJoint);

            for (int k = 0; k < poses.Count; k++)
            {
                Vector3 lerpedZYZ = Quat2Euler.EulerFromQuat(quaternions[k], "zyz", false);
                Vector desiredPose = DenseVector.OfArray(new double[] { poses[k][0], poses[k][1], poses[k][2], lerpedZYZ[0], lerpedZYZ[1], lerpedZYZ[2] });

                InverseKinematics IK;
                List<double> res;
                try
                {
                    IK = new InverseKinematics(desiredPose, solspace);
                    res = IK.InverseKinematicsResult();

                    if (DSRExecutor.writeCSV)
                    {
                        Matrix all = IK.getAllSolutions();
                        List<int> spaces = IK.getAllSpace();
                        string input = string.Format("INPUT,{0},{1},{2},{3},{4},{5},K,{6}\n", desiredPose[0], desiredPose[1], desiredPose[2], desiredPose[3], desiredPose[4], desiredPose[5], k);
                        File.AppendAllText(pth, input);
                        string input0 = string.Format("SOL1,{0},{1},{2},{3},{4},{5},SolSpace,{6}\n", all[0, 0], all[1, 0], all[2, 0], all[3, 0], all[4, 0], all[5, 0], spaces[0]);
                        File.AppendAllText(pth, input0);
                        string input1 = string.Format("SOL2,{0},{1},{2},{3},{4},{5},SolSpace,{6}\n", all[0, 1], all[1, 1], all[2, 1], all[3, 1], all[4, 1], all[5, 1], spaces[1]);
                        File.AppendAllText(pth, input1);
                        string input2 = string.Format("SOL3,{0},{1},{2},{3},{4},{5},SolSpace,{6}\n", all[0, 2], all[1, 2], all[2, 2], all[3, 2], all[4, 2], all[5, 2], spaces[2]);
                        File.AppendAllText(pth, input2);
                        string input3 = string.Format("SOL4,{0},{1},{2},{3},{4},{5},SolSpace,{6}\n", all[0, 3], all[1, 3], all[2, 3], all[3, 3], all[4, 3], all[5, 3], spaces[3]);
                        File.AppendAllText(pth, input3);
                        string input4 = string.Format("SOL5,{0},{1},{2},{3},{4},{5},SolSpace,{6}\n", all[0, 4], all[1, 4], all[2, 4], all[3, 4], all[4, 4], all[5, 4], spaces[4]);
                        File.AppendAllText(pth, input4);
                        string input5 = string.Format("SOL6,{0},{1},{2},{3},{4},{5},SolSpace,{6}\n", all[0, 5], all[1, 5], all[2, 5], all[3, 5], all[4, 5], all[5, 5], spaces[5]);
                        File.AppendAllText(pth, input5);
                        string input6 = string.Format("SOL7,{0},{1},{2},{3},{4},{5},SolSpace,{6}\n", all[0, 6], all[1, 6], all[2, 6], all[3, 6], all[4, 6], all[5, 6], spaces[6]);
                        File.AppendAllText(pth, input6);
                        string input7 = string.Format("SOL8,{0},{1},{2},{3},{4},{5},SolSpace,{6}\n", all[0, 7], all[1, 7], all[2, 7], all[3, 7], all[4, 7], all[5, 7], spaces[7]);
                        File.AppendAllText(pth, input7);

                        string output = string.Format("OUTPUT,{0},{1},{2},{3},{4},{5}\n\n", res[0], res[1], res[2], res[3], res[4], res[5]);
                        File.AppendAllText(pth, output);
                    }
                }
                catch
                {
                    res = new List<double>();
                    for (int i = 0; i < 6; i++)
                    {
                        res.Add(double.NaN);
                    }
                    string input = string.Format("INPUT,{0},{1},{2},{3},{4},{5},K,{6}\n", desiredPose[0], desiredPose[1], desiredPose[2], desiredPose[3], desiredPose[4], desiredPose[5], k);
                    File.AppendAllText(pth, input);
                    string output = string.Format("OUTPUT,{0},{1},{2},{3},{4},{5}\n\n", double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);
                    File.AppendAllText(pth, output);
                }

                foreach (double value in res)
                {
                    jointArr.Add(value);
                }
            }
            frame = poses.Count;
        }

        /// <summary>
        /// Updates Parameters according to the time.
        /// </summary>
        /// <param name="dist">Euclidean distance between current(x, y, z), target(x, y, z). (mm)</param>
        /// <param name="theta">Euclidean distance between current(rx, ry, rz), target(rx, ry, rz). (deg)</param>
        public void UpdateParam1(float dist, float theta)
        {
            if (time >= 1)
            {
                float[] updateVel = new float[] { (float)(dist / (time - 0.5)), (float)(theta / (time - 0.5)) };
                float[] updateAcc = new float[] { (float)(2.0 * updateVel[0]), (float)(2.0 * updateVel[1]) };

                velocity = updateVel;
                acceleration = updateAcc;
            }
        }
        /// <summary>
        /// Updates Parameters according to the Maximum Joint Velocity.
        /// </summary>
        /// <param name="dist">Euclidean distance between current(x, y, z), target(x, y, z). (mm)</param>
        /// <param name="theta">Euclidean distance between current(rx, ry, rz), target(rx, ry, rz). (deg)</param>
        public void UpdateParam2(float dist, float theta)
        {
            if (velocity.Length == 1)
            {
                float[] updateVel = new float[] { velocity[0], theta * velocity[0] / dist };
                float[] updateAcc = new float[] { acceleration[0], updateVel[1] * acceleration[0] / velocity[0] };

                velocity = updateVel;
                acceleration = updateAcc;
                time = velocity[0] / acceleration[0] + dist / velocity[0];
            }
            else
            {
                float time1 = velocity[0] / acceleration[0] + dist / velocity[0];
                float time2 = velocity[1] / acceleration[1] + theta / velocity[1];
                if (time1 >= time2)
                {
                    velocity[1] = theta * velocity[0] / dist;
                    acceleration[1] = velocity[1] * acceleration[0] / velocity[0];
                    time = time1;
                }
                else
                {
                    velocity[0] = dist * velocity[1] / theta;
                    acceleration[0] = velocity[0] * acceleration[1] / velocity[1];
                    time = time2;
                }
            }
        }
    }
}
