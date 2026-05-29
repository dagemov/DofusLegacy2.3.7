using Sunshine.BaseServer.Shutdown;
using Sunshine.Protocol.Enums;

namespace Sunshine.WorldServer.Commands.Administrator
{
    [CommandHandler("stop", RoleEnum.Administrator)]
    public class StopCommand : WorldCommand
    {
        public override string Description => "Planifie ou annule l'arrêt du serveur (.stop 60 / .stop cancel).";

        public override void Execute()
        {
            if (Parameters == null || Parameters.Length == 0)
            {
                Client.Character.SendServerMessage("Usage: .stop <secondes> | .stop cancel");
                return;
            }

            var value = (Parameters[0] ?? string.Empty).ToString().Trim().ToLowerInvariant();

            if (value == "cancel" || value == "off")
            {
                string cancelResult;
                ShutdownManager.Cancel(out cancelResult);
                Client.Character.SendServerMessage(cancelResult);
                return;
            }

            int seconds;
            if (!int.TryParse(value, out seconds))
            {
                Client.Character.SendServerMessage("Usage: .stop <secondes> | .stop cancel");
                return;
            }

            string result;
            ShutdownManager.Schedule(seconds, out result);
            Client.Character.SendServerMessage(result);
        }
    }
}
