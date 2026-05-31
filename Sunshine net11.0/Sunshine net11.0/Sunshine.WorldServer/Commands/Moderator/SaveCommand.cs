using Sunshine.Protocol.Enums;
using Sunshine.Servers;

namespace Sunshine.WorldServer.Commands.Moderator
{
    [CommandHandler("save", RoleEnum.Moderator)]
    public class SaveCommand : WorldCommand
    {
        public override void Execute()
        {
            ServersManager.Instance.Save();
            Client.Character.SendServerMessage("World saved.");
        }

        public override string Description
        {
            get { return "Allows save the world."; }
        }
    }
}