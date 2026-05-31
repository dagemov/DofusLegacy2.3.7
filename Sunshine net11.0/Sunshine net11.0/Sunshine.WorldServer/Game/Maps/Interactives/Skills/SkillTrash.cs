using Sunshine.WorldServer.Game.Exchanges;

namespace Sunshine.WorldServer.Game.Maps.Interactives.Skills
{
    [SkillHandler(153)]
    public class SkillTrash : Skill
    {
        public override void Execute()
        {
            if (Client?.Character?.Map == null)
                return;

            var trade = new TrashTrade(Client.Character, Client.Character.Map.Id, Element);
            Client.Character.SetTrade(Protocol.Enums.ExchangeTypeEnum.STORAGE, trade);
            Client.Character.Trade?.Open();
        }
    }
}
