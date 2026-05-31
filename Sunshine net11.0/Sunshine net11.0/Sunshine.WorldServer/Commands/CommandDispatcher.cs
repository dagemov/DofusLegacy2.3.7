using Sunshine.WorldServer.Client;
using Sunshine.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Sunshine.Protocol.Enums;

namespace Sunshine.WorldServer.Commands
{
    public static class CommandDispatcher
    {
        public static void Dispatch(WorldClient client, string command)
        {
            if (client != null)
            {
                string[] messages = command.Split(' ');
                string realCommand = messages.Length >= 2 ? $"{messages[0].Trim('.')} {messages[1]}" : messages[0].Trim('.');

                if (IsValidCommand(realCommand) || IsValidCommand(messages[0].Trim('.')))
                {

                    var roleRequired = IsValidCommand(realCommand) ? CommandManager.Instance.Commands[realCommand].Item1 : 
                                                                     CommandManager.Instance.Commands[realCommand = messages[0].Trim('.')].Item1;

                    var function = CommandManager.Instance.Commands[realCommand].Item2();
                    if (function != null && CommandManager.Instance.IsAvailable(client, roleRequired))
                    {
                        object[] parameters = new object[messages.Length];

                        if (messages.Length <= 1)
                            parameters[0] = messages[0];
                        else
                        {
                            messages = messages.Where(x => x != messages[0]).ToArray();
                            for (int i = 0; i < messages.Length; i++)
                                parameters[i] = messages[i];                                  
                        }

                        function.Initialize(client, parameters);
                    }
                    else
                        client.Character.SendServerMessage("Unknown command.");
                }
                else
                    client.Character.SendServerMessage("Unknown command.");
            }
            else
                return;
        }

        public static bool IsValidCommand(string cmd)
        {
            return CommandManager.Instance.Commands.ContainsKey(cmd);
        }
    }
}
