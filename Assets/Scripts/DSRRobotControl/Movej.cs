using MathNet.Numerics.Distributions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

namespace DSRRobotControl
{
    public class Movej : Command
    {
        /// <summary>
        /// Lerp Method: Linear Interpolation in Joint Space.
        /// </summary>
        /// <param name="jointArr">The list containing interpolated joint values. (Deg)</param>
        /// <param name="EndEffector">The end effector of the robot.</param>
        /// <param name="link1">The first link of the robot to control its joints.</param>
        public override void ExecuteCommand(List<double> jointArr, ArticulationBody EndEffector, ArticulationBody link1)
        {
            // Debug.Log("MoveJ Executed.");
            // Update Param
            /*if (velocity.Length == 1)
            {
                velocity = new float[] { velocity[0], velocity[0], velocity[0], velocity[0], velocity[0], velocity[0] };
            }
            if (acceleration.Length == 1)
            {
                acceleration = new float[] { acceleration[0], acceleration[0], acceleration[0], acceleration[0], acceleration[0], acceleration[0] };
            }*/


            // JointPositions
            List<double> currentJ = GetCurrentJ(link1, jointArr);

            List<double> jointStart = new List<double>{
            currentJ[0],
            currentJ[1],
            currentJ[2],
            currentJ[3],
            currentJ[4],
            currentJ[5]};

            double[] jointEnd = new double[] { this.desiredPosition[0], this.desiredPosition[1], this.desiredPosition[2], this.desiredPosition[3], this.desiredPosition[4], this.desiredPosition[5] };

            double[] Dtheta = new double[] { 
                Mathf.Abs((float)(desiredPosition[0] - currentJ[0])), 
                Mathf.Abs((float)(desiredPosition[1] - currentJ[1])),
                Mathf.Abs((float)(desiredPosition[2] - currentJ[2])),
                Mathf.Abs((float)(desiredPosition[3] - currentJ[3])),
                Mathf.Abs((float)(desiredPosition[4] - currentJ[4])),
                Mathf.Abs((float)(desiredPosition[5] - currentJ[5])),
            };

            if (time != 0.0)
            {
                UpdateParam1(Dtheta);
            }
            else
            {
                UpdateParam2(Dtheta);
            }

            List<double> joint0 = LerpInterpolation.JointInterpolation((float)jointStart[0], (float)jointEnd[0], this.velocity[0], this.acceleration[0], time);
            List<double> joint1 = LerpInterpolation.JointInterpolation((float)jointStart[1], (float)jointEnd[1], this.velocity[1], this.acceleration[1], time);
            List<double> joint2 = LerpInterpolation.JointInterpolation((float)jointStart[2], (float)jointEnd[2], this.velocity[2], this.acceleration[2], time);
            List<double> joint3 = LerpInterpolation.JointInterpolation((float)jointStart[3], (float)jointEnd[3], this.velocity[3], this.acceleration[3], time);
            List<double> joint4 = LerpInterpolation.JointInterpolation((float)jointStart[4], (float)jointEnd[4], this.velocity[4], this.acceleration[4], time);
            List<double> joint5 = LerpInterpolation.JointInterpolation((float)jointStart[5], (float)jointEnd[5], this.velocity[5], this.acceleration[5], time);

            int num = Mathf.Max(joint0.Count, joint1.Count, joint2.Count, joint3.Count, joint4.Count, joint5.Count);

            ExtendList.extendList(joint0, num);
            ExtendList.extendList(joint1, num);
            ExtendList.extendList(joint2, num);
            ExtendList.extendList(joint3, num);
            ExtendList.extendList(joint4, num);
            ExtendList.extendList(joint5, num);

            for (int i = 0; i < num; i++)
            {
                jointArr.Add(joint0[i]);
                jointArr.Add(joint1[i]);
                jointArr.Add(joint2[i]);
                jointArr.Add(joint3[i]);
                jointArr.Add(joint4[i]);
                jointArr.Add(joint5[i]);
            }
            frame = num;
        }
        /// <summary>
        /// Updates Parameters according to the time.
        /// </summary>
        /// <param name="Dtheta">Absolute Difference between each Joint Value (Deg)</param>
        public void UpdateParam1(double[] Dtheta)
        {
            if (time >= 1)
            {
                for (int i =0; i < 6; i++)
                {
                    velocity[i] = (float)(Dtheta[i] / (time - 0.5));
                    acceleration[i] = (float)(2.0 * velocity[i]);
                }
            }
        }
        /// <summary>
        /// Updates Parameters according to the Maximum Joint Velocity.
        /// </summary>
        /// <param name="Dtheta">Absolute Difference between each Joint Value (Deg)</param>
        public void UpdateParam2(double[] Dtheta)
        {
            if (velocity.Length == 1)
            {
                float accTime = velocity[0] / acceleration[0];
                time = accTime + (float)Dtheta.Max() / velocity[0];

                float[] updateVel = new float[] { 0, 0, 0, 0, 0, 0 };
                float[] updateAcc = new float[] { 0, 0, 0, 0, 0, 0 };
                for (int i = 0; i < 6; i++)
                {
                    updateVel[i] = (float)(Dtheta[i] / (time - accTime));
                    updateAcc[i] = updateVel[i] / accTime;
                }
                velocity = updateVel;
                acceleration = updateAcc;
            }
            else
            {
                double[] times = new double[6];
                for (int i = 0; i < 6; i++)
                {
                    times[i] = velocity[i] / acceleration[i] + Dtheta[i] / velocity[i];
                }
                int maxIndex = Array.IndexOf(times, times.Max());
                time = (float)times[maxIndex];
                switch (maxIndex)
                {
                    case 0:
                        for (int i = 0; i < 6; i++)
                        {
                            velocity[i] = (float)(Dtheta[i] * velocity[0] / Dtheta[0]);
                            acceleration[i] = velocity[i] * acceleration[0] / velocity[0];
                        }
                        break;
                    case 1:
                        for (int i = 0; i < 6; i++)
                        {
                            velocity[i] = (float)(Dtheta[i] * velocity[1] / Dtheta[1]);
                            acceleration[i] = velocity[i] * acceleration[1] / velocity[1];
                        }
                        break;
                    case 2:
                        for (int i = 0; i < 6; i++)
                        {
                            velocity[i] = (float)(Dtheta[i] * velocity[2] / Dtheta[2]);
                            acceleration[i] = velocity[i] * acceleration[2] / velocity[2];
                        }
                        break;
                    case 3:
                        for (int i = 0; i < 6; i++)
                        {
                            velocity[i] = (float)(Dtheta[i] * velocity[3] / Dtheta[3]);
                            acceleration[i] = velocity[i] * acceleration[3] / velocity[3];
                        }
                        break;
                    case 4:
                        for (int i = 0; i < 6; i++)
                        {
                            velocity[i] = (float)(Dtheta[i] * velocity[4] / Dtheta[4]);
                            acceleration[i] = velocity[i] * acceleration[4] / velocity[4];
                        }
                        break;
                    case 5:
                        for (int i = 0; i < 6; i++)
                        {
                            velocity[i] = (float)(Dtheta[i] * velocity[5] / Dtheta[5]);
                            acceleration[i] = velocity[i] * acceleration[5] / velocity[5];
                        }
                        break;

                }
            }
        }
    }
}
