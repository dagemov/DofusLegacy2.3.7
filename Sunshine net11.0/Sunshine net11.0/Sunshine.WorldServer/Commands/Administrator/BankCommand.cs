using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Characters;

namespace Sunshine.WorldServer.Commands.Administrator
{
    [CommandHandler("bank", RoleEnum.Administrator)]
    public class BankCommand : WorldCommand
    {
        public override string Description => "Abre el banco del personaje objetivo o del lanzador (.bank [nombre]).";

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
                        Client.Character.SendServerMessage($"Personaje no encontrado o desconectado: {targetName}");
                        return;
                    }
                }
            }

            if (target.IsInTrade())
                target.LeaveTrade();

            target.SetTrade(ExchangeTypeEnum.STORAGE, null, target);
            target.Trade.Open();

            if (target == Client.Character)
                Client.Character.SendServerMessage("Banco abierto.");
            else
                Client.Character.SendServerMessage($"Banco abierto para {target.Name}.");
        }
    }
}
