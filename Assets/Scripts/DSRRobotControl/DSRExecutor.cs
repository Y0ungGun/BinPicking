using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Reflection;


namespace DSRRobotControl
{
    public class DSRExecutor : MonoBehaviour
    {
        public ArticulationBody link1;
        public ArticulationBody link2;
        public ArticulationBody link3;
        public ArticulationBody link4;
        public ArticulationBody link5;
        public ArticulationBody link6;

        public float j1;
        public float j2;
        public float j3;
        public float j4;
        public float j5;
        public float j6;

        public static int moveIndex = 0;

        private List<float> jointPositions;
        public TextAsset json;

        public bool CSV = false;
        public static bool writeCSV = false;

        public bool DI1 = false;
        public bool DO1 = false;
        protected IEnumerator Start()
        {
            writeCSV = CSV;
            
            StartwithJointValue();

            yield return new WaitForSeconds(1f);

            CommandList commandList = new CommandList();
            ReadJson jsonReader = new ReadJson(commandList);
            jsonReader.jsonString = json.ToString();
            commandList = jsonReader.ParseCommandList();

            List<double> jointArr = commandList.ExecuteCommands(link6, link1);

            
            string timestamp = DateTime.Now.ToString("yyMMdd_HH_mm");
            string Path2 = $"C://Users/dudrj/unityworkspace/DSR_VirtualCommission/Assets/Log/{timestamp}_JointValueLog.csv";
            for (int i = 0; i < jointArr.Count/6; i++)
            {
                string joint = string.Format("{0},{1},{2},{3},{4},{5}\n", jointArr[6 * i + 0], jointArr[6 * i + 1], jointArr[6 * i + 2], jointArr[6 * i + 3], jointArr[6 * i + 4], jointArr[6 * i + 5]);
                if (writeCSV) { File.AppendAllText(Path2, joint); }
            }

            StartCoroutine(MoveRobot(jointArr, commandList, CommandList.frames));

        }

        public IEnumerator MoveRobot(List<double> jointArr, CommandList commandList, List<int> frames)
        {
            moveIndex = 0;
            for (int i = 0; i < commandList.commands.Count; i++)
            {
                for (; moveIndex < frames[i]; moveIndex++)
                {
                    if (commandList.commands[i].command == "wait_digital_input")
                    {
                        while (DI1 != commandList.commands[i].value)
                        {
                            yield return null;
                        }
                    }
                    else if (commandList.commands[i].command == "set_digital_output")
                    {
                        DO1 = commandList.commands[i].value;
                    }
                    else
                    {
                        SetEachJointPositions(jointArr, moveIndex);
                        yield return new WaitForSeconds(0.05f);
                    }
                }
            }

            Debug.Log("PROGRAM FINISHED.");
        }


        /// <summary>
        /// Set Joint Positions based on jointArr(deg) , Convert Deg to Rad.
        /// </summary>
        /// <param name="jointArr">The list containing interpolated joint values. (Deg)</param>
        /// <param name="index">Frame Number(1 Frame per 0.05s) </param>
        private void SetJointPositions(List<double> jointArr, int index)
        {
            ArticulationReducedSpace joint1 = new ArticulationReducedSpace((float)jointArr[6 * index + 0]);
            link1.jointPosition = joint1;
            List<float> floatPositions = new List<float>
            {
                (float)jointArr[6*index+0],
                (float)jointArr[6*index+1],
                (float)jointArr[6*index+2],
                (float)jointArr[6*index+3],
                (float)jointArr[6*index+4],
                (float)jointArr[6*index+5]
            };
            link6.SetJointPositions(floatPositions);
        }

        private void SetEachJointPositions(List<double> jointArr, int index)
        {
            for (int i = 0; i < 6; i++)
            {
                if (double.IsNaN(jointArr[6 * index + i]))
                {
                    Debug.LogError($"Error: jointArr[{6 * index + i}] is NaN. Program will terminate.");
                    throw new InvalidOperationException("The solution is not Valid");
                }
            }

            ArticulationReducedSpace joint1 = new ArticulationReducedSpace((float)jointArr[6 * index + 0]);
            ArticulationReducedSpace joint2 = new ArticulationReducedSpace((float)jointArr[6 * index + 1]);
            ArticulationReducedSpace joint3 = new ArticulationReducedSpace((float)jointArr[6 * index + 2]);
            ArticulationReducedSpace joint4 = new ArticulationReducedSpace((float)jointArr[6 * index + 3]);
            ArticulationReducedSpace joint5 = new ArticulationReducedSpace((float)jointArr[6 * index + 4]);
            ArticulationReducedSpace joint6 = new ArticulationReducedSpace((float)jointArr[6 * index + 5]);

            ArticulationDrive drive1 = link1.xDrive;
            ArticulationDrive drive2 = link2.xDrive;
            ArticulationDrive drive3 = link3.xDrive;
            ArticulationDrive drive4 = link4.xDrive;
            ArticulationDrive drive5 = link5.xDrive;
            ArticulationDrive drive6 = link6.xDrive;

            drive1.target = (float)jointArr[6 * index + 0] * 180 / Mathf.PI;
            drive2.target = (float)jointArr[6 * index + 1] * 180 / Mathf.PI;
            drive3.target = (float)jointArr[6 * index + 2] * 180 / Mathf.PI;
            drive4.target = (float)jointArr[6 * index + 3] * 180 / Mathf.PI;
            drive5.target = (float)jointArr[6 * index + 4] * 180 / Mathf.PI;
            drive6.target = (float)jointArr[6 * index + 5] * 180 / Mathf.PI;

            link1.xDrive = drive1;
            link2.xDrive = drive2;
            link3.xDrive = drive3;
            link4.xDrive = drive4;
            link5.xDrive = drive5;
            link6.xDrive = drive6;

            //link1.jointPosition = joint1;
            //link2.jointPosition = joint2;
            //link3.jointPosition = joint3;
            //link4.jointPosition = joint4;
            //link5.jointPosition = joint5;
            //link6.jointPosition = joint6;
        }

        private void StartwithJointValue()
        {
            ArticulationReducedSpace joint1 = new ArticulationReducedSpace(j1 * 3.141592f / 180);
            ArticulationReducedSpace joint2 = new ArticulationReducedSpace(j2 * 3.141592f / 180);
            ArticulationReducedSpace joint3 = new ArticulationReducedSpace(j3 * 3.141592f / 180);
            ArticulationReducedSpace joint4 = new ArticulationReducedSpace(j4 * 3.141592f / 180);
            ArticulationReducedSpace joint5 = new ArticulationReducedSpace(j5 * 3.141592f / 180);
            ArticulationReducedSpace joint6 = new ArticulationReducedSpace(j6 * 3.141592f / 180);

            ArticulationDrive drive1 = link1.xDrive;
            ArticulationDrive drive2 = link2.xDrive;
            ArticulationDrive drive3 = link3.xDrive;
            ArticulationDrive drive4 = link4.xDrive;
            ArticulationDrive drive5 = link5.xDrive;
            ArticulationDrive drive6 = link6.xDrive;

            drive1.target = j1;
            drive2.target = j2;
            drive3.target = j3;
            drive4.target = j4;
            drive5.target = j5;
            drive6.target = j6;

            link1.xDrive = drive1;
            link2.xDrive = drive2;
            link3.xDrive = drive3;
            link4.xDrive = drive4;
            link5.xDrive = drive5;
            link6.xDrive = drive6;

            link1.jointPosition = joint1;
            link2.jointPosition = joint2;
            link3.jointPosition = joint3;
            link4.jointPosition = joint4;
            link5.jointPosition = joint5;
            link6.jointPosition = joint6;
        }
    }
}
