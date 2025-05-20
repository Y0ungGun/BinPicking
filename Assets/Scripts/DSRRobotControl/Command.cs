using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace DSRRobotControl
{
    [System.Serializable]
    
    public class Command
    {
        public string command;
        public float[] desiredPosition;
        public float[] velocity;
        public float[] acceleration;
        public float radius;
        public int mod;
        public int ra;
        public int reference;
        public float time;
        public int frame;
        public int index;
        public bool value;

        /// <summary>
        /// Return jointArr containing Joint Values according to each commands' Lerp method.
        /// </summary>
        /// <param name="jointArr">The list containing interpolated joint values. (Rad)</param>
        /// <param name="EndEffector">The end effector of the robot.</param>
        /// <param name="link1">The first link of the robot to control its joints.</param>
        public virtual void ExecuteCommand(List<double> jointArr, ArticulationBody EndEffector, ArticulationBody link1)
        {
            
        }
        /// <summary>
        /// Returns the joint values at the moment the command is executed. (Rad)
        /// </summary>
        /// <param name="link1">The first link of the robot to control its joints.</param>
        /// <param name="jointArr">The list containing all joint positions. (Rad)</param>
        /// <returns>A list of current joint positions, converted to radians.</returns>
        public List<double> GetCurrentJ(ArticulationBody link1, List<double> jointArr)
        {
            List<double> currentJ = new List<double>();
            if (jointArr.Count >= 6)
            {
                currentJ = jointArr.GetRange(jointArr.Count - 6, 6);

                if (currentJ.Any(v => double.IsNaN(v)))
                {
                    // NaN이 아닌 값을 찾아 반환하도록 하는 recursive part입니다.
                    // 문제가 발생할 경우 해당 부분을 확인하세요.
                    return GetCurrentJ(link1, jointArr.Take(jointArr.Count - 6).ToList());
                }
            }
            else
            {
                List<float> currentJFloat = new List<float>();
                link1.GetJointPositions(currentJFloat);
                currentJ = currentJFloat.Select(x => (double)x).ToList();
            }
            return currentJ;
        }
        /// <summary>
        /// Returns the 6D Pose of end effector(R, P, Y) at the moment the command is executed. (m, rad)
        /// </summary>
        /// <param name="EndEffector">The end effector of the robot.</param>
        /// <param name="jointArr">The list containing all joint positions. (Rad)</param>
        /// <returns>A list of current joint positions, converted to radians.</returns>
        public List<double> GetCurrentP(ArticulationBody link1, ArticulationBody EndEffector, List<double> jointArr)
        {
            List<double> currentP = new List<double>();
            List<double> currentJ = new List<double>();
            string urdfPath = Path.Combine(Application.dataPath, "Scripts/DSRRobotControl/urdf/m1509.urdf");
            ForwardKinematics FK = new ForwardKinematics(urdfPath);

            currentJ = GetCurrentJ(link1, jointArr);

            for (int i = 0; i < 6; i++)
            {
                FK.JointAngles[i] = currentJ[i];
            }

            FK.ForwardKinematicsSolver();
            currentP = FK.ForwardKinematicsResult();

            return currentP;
        }

    }
}
