using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Tools.Dlm;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Dialogs.Houses;
using Sunshine.WorldServer.Game.Maps.Houses;
using Sunshine.WorldServer.Handlers.Context.Roleplay;
using Sunshine.WorldServer.Handlers.Guilds;
using Sunshine.WorldServer.Handlers.Dialogs;
using System;
using System.Linq;

namespace Sunshine.WorldServer.Handlers.Houses
{
    public class HouseHandler : WorldPacketHandler
    {
        private const int MaxHousePrice = 2000000000;

        public static void SendHouseBuyRequestMessage(WorldClient client, int price)
        {
            if (client == null)
                return;

            client.Send(new HouseBuyRequestMessage(price));
        }

        public static void SendPurchasableDialogMessage(WorldClient client, bool buyOrSell, int purchasableId, int price)
        {
            if (client == null)
                return;

            client.Send(new ExchangeStartedMessage((sbyte)ExchangeTypeEnum.REALESTATE_HOUSE));
            client.Send(new PurchasableDialogMessage(buyOrSell, purchasableId, price));
        }

        public static void SendLockableShowCodeDialogMessage(WorldClient client, bool changeOrUse, int nbCase)
        {
            if (client == null)
                return;

            client.Send(new LockableShowCodeDialogMessage(changeOrUse, (sbyte)nbCase));
        }

        public static void SendHousePanel(WorldClient client, House house)
        {
            if (client?.Character == null || house == null)
                return;

            client.Send(new HousePropertiesMessage(house.GetInformations(client)));

            if (house.GuildId.HasValue &&
                client.Character.Guild != null &&
                client.Character.Guild.Id == house.GuildId.Value)
            {
                client.Send(new HouseGuildRightsMessage(
                    (short)house.Id,
                    client.Character.Guild.GetGuildInformations(),
                    house.GuildShareParams));
            }
            else
            {
                client.Send(new HouseGuildNoneMessage((short)house.Id));
            }
        }

        [WorldHandler(HouseBuyRequestMessage.Id)]
        public static void HandleHouseBuyRequestMessage(WorldClient client, HouseBuyRequestMessage message)
        {
            var house = GetHouseFromCurrentMap(client);
            if (house == null)
                return;

            if (!house.OnSale)
            {
                client.Send(new HouseBuyResultMessage(house.Id, false, 0));
                return;
            }

            var price = house.Price;
            if (price <= 0)
                price = house.Record.DefaultPrice;

            if (price > MaxHousePrice)
                price = MaxHousePrice;

            if (client.Character.Inventory.Kamas < price)
            {
                client.Send(new HouseBuyResultMessage(house.Id, false, price));
                return;
            }

            if (house.IsOwner(client.Character) || HouseManager.Instance.AccountAlreadyOwnsHouse(client.Character.Account.Id))
            {
                client.Send(new HouseBuyResultMessage(house.Id, false, price));
                return;
            }

            client.Character.Inventory.SetKamas(-price);

            house.OwnerId = client.Character.Account.Id;
            house.OwnerName = client.Account.Nickname;
            house.OnSale = false;
            house.SaleLocked = false;
            house.Locked = false;
            house.Code = string.Empty;
            house.ChestCode = string.Empty;
            house.GuildId = null;
            house.GuildShareParams = 0;
            house.ClearChestState();

            HouseManager.Instance.Save();

            client.Send(new HouseBuyResultMessage(house.Id, true, price));
            RefreshHouseState(house);
        }

        [WorldHandler(HouseSellRequestMessage.Id)]
        public static void HandleHouseSellRequestMessage(WorldClient client, HouseSellRequestMessage message)
        {
            var house = GetOwnedHouseFromCurrentMap(client);
            if (house == null)
                return;

            int amount = message.amount;

            if (amount <= 0)
            {
                if (house.OnSale)
                {
                    house.OnSale = false;
                    house.SaleLocked = false;
                    house.Price = house.Record.DefaultPrice;

                    HouseManager.Instance.Save();

                    client.Character.SendServerMessage("La vente de la maison a été annulée.");
                    RefreshHouseState(house);
                }

                return;
            }

            if (amount > MaxHousePrice)
                amount = MaxHousePrice;

            house.Price = amount;
            house.OnSale = true;
            house.SaleLocked = false;
            house.Locked = false;
            house.Code = string.Empty;

            HouseManager.Instance.Save();

            client.Character.SendServerMessage("La maison est maintenant en vente pour " + house.Price + " kamas.");
            RefreshHouseState(house);
        }

        [WorldHandler(HouseSellFromInsideRequestMessage.Id)]
        public static void HandleHouseSellFromInsideRequestMessage(WorldClient client, HouseSellFromInsideRequestMessage message)
        {
            HandleHouseSellRequestMessage(client, new HouseSellRequestMessage(message.amount));
        }

        [WorldHandler(LockableUseCodeMessage.Id)]
        public static void HandleLockableUseCodeMessage(WorldClient client, LockableUseCodeMessage message)
        {
            try
            {
                if (client == null || client.Character == null)
                    return;

                var dialog = client.Character.Dialog as HouseCodeDialog;
                if (dialog == null || dialog.House == null)
                    return;

                string enteredCode = House.NormalizeDigits(message?.code, dialog.IsChest ? House.ChestCodeLength : House.HouseCodeLength);
                int expectedLength = dialog.IsChest ? House.ChestCodeLength : House.HouseCodeLength;

                if (enteredCode.Length != expectedLength)
                {
                    client.Send(new LockableCodeResultMessage(false));
                    client.Character.SendServerMessage(dialog.IsChest
                        ? "Code du coffre incorrect."
                        : "Code de la maison incorrect.");

                    new HouseCodeDialog(client.Character, dialog.House, dialog.Action, dialog.IsChest, dialog.ElementId).Open();
                    return;
                }

                if (dialog.IsChest)
                {
                    if (!dialog.House.IsOwner(client.Character) &&
                        !dialog.House.IsGuildMemberAllowedChest(client.Character) &&
                        dialog.House.HasGuildRight(HouseRightsBitEnum.HOUSE_FORBID_OTHERS_ACCESS_CHEST))
                    {
                        client.Send(new LockableCodeResultMessage(false));
                        client.Character.SendServerMessage("Vous n'êtes pas autorisé à ouvrir ce coffre.");
                        DialogHandler.SendLeaveDialogMessage(client);
                        return;
                    }
                }
                else
                {
                    if (!dialog.House.IsOwner(client.Character) &&
                        !dialog.House.IsGuildMemberAllowed(client.Character) &&
                        dialog.House.HasGuildRight(HouseRightsBitEnum.HOUSE_FORBID_OTHERS_ACCESS))
                    {
                        client.Send(new LockableCodeResultMessage(false));
                        client.Character.SendServerMessage("Vous n'êtes pas autorisé à entrer dans cette maison.");
                        DialogHandler.SendLeaveDialogMessage(client);
                        return;
                    }
                }

                bool result = dialog.IsChest
                    ? dialog.House.CheckChestCode(enteredCode)
                    : dialog.House.CheckCode(enteredCode);

                if (!result)
                {
                    client.Send(new LockableCodeResultMessage(false));
                    client.Character.SendServerMessage(dialog.IsChest
                        ? "Code du coffre incorrect."
                        : "Code de la maison incorrect.");

                    new HouseCodeDialog(client.Character, dialog.House, dialog.Action, dialog.IsChest, dialog.ElementId).Open();
                    return;
                }

                if (dialog.IsChest)
                {
                    if (dialog.Action == HouseCodeDialogAction.UnlockChest)
                    {
                        if (!dialog.House.IsOwner(client.Character))
                        {
                            client.Send(new LockableCodeResultMessage(false));
                            client.Character.SendServerMessage("Seul le propriétaire peut déverrouiller le coffre.");
                            return;
                        }

                        dialog.House.UnlockChest();
                        HouseManager.Instance.Save();
                        DialogHandler.SendLeaveDialogMessage(client);

                        client.Send(new LockableStateUpdateStorageMessage(
                            false,
                            client.Character.Map != null ? client.Character.Map.Id : dialog.House.Map.Id,
                            dialog.ElementId > 0 ? dialog.ElementId : dialog.House.InteractiveId));
                                                RefreshHouseState(dialog.House);
                        return;
                    }

                    DialogHandler.SendLeaveDialogMessage(client);
                    client.Character.SetTrade(ExchangeTypeEnum.STORAGE, null, dialog.House);

                    if (client.Character.Trade == null)
                    {
                        client.Character.SendServerMessage("Impossible d'ouvrir le coffre.");
                        return;
                    }

                    client.Character.Trade.Open();

                    client.Send(new LockableStateUpdateStorageMessage(
                        !string.IsNullOrWhiteSpace(dialog.House.ChestCode),
                        client.Character.Map != null ? client.Character.Map.Id : dialog.House.Map.Id,
                        dialog.ElementId > 0 ? dialog.ElementId : dialog.House.InteractiveId));
                    return;
                }

                if (dialog.House.EnterMap == null)
                {
                    client.Character.SendServerMessage("Map intérieure de la maison introuvable.");
                    DialogHandler.SendLeaveDialogMessage(client);
                    return;
                }

                DialogHandler.SendLeaveDialogMessage(client);
                client.Character.BindToHouse(dialog.House, true);
                client.Character.Teleport(dialog.House.EnterMap.Id, dialog.House.EnterCellId);
            }
            catch (Exception)
            {
                if (client?.Character != null)
                {
                    client.Send(new LockableCodeResultMessage(false));
                    client.Character.SendServerMessage("Erreur lors de la vérification du code.");
                    DialogHandler.SendLeaveDialogMessage(client);
                }
            }
        }

        [WorldHandler(LockableChangeCodeMessage.Id)]
        public static void HandleLockableChangeCodeMessage(WorldClient client, LockableChangeCodeMessage message)
        {
            var dialog = client.Character.Dialog as HouseCodeDialog;
            if (dialog == null || dialog.House == null)
                return;

            bool success;

            if (dialog.IsChest)
            {
                if (!dialog.House.IsOwner(client.Character))
                {
                    client.Send(new LockableCodeResultMessage(false));
                    client.Character.SendServerMessage("Seul le propriétaire peut verrouiller le coffre.");
                    return;
                }

                success = dialog.House.SetChestCode(message?.code);
                if (!success)
                {
                    client.Send(new LockableCodeResultMessage(false));
                    client.Character.SendServerMessage($"Le code du coffre doit contenir exactement {House.ChestCodeLength} chiffres.");
                    new HouseCodeDialog(client.Character, dialog.House, dialog.Action, true, dialog.ElementId).Open();
                    return;
                }

                HouseManager.Instance.Save();
                DialogHandler.SendLeaveDialogMessage(client);
                client.Send(new LockableCodeResultMessage(true));
                client.Send(new LockableStateUpdateStorageMessage(
                    true,
                    client.Character.Map != null ? client.Character.Map.Id : dialog.House.Map.Id,
                    dialog.ElementId > 0 ? dialog.ElementId : dialog.House.InteractiveId));
                                RefreshHouseState(dialog.House);
                return;
            }

            var normalizedHouseCode = House.NormalizeDigits(message?.code, House.HouseCodeLength);
            if (string.IsNullOrWhiteSpace(normalizedHouseCode))
            {
                dialog.House.Locked = false;
                dialog.House.Code = string.Empty;
                HouseManager.Instance.Save();
                DialogHandler.SendLeaveDialogMessage(client);
                RefreshHouseState(dialog.House);
                return;
            }

            success = dialog.House.SetHouseCode(normalizedHouseCode);
            if (!success)
            {
                client.Send(new LockableCodeResultMessage(false));
                client.Character.SendServerMessage($"Le code de la maison doit contenir exactement {House.HouseCodeLength} chiffres.");
                new HouseCodeDialog(client.Character, dialog.House, dialog.Action, false, dialog.ElementId).Open();
                return;
            }

            HouseManager.Instance.Save();
            DialogHandler.SendLeaveDialogMessage(client);
            client.Send(new LockableCodeResultMessage(true));
                        RefreshHouseState(dialog.House);
        }

        [WorldHandler(HouseGuildRightsViewMessage.Id)]
        public static void HandleHouseGuildRightsViewMessage(WorldClient client, HouseGuildRightsViewMessage message)
        {
            var house = GetHouseFromCurrentInterior(client);
            if (house == null)
                return;

            if (!house.GuildId.HasValue ||
                client.Character.Guild == null ||
                client.Character.Guild.Id != house.GuildId.Value)
            {
                client.Send(new HouseGuildNoneMessage((short)house.Id));
                return;
            }

            client.Send(new HouseGuildRightsMessage(
                (short)house.Id,
                client.Character.Guild.GetGuildInformations(),
                house.GuildShareParams));
        }


        [WorldHandler(HouseGuildRightsChangeRequestMessage.Id)]
        public static void HandleHouseGuildRightsChangeRequestMessage(WorldClient client, HouseGuildRightsChangeRequestMessage message)
        {
            var house = GetOwnedHouseFromCurrentInterior(client);
            if (house == null || client.Character.Guild == null)
                return;

            house.GuildId = client.Character.Guild.Id;
            house.GuildShareParams = message.rights;

            HouseManager.Instance.Save();

            client.Send(new HouseGuildRightsMessage(
                (short)house.Id,
                client.Character.Guild.GetGuildInformations(),
                house.GuildShareParams));

            RefreshHouseState(house);
        }

        [WorldHandler(HouseGuildShareRequestMessage.Id)]
        public static void HandleHouseGuildShareRequestMessage(WorldClient client, HouseGuildShareRequestMessage message)
        {
            var house = GetOwnedHouseFromCurrentInterior(client);
            if (house == null)
                return;

            if (client.Character.Guild == null)
            {
                client.Character.SendServerMessage("Vous n'avez pas de guilde.");
                return;
            }

            if (message.enable)
            {
                house.GuildId = client.Character.Guild.Id;

                // Si aucun droit n’a été défini, on met un droit minimal
                if (house.GuildShareParams == 0)
                    house.GuildShareParams = 1; // 1 = accès maison
            }
            else
            {
                house.GuildId = null;
                house.GuildShareParams = 0;
            }


            HouseManager.Instance.Save();

            RefreshHouseState(house);
            SendHousePanel(client, house);
        }


        [WorldHandler(HouseToSellListRequestMessage.Id)]
        public static void HandleHouseToSellListRequestMessage(WorldClient client, HouseToSellListRequestMessage message)
        {
            var houses = HouseManager.Instance.GetHousesToSell()
                .Where(x => x.Map != null)
                .Select(x => x.GetInformationsForSell())
                .ToList();

            const int pageSize = 10;
            short totalPages = (short)System.Math.Max(1, (int)System.Math.Ceiling(houses.Count / (double)pageSize));
            short pageIndex = message.pageIndex;

            if (pageIndex < 0)
                pageIndex = 0;
            if (pageIndex >= totalPages)
                pageIndex = (short)(totalPages - 1);

            var page = houses
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToList();

            client.Send(new HouseToSellListMessage(pageIndex, totalPages, page));
        }

        [WorldHandler(HouseLockFromInsideRequestMessage.Id)]
        public static void HandleHouseLockFromInsideRequestMessage(WorldClient client, HouseLockFromInsideRequestMessage message)
        {
            var house = GetOwnedHouseFromCurrentInterior(client);
            if (house == null || house.OnSale)
                return;

            new HouseCodeDialog(client.Character, house, HouseCodeDialogAction.ChangeCode, false).Open();
        }

        [WorldHandler(HouseKickRequestMessage.Id)]
        public static void HandleHouseKickRequestMessage(WorldClient client, HouseKickRequestMessage message)
        {
            if (client?.Character?.Map == null)
                return;

            var house = GetHouseFromCurrentInterior(client);
            if (house == null || !house.IsOwner(client.Character))
                return;

            var target = client.Character.Map.GetActor(message.id) as Character;
            if (target == null)
                return;

            TeleportOutOfHouse(target, house);
        }

        public static void TrySendMapComplementaryInHouse(WorldClient client)
        {
            var house = HouseManager.Instance.ResolveInteriorHouse(client.Character);
            if (house == null)
                return;

            var map = client.Character.Map;
            var obstacles = map.Interactives.SelectMany(x => x.GetObstacles).ToList();

            client.Send(new MapComplementaryInformationsDataInHouseMessage(
                (short)map.SubAreaId,
                map.Id,
                0,
                HouseManager.Instance.GetHousesInformationsByMap(map.Id, client),
                map.RolePlayActors.Select(x => x is Game.Actors.Npcs.Npc
                    ? (x as Game.Actors.Npcs.Npc).GetGameRolePlayNpcInformations(client)
                    : x.GetGameRolePlayActorInformations()),
                map.Interactives.Select(x => Game.Maps.Interactives.HouseInteractiveBuilder.BuildForClient(x, client)).Where(x => x != null),
                map.Interactives.Where(x => x.GetStatedElement != null).Select(x => x.GetStatedElement),
                obstacles,
                map.Fights.Where(x => x != null && x.State == FightStateEnum.Placement).Select(x => x.GetFightCommonInformations),
                house.GetInformationsInside()));

            if (house.IsOwner(client.Character))
                SendHousePanel(client, house);
        }

        public static HouseInformationsInside GetHouseInformationsInside(WorldClient client)
        {
            House house = GetHouseFromCurrentInterior(client);
            if (house == null)
                return null;

            return house.GetHouseInformationsInside();
        }

        public static void SendHouseInformationsInside(WorldClient client)
        {
            HouseInformationsInside informations = GetHouseInformationsInside(client);
            if (informations == null)
                return;

            client.Send(new HousePropertiesMessage(new HouseInformations(
                false,
                false,
                informations.houseId,
                new int[0],
                informations.ownerName,
                informations.modelId
            )));
        }

        public static void TrySendInsideHousePanel(WorldClient client)
        {
            if (client == null || client.Character == null || client.Character.Map == null)
                return;

            House house = GetHouseFromCurrentInterior(client);
            if (house == null)
                return;

            client.Character.BindToHouse(house, true);


            var infos = house.GetInformations(client);

            client.Send(new HousePropertiesMessage(infos));

            if (house.GuildId.HasValue &&
                client.Character.Guild != null &&
                client.Character.Guild.Id == house.GuildId.Value)
            {
                client.Send(new HouseGuildRightsMessage(
                    (short)house.Id,
                    client.Character.Guild.GetGuildInformations(),
                    house.GuildShareParams));
            }
            else
            {
                client.Send(new HouseGuildNoneMessage((short)house.Id));
            }
        }







        private static House ResolveCharacterHouse(WorldClient client)
        {
            if (client?.Character?.Map == null)
                return null;

            var mapId = client.Character.Map.Id;
            var preferredHouse = client.Character.LastTargetedHouse;

            if (preferredHouse != null)
            {
                if (preferredHouse.Map != null && preferredHouse.Map.Id == mapId)
                    return preferredHouse;

                if (preferredHouse.ContainsInteriorMap(mapId))
                    return preferredHouse;
            }

            return HouseManager.Instance.GetHouseByInteriorMapId(mapId)
                   ?? HouseManager.Instance.GetHouseByExteriorMapId(mapId);
        }

        private static House GetHouseFromCurrentMap(WorldClient client)
        {
            return ResolveCharacterHouse(client);
        }

        private static House GetOwnedHouseFromCurrentMap(WorldClient client)
        {
            var house = GetHouseFromCurrentMap(client);
            if (house == null || !house.IsOwner(client.Character))
                return null;

            return house;
        }

        private static House GetHouseFromCurrentInterior(WorldClient client)
        {
            var house = ResolveCharacterHouse(client);
            return house != null && house.ContainsInteriorMap(client.Character.Map.Id)
                ? house
                : null;
        }

        private static House GetOwnedHouseFromCurrentInterior(WorldClient client)
        {
            var house = GetHouseFromCurrentInterior(client);
            if (house == null || !house.IsOwner(client.Character))
                return null;

            return house;
        }

        private static void RefreshHouseState(House house)
        {
            if (house == null)
                return;

            if (house.Map != null)
            {
                foreach (var mapClient in house.Map.Clients)
                    ContextRoleplayHandler.SendMapComplementaryInformationsDataMessage(mapClient);
            }

            foreach (var interiorMapId in house.Record.Interiors ?? new uint[0])
            {
                var interiorMap = MySql.Database.Managers.MapManager.Instance.GetMap((int)interiorMapId);
                if (interiorMap == null)
                    continue;

                foreach (var mapClient in interiorMap.Clients)
                    ContextRoleplayHandler.SendMapComplementaryInformationsDataMessage(mapClient);
            }

            if (house.GuildId.HasValue)
            {
                var guild = MySql.Database.Managers.GuildManager.Instance.GetGuild(house.GuildId.Value);
                if (guild != null)
                {
                    foreach (var member in guild.Members.Where(x => x.IsConnected()))
                        GuildHandler.SendGuildHousesInformationMessage(member.Client, guild);
                }
            }
        }

        private static void TeleportOutOfHouse(Character character, House house)
        {
            if (character == null || house == null)
                return;

            if (!house.TryGetExitDestination(out int targetMapId, out short targetCellId))
                return;

            if (character.LastTargetedHouse != null && character.LastTargetedHouse.Id == house.Id)
                character.ClearHouseBinding(true);

            character.Teleport(targetMapId, targetCellId);
        }
    }
}