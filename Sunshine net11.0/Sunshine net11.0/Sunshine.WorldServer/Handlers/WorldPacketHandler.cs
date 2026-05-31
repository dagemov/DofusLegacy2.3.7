using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Client;

namespace Sunshine.WorldServer.Handlers
{
    public class WorldPacketHandler
    {
    }
}

namespace Sunshine.WorldServer.Handlers.Security
{
    public class SecurityHandler : Handlers.WorldPacketHandler
    {
        [WorldHandler(ClientKeyMessage.Id)]
        public static void HandleClientKeyMessage(WorldClient client, ClientKeyMessage message)
        {
            // The client key is expected by newer clients. Sunshine does not rely on it yet,
            // but handling the packet prevents noisy unknown-message errors and keeps the session stable.
            _ = client;
            _ = message?.key;
        }
    }
}
