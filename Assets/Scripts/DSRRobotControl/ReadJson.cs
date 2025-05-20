using System.Collections;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DSRRobotControl
{
    public class ReadJson
    {
        public CommandList commandList;
        public string jsonString;
        
        public ReadJson(CommandList commandList)
        {
            this.commandList = commandList;
        }
    

        public CommandList ParseCommandList()
        {
            CommandListWrapper commandListWrapper = JsonUtility.FromJson<CommandListWrapper>(jsonString);

            List<Command> commands = new List<Command>();

            foreach (var commandData in commandListWrapper.commands)
            {
                Command command = new Command();
                switch (commandData.command)
                {
                    case "movel":
                        command = new Movel()
                        {
                            command = commandData.command,
                            desiredPosition = Array.ConvertAll(commandData.desiredPosition.Split(','), float.Parse),
                            velocity = Array.ConvertAll(commandData.velocity.Split(','), float.Parse),
                            acceleration = Array.ConvertAll(commandData.acceleration.Split(','), float.Parse),
                            time = float.Parse(commandData.time),
                            // radius = float.Parse(commandData.radius),
                            // mod = int.Parse(commandData.mod),
                            // ra = int.Parse(commandData.ra),
                            reference = (commandData.reference == "base") ? 0 : (commandData.reference == "tool") ? 1 : -1
                        };
                        commands.Add(command);
                        break;
                    case "movej":
                        command = new Movej()
                        {
                            command = commandData.command,
                            desiredPosition = Array.ConvertAll(commandData.desiredPosition.Split(','), float.Parse),
                            velocity = Array.ConvertAll(commandData.velocity.Split(','), float.Parse),
                            acceleration = Array.ConvertAll(commandData.acceleration.Split(','), float.Parse),
                            time = float.Parse(commandData.time)
                            // radius = float.Parse(commandData.radius),
                            // mod = int.Parse(commandData.mod),
                            // ra = int.Parse(commandData.ra)
                        };
                        commands.Add(command);
                        break;
                    case "wait":
                        command = new Wait()
                        {
                            command = commandData.command,
                            desiredPosition = Array.ConvertAll(commandData.desiredPosition.Split(','), float.Parse),
                            velocity = Array.ConvertAll(commandData.velocity.Split(','), float.Parse),
                            acceleration = Array.ConvertAll(commandData.acceleration.Split(','), float.Parse),
                            time = float.Parse(commandData.time)
                        };
                        commands.Add(command);
                        break;
                    case "wait_digital_input":
                        command = new Wait_Digital_Input()
                        {
                            command = commandData.command,
                            index = int.Parse(commandData.index),
                            value = commandData.value.Equals("ON", StringComparison.OrdinalIgnoreCase)
                        };
                        commands.Add(command);
                        break;
                    case "set_digital_output":
                        command = new Set_Digital_Output()
                        {
                            command = commandData.command,
                            index = int.Parse(commandData.index),
                            value = commandData.value.Equals("ON", StringComparison.OrdinalIgnoreCase)
                        };
                        commands.Add(command);
                        break;
                }
            }
            return new CommandList
            {
                commands = commands
            };
        }
    }


    [Serializable]
    public class CommandData
    {
        public string command;
        public string desiredPosition;
        public string velocity;
        public string acceleration;
        public string time;
        // public string radius;
        public string reference;
        public string mod;
        // public string ra;
        public string index;
        public string value;
    }

    [Serializable]
    public class CommandListWrapper
    {
        public List<CommandData> commands;
    }

}
