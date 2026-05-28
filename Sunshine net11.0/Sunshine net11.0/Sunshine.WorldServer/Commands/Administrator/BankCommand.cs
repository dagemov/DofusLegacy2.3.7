using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Characters;

namespace Sunshine.WorldServer.Commands.Administrator
{
    [CommandHandler("bank", RoleEnum.Administrator)]
    public class BankCommand : WorldCommand
    {
        public override string Description => "Ouvre la banque du personnage ciblé ou du lanceur (.bank [nom]).";

        public override void Execute()
        {
            Character target = Client.Character;

            if (Parameters != null && Parameters.Length > 0)
            {
                string targetName = (Parameters[0] ?? string.Empty).ToString().Trim();
                if (!string.IsNullOrWhiteSpace(targetName))
                {
                    target = MySql.Database.Managers.CharacterManager.Instance.GetCharacter(targetName);
                    if (target == null || target.Client == null)
                    {
                        Client.Character.SendServerMessage($"Personnage introuvable ou hors ligne : {targetName}");
                        return;
                    }
                }
            }

            if (target.IsInTrade())
                target.LeaveTrade();

            target.SetTrade(ExchangeTypeEnum.STORAGE, null, target);
            target.Trade.Open();

            if (target == Client.Character)
                Client.Character.SendServerMessage("Banque ouverte.");
            else
                Client.Character.SendServerMessage($"Banque ouverte pour {target.Name}.");
        }
    }
}
