using Sunshine.Protocol.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.BaseServer.Messages
{
    public class MessageInitializer
    {
        public static void Initialize()
        {
            Logs.Logger.Write("[ Protocol ] Initializing all Messages");

            var messages = Assembly.GetAssembly(typeof(Message)).GetTypes().Where(x => x.Namespace != null && x.Namespace == "Sunshine.Protocol.Messages");

            foreach (var message in messages)
            {
                FieldInfo field = message.GetField("Id");
                uint _id = 0;       
                
                if(field != null)
                    _id = (uint)field.GetValue(message);

                if (_id != 0)
                {
                    var currentMessage = message.GetConstructor(Type.EmptyTypes);
                    var parameters = currentMessage.GetParameters().Select(x => Expression.Parameter(x.ParameterType));
                    var function = Expression.Lambda<Func<Message>>(Expression.New(currentMessage, parameters), parameters);
                    var newMessage = function.Compile();
                    Message.Messages.Add(_id, newMessage);
                }
            }
        }
    }
}
