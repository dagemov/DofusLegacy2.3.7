using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Dialogs;
using Sunshine.WorldServer.Game.Maps.Houses;
using Sunshine.WorldServer.Game.Maps.PaddockInstances;
using Sunshine.WorldServer.Handlers.Guilds;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Maps.Interactives.Skills
{
    [SkillHandler(184), SkillHandler(183)]
    public class SkillUse : Skill, IDialog
    {
        public override void Execute()
        {
            var interactiveCandidates = Client.Character.Map.Interactives != null
                ? Client.Character.Map.Interactives
                    .Where(x => x != null && x.Element == Element)
                    .ToList()
                : new List<Interactive>();

            var interact =
                interactiveCandidates.FirstOrDefault(x =>
                    x.PaddockInstanceId > 0 &&
                    PaddockInstanceManager.Instance.CanUsePaddockInstanceInteractive(
                        Client.Character,
                        x.PaddockInstanceId,
                        Client.Character.Map.Id,
                        Element))

                ?? interactiveCandidates.FirstOrDefault(x =>
                    x.HouseId > 0 &&
                    HouseManager.Instance.CanUseHouseInteractive(
                        Client.Character,
                        x.HouseId,
                        Client.Character.Map.Id,
                        Element))

                ?? interactiveCandidates.FirstOrDefault(x => x.HouseId == 0 && x.PaddockInstanceId == 0)

                ?? interactiveCandidates.FirstOrDefault();

            if (interact == null)
                return;

            if (interact.Parameters.Count <= 0)
            {
                Client.Character.Dialog = this;

                if (Client.Character.Guild == null)
                    GuildHandler.SendGuildCreationStartedMessage(Client);
                else
                {
                    GuildHandler.SendGuildCreationResultMessage(
                        Client,
                        GuildCreationResultEnum.GUILD_CREATE_ERROR_ALREADY_IN_GUILD);

                    Client.Character.Dialog = null;
                }

                return;
            }

            if (interact.Parameters.Count < 3)
            {
                Client.Character.SendServerMessage("Téléportation interactive invalide : paramètres incomplets.");
                return;
            }

            var targetMap = MapManager.Instance.GetMap(interact.Parameters[0]);
            if (targetMap == null)
                return;

            House currentHouse = null;

            if (interact.PaddockInstanceId > 0)
            {
                var paddockInstance = PaddockInstanceManager.Instance.GetInstance(interact.PaddockInstanceId);
                if (paddockInstance == null)
                {
                    Client.Character.SendServerMessage("Instance d'enclos introuvable.");
                    return;
                }

                if (!PaddockInstanceManager.Instance.CanUsePaddockInstanceInteractive(
                    Client.Character,
                    interact.PaddockInstanceId,
                    Client.Character.Map.Id,
                    Element))
                {
                    Client.Character.SendServerMessage("Cette interaction n'appartient pas à cet enclos instancié.");
                    return;
                }

                var targetIsPaddockInterior = paddockInstance.ContainsInteriorMap(targetMap.Id) ||
                                              paddockInstance.EnterMapId == targetMap.Id;

                if (targetIsPaddockInterior)
                    Client.Character.BindToPaddockInstance(paddockInstance);
                else
                    Client.Character.ClearPaddockInstanceBinding();
            }
            else if (interact.HouseId > 0)
            {
                currentHouse = HouseManager.Instance.GetHouse(interact.HouseId);

                if (currentHouse == null)
                {
                    Client.Character.SendServerMessage("Maison introuvable pour cette interaction.");
                    return;
                }

                if (!HouseManager.Instance.CanUseHouseInteractive(
                    Client.Character,
                    interact.HouseId,
                    Client.Character.Map.Id,
                    Element))
                {
                    Client.Character.SendServerMessage("Cette interaction n'appartient pas à cette maison.");
                    return;
                }

                Client.Character.BindToHouse(currentHouse, true);
            }
            else
            {
                currentHouse = HouseManager.Instance.ResolveInteriorHouse(Client.Character, Element);

                if (currentHouse != null && currentHouse.ContainsInteriorMap(Client.Character.Map.Id))
                {
                    var targetIsHouseRelated =
                        currentHouse.ContainsInteriorMap(targetMap.Id) ||
                        currentHouse.EnterMap != null && currentHouse.EnterMap.Id == targetMap.Id ||
                        currentHouse.Map != null && currentHouse.Map.Id == targetMap.Id ||
                        currentHouse.EndMapInstance != null && currentHouse.EndMapInstance.Id == targetMap.Id;

                    if (!targetIsHouseRelated)
                    {
                        Client.Character.SendServerMessage("Cette téléportation n'appartient pas à cette maison.");
                        return;
                    }

                    Client.Character.BindToHouse(currentHouse, true);
                }
            }

            short cell = (short)interact.Parameters[1];
            var direction = interact.Parameters[2];

            Client.Character.Direction = direction;
            Client.Character.Teleport(targetMap.Id, cell);
        }
    }
}