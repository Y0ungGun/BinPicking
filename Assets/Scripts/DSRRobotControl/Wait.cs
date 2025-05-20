using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
namespace DSRRobotControl
{
    public class Wait : Command
    {
        /// <summary>
        /// Lerp Method: Add last Joint Values for corresponding time.
        /// </summary>
        /// <param name="jointArr">The list containing interpolated joint values. (Deg)</param>
        /// <param name="EndEffector">The end effector of the robot.</param>
        /// <param name="link1">The first link of the robot to control its joints.</param>
        public override void ExecuteCommand(List<double> jointArr, ArticulationBody EndEffector, ArticulationBody link1)
        {
            // Debug.Log("Wait Executed");
            float time = this.time;
            int num = (int)(time / 0.05f);

            List<double> currentJ = GetCurrentJ(link1, jointArr);

            for (int i=0; i < num; i++)
            {
                foreach (double value in currentJ)
                {
                    jointArr.Add(value);
                }
            }
            frame = num;
        }
    }
}