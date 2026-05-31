using Sunshine.AuthServer;
using Sunshine.AuthServer.Client;
using Sunshine.AuthServer.Handlers;
using Sunshine.BaseClient;
using Sunshine.Logs;
using Sunshine.Protocol.IO;
using Sunshine.WorldServer;
using Sunshine.WorldServer.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Sunshine.Protocol.Utils.Extensions;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection.Emit;
using Sunshine.Servers;

namespace Sunshine.Protocol.Messages
{
    public static class MessageDispatcher
    {
        private static ushort _hiheader;
        private static uint _id;
        private static uint _length;
        private static BigEndianReader _reader;
        private static readonly object _dispatchLock = new object();

        public static void Dispatch(IBaseClient client, byte[] data)
        {
            if (client == null || data == null || data.Length == 0)
                return;

            lock (_dispatchLock)
            {
                _reader = new BigEndianReader(data);

                while (_reader.BytesAvailable > 0)
                {
                    MessagePart _currentMessage = new MessagePart(false);

                    _hiheader = _reader.ReadUShort();
                    _id = (uint)_hiheader >> 2;
                    _length = (uint)_hiheader & 3;
                    _reader = UpdateReader(_reader);

                    try
                    {
                        Message buildMessage = BuildMessage();
                        if (buildMessage == null)
                        {
                            Logger.WriteError(string.Format("Receive unkown message Id : {0} | Length : {1}", _id, _length));
                            return;
                        }

                        buildMessage.Unpack(_reader);
                        HandledMessage(client, buildMessage);
                    }
                    catch (Exception e)
                    {
                        Logger.WriteError(e.ToString());
                        return;
                    }
                }
            }
        }

        private static Message BuildMessage()
        {
            if (Message.Messages.ContainsKey(_id))
                return Message.Messages[_id]();
            return null;
        }

        private static void HandledMessage(IBaseClient client, Message message)
        {
            if (message == null)
            {
                Logger.WriteError(string.Format("Receive unkown message Id : {0} | Length : {1}", _id, _length));
                return;
            }

            IEnumerable<Type> types = null;
            Type handlerType = null;
            MethodInfo[] methods = null;
            MethodInfo function = null;

            #region MessageHandler
            if (client is AuthClient)
                types = Assembly.GetAssembly(typeof(AuthPacketHandler)).GetTypes().Where(x => x.BaseType != null && x.BaseType.Name == "AuthPacketHandler");                        
            else
                types = Assembly.GetAssembly(typeof(WorldPacketHandler)).GetTypes().Where(x => x.BaseType != null && x.BaseType.Name == "WorldPacketHandler");

            foreach (var type in types)
            {
                handlerType = type;
                methods = type.GetMethods();
                function = methods.FirstOrDefault(_id, client is AuthClient ? true : false);
                if (function != null)
                    break;
            }
            #endregion

            if (function != null)
            {
                Logger.WriteClient(client is AuthClient ? "AuthServer" : "WorldServer", client.Ip, client.Port, message.ToString());

                var methodCall = DynamicExtensions.CreateDelegate(function, typeof(IBaseClient), typeof(Message)) as Action<object, IBaseClient, Message>;
                methodCall.Invoke(function, client, message);              
            }
            else
            {
                Logger.WriteError(string.Format("Receive unkown message : {0} | Length : {1}", message.ToString(), _length));
                return;
            }
        }

        private static BigEndianReader UpdateReader(BigEndianReader reader)
        {
            #region switch length
            switch (_length)
            {
                case 0:
                    _length = 0;
                    break;
                case 1:
                    _length = reader.ReadByte();
                    break;
                case 2:
                    _length = reader.ReadUShort();
                    break;
                case 3:
                    _length = (uint)(((reader.ReadByte() & 255) << 16) + ((reader.ReadByte() & 255) << 8) + (reader.ReadByte() & 255));
                    break;
                default:
                    _length = 0;
                    break;
            }
            return reader;
            #endregion
        }
    }
}
