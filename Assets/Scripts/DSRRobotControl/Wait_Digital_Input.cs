using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using Unity.VisualScripting;
namespace DSRRobotControl
{
    public class Wait_Digital_Input : Command
    {
        /// <summary>
        /// Lerp Method: Add last Joint Values for corresponding time.
        /// </summary>
        /// <param name="jointArr">The list containing interpolated joint values. (Deg)</param>
        /// <param name="EndEffector">The end effector of the robot.</param>
        /// <param name="link1">The first link of the robot to control its joints.</param>
        public override void ExecuteCommand(List<double> jointArr, ArticulationBody EndEffector, ArticulationBody link1)
        {
            List<double> currentJ = GetCurrentJ(link1, jointArr);
            jointArr.Add(currentJ[0]);
            jointArr.Add(currentJ[1]);
            jointArr.Add(currentJ[2]);
            jointArr.Add(currentJ[3]);
            jointArr.Add(currentJ[4]);
            jointArr.Add(currentJ[5]);

            frame = 1;
        }
    }
}