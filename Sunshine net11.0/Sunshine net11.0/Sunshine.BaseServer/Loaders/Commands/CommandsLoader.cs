using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.BaseServer.Loaders.Commands
{
    public static class CommandsLoader
    {
        public static void Initialize()
        {
            Logs.Logger.Write("[ World ] Loading Commands...");

            var commands = Assembly.GetAssembly(typeof(WorldCommand)).GetTypes().Where(x => x.BaseType != null && x.BaseType.Name == "WorldCommand");
            foreach (var command in commands)
            {
                var commandAttribute = command.GetCustomAttribute(typeof(CommandHandler));
                if (commandAttribute != null)
                {
                    var attribute = commandAttribute as CommandHandler;
                    string nameCommand = attribute.Name;
                    RoleEnum roleCommand = attribute.Role;
                    var currentCommand = command.GetConstructor(Type.EmptyTypes);
                    var function = Expression.Lambda<Func<WorldCommand>>(Expression.New(currentCommand));
                    Tuple<RoleEnum, Func<WorldCommand>> cmd = new Tuple<RoleEnum, Func<WorldCommand>>(roleCommand, function.Compile());
                    CommandManager.Instance.Commands.Add(nameCommand, cmd);
                }
            }

            Logs.Logger.Write($"[ World ] {CommandManager.Instance.Commands.Count} Commands Loaded");
        }
    }
}
