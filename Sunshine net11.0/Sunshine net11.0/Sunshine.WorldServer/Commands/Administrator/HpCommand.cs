using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Commands;
using Sunshine.WorldServer.Handlers.Characters;

namespace Sunshine.WorldServer.Commands.Administrator
{
    [CommandHandler("hp", RoleEnum.Administrator)]
    public class HpCommand : WorldCommand
    {
        public override string Description => "Restaure complètement les points de vie.";

        public override void Execute()
        {
            if (Client?.Character == null)
                return;

            Client.Character.Stats.Health.Taken = 0;
            CharacterHandler.SendUpdateLifePointsMessage(Client);
            Client.Character.RefreshStats();
            Client.Character.SendServerMessage("Points de vie restaurés.");
        }
    }
}
