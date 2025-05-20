using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

namespace DSRRobotControl
{
    [System.Serializable]
    public class CommandList
    {
        public List<Command> commands = new List<Command>();
        public bool isfinished = false;
        public Command currentCommand;
        public static string currentCommandName;
        public static List<string> commandNames = new List<string>();
        public static List<string> desiredPositions = new List<string>();   
        public static string currentCommandDesiredPosition;
        public static List<int> frames = new List<int> ();
        
        /// <summary>
        /// Execute each command.ExecuteCommand. (Calculate Interpolated Joint Values.)
        /// </summary>
        /// <param name="EndEffector">The end effector of the robot.</param>
        /// <param name="link1">The first link of the robot to control its joints.</param>
        public List<double> ExecuteCommands(ArticulationBody EndEffector, ArticulationBody link1)
        {
            List<double> jointArr = new List<double>();
            foreach(Command command in commands)
            {
                command.ExecuteCommand(jointArr, EndEffector, link1);
            }
            AppendFrames(frames);

            return jointArr;
        }

        private void AppendFrames(List<int> frames)
        {
            foreach (Command command in commands)
            {
                int f;
                if (frames.Count > 0)
                {
                    f = frames[frames.Count - 1] + command.frame;
                }
                else
                {
                    f = command.frame;
                }
                commandNames.Add(command.command);
                if (command.desiredPosition != null && command.desiredPosition.Length > 0)
                {
                    desiredPositions.Add(string.Join(",", command.desiredPosition));
                }
                frames.Add(f);
            }
        }

        public void LogCommands()
        {
            foreach (var command in commands)
            {
                Debug.Log($"Command: {command.command}");
                Debug.Log($"DesiredPosition: {string.Join(",", command.desiredPosition)}");
                Debug.Log($"Velocity: {string.Join(",", command.velocity)}");
                Debug.Log($"Acceleration: {string.Join(",", command.acceleration)}");
                Debug.Log($"Time: {command.time}");
                Debug.Log($"Radius: {command.radius}");
                Debug.Log($"Mod: {command.mod}");
            }
        }
        

    }
}
