﻿using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using TBYTEConsole.Utilities;

namespace TBYTEConsole
{
    public struct CCommand
    {
        public readonly string token;
        public readonly Func<string[], string> callback;

        public CCommand(string commandName, Func<string[], string> callback)
        {
            commandName.ThrowIfNullOrEmpty("commandName");
            callback.ThrowIfNull("callback");

            this.token = commandName; 
            this.callback = callback;
        }

        public string Execute(string[] argv)
        {
            return callback(argv);
        }
    }
    
    public static class Console
    {
        private struct ConsoleExpression
        {
            public readonly string token;
            public readonly string[] arguments;

            public ConsoleExpression(string token, string[] arguments)
            {
                this.token = token;
                this.arguments = arguments;
            }
        }

        static Console()
        {
            // register default commands
            Register(new CCommand("help", ConsoleDefaultCommands.HelpCommand));
            Register(new CCommand("clear", ConsoleDefaultCommands.ClearCommand));
            Register(new CCommand("echo", ConsoleDefaultCommands.EchoCommand));
            Register(new CCommand("list", ConsoleDefaultCommands.ListCommand));
        }

        private static Dictionary<string, CCommand> commands = new Dictionary<string, CCommand>();
        private static string consoleOutput;

        public static string ProcessConsoleInput(string command)
        {
            // remove excess whitespace
            command.Trim();

            // exit if empty
            if (command == string.Empty)
                return consoleOutput;

            // echo command back to console
            consoleOutput += ">" + command + "\n";

            ConsoleExpression evaluation = DecomposeInput(command);

            string executionResult = ValidateCommand(evaluation) ? ProcessCommand(evaluation) :
                                     ValidateCVar(evaluation)    ? ProcessCvar(evaluation)    :
                                     evaluation.token + " is not a valid token\n";

            consoleOutput += executionResult;
            
            return consoleOutput;
        }

        public static bool Register(CCommand newCommand)
        {
            if(commands.ContainsKey(newCommand.token))
            {
                return false;
            }

            commands[newCommand.token] = newCommand;

            return true;
        }

        private static CCommand[] GatherCommands()
        {
            throw new System.NotImplementedException();
        }

        private static ConsoleExpression DecomposeInput(string command)
        {
            command.Trim();

            // split into command and args
            string[] input = command.Split(' ');

            string cmd = input[0];
            string[] args = null;

            if (command.Length == cmd.Length)
                args = new string[0];
            else
                args = command.Substring(cmd.Length).Trim().Split(' ');

            return new ConsoleExpression(cmd, args);
        }
        private static bool ValidateCommand(ConsoleExpression command)
        {
            return commands.ContainsKey(command.token);
        }     
        private static string ProcessCommand(ConsoleExpression command)
        {
            if (commands.ContainsKey(command.token))
                return commands[command.token].Execute(command.arguments);

            return string.Format("{0} is not a valid command", command.token);
        }

        private static bool ValidateCVar(ConsoleExpression cvarCommand)
        {
            return CVarRegistry.ContainsCVar(cvarCommand.token);
        }
        private static string ProcessCvar(ConsoleExpression cvarCommand)
        {
            if (cvarCommand.arguments.Length == 0)
                return cvarCommand.token + " = " + CVarRegistry.LookUp(cvarCommand.token).ToString() + "\n";
            else
            {
                string reassembledArgs = string.Empty;

                for (int i = 0; i < cvarCommand.arguments.Length - 1; ++i)
                {
                    reassembledArgs += cvarCommand.arguments[i] + " ";
                }
                reassembledArgs += cvarCommand.arguments[cvarCommand.arguments.Length - 1];

                try
                {
                    CVarRegistry.WriteTo(cvarCommand.token, reassembledArgs);
                }
                catch (Exception ex)
                {
                    if (ex is CVarRegistryException)
                    {
                        return "Failed to assign to " + cvarCommand.token + "\n";
                    }
                }
                return string.Empty;
            }
        }
        
        private static class ConsoleDefaultCommands
        {
            static public string HelpCommand(string[] Arguments)
            {
                string output = string.Empty;

                foreach (var command in commands.Keys)
                {
                    output += command + "\n";
                }

                return output;
            }
            static public string ClearCommand(string[] Arguments)
            {
                consoleOutput = string.Empty;
                return consoleOutput;
            }
            static public string EchoCommand(string[] Arguments)
            {
                StringBuilder bldr = new StringBuilder();

                foreach (var arg in Arguments)
                {
                    bldr.Append(arg);
                    bldr.Append(' ');
                }

                return bldr.ToString() + "\n";
            }
            static public string ListCommand(string[] Arguments)
            {
                string output = string.Empty;

                var keyArray = CVarRegistry.GetCVarNames();

                foreach (var key in keyArray)
                {
                    output += key + "\n";
                }

                return output;
            }
        }
    }
}