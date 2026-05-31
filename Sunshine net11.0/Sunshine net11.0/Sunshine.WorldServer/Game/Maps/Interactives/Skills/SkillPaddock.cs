using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Game.Dialogs.Paddocks;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Maps.Paddocks;

namespace Sunshine.WorldServer.Game.Maps.Interactives.Skills
{
    [SkillHandler(175)]
    public class SkillPaddock : Skill
    {
        public override void Execute()
        {
            if (Client?.Character?.Map == null)
                return;

            var paddock = PaddockManager.Instance.GetPaddockByMap(Client.Character.Map.Id);
            if (paddock != null && !paddock.CanUsePaddock(Client.Character))
            {
                Client.Character.SendServerMessage("Vous n'êtes pas autorisé à utiliser cet enclos.");
                return;
            }

            Handlers.Mounts.MountHandler.OpenPaddockPanel(Client);
        }

        public static void SendPaddockPanel(WorldClient client)
        {
            if (client == null)
                return;

            Handlers.Mounts.MountHandler.OpenPaddockPanel(client);
        }
    }

    [SkillHandler(176)]
    public class SkillPaddockBuy : Skill
    {
        public override void Execute()
        {
            if (Client?.Character?.Map == null || Client.Character.IsBusy())
                return;

            var paddock = PaddockManager.Instance.GetPaddockByMap(Client.Character.Map.Id);
            if (paddock == null || !paddock.ShouldDisplayBuySkill(Client.Character))
                return;

            new PaddockBuySellDialog(Client.Character, paddock, false, paddock.SalePrice).Open();
        }
    }

    [SkillHandler(177)]
    public class SkillPaddockSell : Skill
    {
        public override void Execute()
        {
            if (Client?.Character?.Map == null || Client.Character.IsBusy())
                return;

            var paddock = PaddockManager.Instance.GetPaddockByMap(Client.Character.Map.Id);
            if (paddock == null || !paddock.CanSell(Client.Character))
                return;

            new PaddockBuySellDialog(Client.Character, paddock, true, paddock.SalePrice).Open();
        }
    }

    [SkillHandler(178)]
    public class SkillPaddockModifySellPrice : Skill
    {
        public override void Execute()
        {
            if (Client?.Character?.Map == null || Client.Character.IsBusy())
                return;

            var paddock = PaddockManager.Instance.GetPaddockByMap(Client.Character.Map.Id);
            if (paddock == null || !paddock.CanModifySalePrice(Client.Character))
                return;

            new PaddockBuySellDialog(Client.Character, paddock, true, paddock.SalePrice).Open();
        }
    }
}
