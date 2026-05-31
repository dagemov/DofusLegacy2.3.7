using System;
using System.Linq;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Game.Dialogs.Houses;
using Sunshine.WorldServer.Game.Maps.Houses;
using Sunshine.WorldServer.Handlers.Context.Roleplay;
using Sunshine.WorldServer.Handlers.Houses;

namespace Sunshine.WorldServer.Game.Maps.Interactives.Skills
{
    [SkillHandler(84)]
    public class SkillHouseEnter : Skill
    {
        public override void Execute()
        {
            var house =
                HouseManager.Instance.GetHouseByExteriorInteractive(Client.Character.Map.Id, Element) ??
                HouseManager.Instance.GetHouseByExteriorMapId(Client.Character.Map.Id) ??
                HouseManager.Instance.ResolveInteriorHouse(Client.Character, Element);

            if (house == null || house.EnterMap == null)
                return;

            void TeleportInside()
            {
                Client.Character.BindToHouse(house, true);
                Client.Character.Teleport(house.EnterMap.Id, house.EnterCellId);
            }

            if (house.OnSale || house.IsOwner(Client.Character))
            {
                TeleportInside();
                return;
            }

            if (house.RequiresCode(Client.Character))
            {
                new HouseCodeDialog(Client.Character, house, HouseCodeDialogAction.UseCode, false).Open();
                return;
            }

            if (house.CanEnter(Client.Character))
            {
                TeleportInside();
                return;
            }

            Client.Character.SendServerMessage("Vous n'êtes pas autorisé à entrer dans cette maison.");
        }
    }

    [SkillHandler(97)]
    public class SkillHouseBuy : Skill
    {
        private const int MaxHousePrice = 2000000000;

        public override void Execute()
        {
            var house = HouseManager.Instance.GetHouseByExteriorInteractive(Client.Character.Map.Id, Element) ?? HouseManager.Instance.GetHouseByInteractive(Element);
            if (house == null)
                return;

            if (!house.OnSale)
            {
                Client.Character.SendServerMessage("Cette maison n'est pas en vente.");
                return;
            }

            if (house.IsOwner(Client.Character))
            {
                Client.Character.SendServerMessage("Vous êtes déjà propriétaire de cette maison.");
                return;
            }

            if (HouseManager.Instance.AccountAlreadyOwnsHouse(Client.Character.Account.Id))
            {
                Client.Character.SendServerMessage("Vous possédez déjà une maison sur ce compte.");
                return;
            }

            int price = house.Price;
            if (price <= 0)
                price = house.Record.DefaultPrice;

            if (price <= 0 || price > MaxHousePrice)
            {
                Client.Character.SendServerMessage("Cette maison n'a pas de prix de vente valide.");
                Client.Send(new HouseBuyResultMessage(house.Id, false, 0));
                return;
            }

            if (Client.Character.Inventory.Kamas < price)
            {
                Client.Character.SendServerMessage($"Vous n'avez pas assez de kamas. Prix de la maison : {price:N0} kamas.");
                Client.Send(new HouseBuyResultMessage(house.Id, false, price));
                return;
            }

            HouseHandler.SendPurchasableDialogMessage(Client, true, house.Id, price);
        }
    }

    [SkillHandler(98)]
    [SkillHandler(108)]
    public class SkillHouseSell : Skill
    {
        public override void Execute()
        {
            var house =
                HouseManager.Instance.GetHouseByExteriorInteractive(Client.Character.Map.Id, Element) ??
                HouseManager.Instance.GetHouseByExteriorMapId(Client.Character.Map.Id) ??
                HouseManager.Instance.ResolveInteriorHouse(Client.Character, Element);

            if (house == null || !house.IsOwner(Client.Character))
                return;

            HouseHandler.SendPurchasableDialogMessage(Client, false, house.Id, house.Price > 0 ? house.Price : house.Record.DefaultPrice);
            Client.Character.SendServerMessage("Entrez le prix de vente dans l'interface.");
        }
    }

    [SkillHandler(81)]
    [SkillHandler(100)]
    public class SkillHouseLock : Skill
    {
        public override void Execute()
        {
            var house =
                HouseManager.Instance.GetHouseByExteriorInteractive(Client.Character.Map.Id, Element) ??
                HouseManager.Instance.GetHouseByExteriorMapId(Client.Character.Map.Id) ??
                HouseManager.Instance.ResolveInteriorHouse(Client.Character, Element);

            if (house == null || !house.IsOwner(Client.Character))
                return;

            new HouseCodeDialog(Client.Character, house, HouseCodeDialogAction.ChangeCode, false).Open();
        }
    }

    public abstract class SkillHouseChestBase : Skill
    {
        protected House GetHouse()
        {
            if (Client?.Character?.Map == null)
                return null;

            var currentMapId = Client.Character.Map.Id;

            // Les coffres sont chargés depuis worlds_interactives.
            // Priorité au resolver coffre pour garder le bon contexte après changement de map.
            var house = HouseManager.Instance.ResolveChestHouse(Client.Character, Element);
            if (house != null)
                return house;

            var preferredHouse = Client.Character.LastTargetedHouse;
            if (preferredHouse != null && preferredHouse.ContainsInteriorMap(currentMapId))
                return preferredHouse;

            house = HouseManager.Instance.ResolveInteriorHouse(Client.Character, Element);
            if (house != null)
                return house;

            return HouseManager.Instance.GetHousesByInteriorMapId(currentMapId).FirstOrDefault();
        }

        protected bool IsEffectiveOwner(House house)
        {
            if (house == null || Client?.Character == null)
                return false;

            var character = Client.Character;
            if (house.IsOwner(character))
                return true;

            if (house.OwnerId.HasValue)
            {
                if (house.OwnerId.Value == character.Id)
                    return true;

                if (character.Account != null && house.OwnerId.Value == character.Account.Id)
                    return true;
            }

            if (!string.IsNullOrWhiteSpace(house.OwnerName) &&
                string.Equals(house.OwnerName, character.Name, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        protected void OpenChest(House house)
        {
            if (house == null)
            {
                Client.Character.SendServerMessage("Maison/coffre introuvable.");
                return;
            }

            Client.Character.SetTrade(ExchangeTypeEnum.STORAGE, null, house);

            if (Client.Character.Trade == null)
            {
                Client.Character.SendServerMessage("Impossible d'ouvrir le coffre.");
                return;
            }

            Client.Character.Trade.Open();

            Client.Send(new LockableStateUpdateStorageMessage(
                !string.IsNullOrWhiteSpace(house.ChestCode),
                Client.Character.Map.Id,
                Element > 0 ? Element : house.InteractiveId));
        }
    }

    [SkillHandler(104)]
    public class SkillHouseChestOpen : SkillHouseChestBase
    {
        public override void Execute()
        {
            var house = GetHouse();
            if (house == null)
            {
                Client.Character.SendServerMessage("Maison/coffre introuvable.");
                return;
            }

            if (house.RequiresChestCode(Client.Character))
            {
                new HouseCodeDialog(Client.Character, house, HouseCodeDialogAction.UseCode, true, Element).Open();
                return;
            }

            if (house.CanOpenChest(Client.Character))
            {
                OpenChest(house);
                return;
            }

            Client.Character.SendServerMessage("Vous n'êtes pas autorisé à ouvrir ce coffre.");
        }
    }

    [SkillHandler(105)]
    public class SkillHouseChestLock : SkillHouseChestBase
    {
        public override void Execute()
        {
            var house = GetHouse();
            if (house == null)
            {
                Client.Character.SendServerMessage("Maison/coffre introuvable.");
                return;
            }

            if (!IsEffectiveOwner(house))
            {
                Client.Character.SendServerMessage("Seul le propriétaire peut utiliser cette action coffre.");
                return;
            }

            if (house.OnSale)
            {
                Client.Character.SendServerMessage("Impossible de verrouiller le coffre d'une maison en vente.");
                return;
            }

            new HouseCodeDialog(Client.Character, house, HouseCodeDialogAction.ChangeCode, true, Element).Open();
        }
    }

    [SkillHandler(106)]
    public class SkillHouseChestUnlock : SkillHouseChestBase
    {
        public override void Execute()
        {
            var house = GetHouse();
            if (house == null)
            {
                Client.Character.SendServerMessage("Maison/coffre introuvable.");
                return;
            }

            if (!IsEffectiveOwner(house))
            {
                Client.Character.SendServerMessage("Seul le propriétaire peut utiliser cette action coffre.");
                return;
            }

            if (string.IsNullOrWhiteSpace(house.ChestCode))
            {
                Client.Character.SendServerMessage("Le coffre n'est pas verrouillé.");
                return;
            }

            new HouseCodeDialog(Client.Character, house, HouseCodeDialogAction.UnlockChest, true, Element).Open();
        }
    }
}
