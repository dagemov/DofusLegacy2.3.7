using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Commands;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Items;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.Sunshine.WorldServer.Commands.Moderator
{
    [CommandHandler("item", RoleEnum.Moderator)]
    public class ItemCommand : WorldCommand
    {
        public override void Execute()
        {
            if (Parameters.Length < 3)
                Client.Character.SendServerMessage("Unknown item and quantity !", Color.Red);
            else
            {
                int itemId = int.TryParse((string)Parameters[1], out int itemid) ? int.Parse((string)Parameters[1]) : -1;
                int quantity = int.TryParse((string)Parameters[2], out int stack) ? int.Parse((string)Parameters[2]) : -1;
                Character target = null;
                string name = null;

                if(Parameters.Length > 3)
                    name = (string)Parameters[3];

                target = CharacterManager.Instance.GetCharacter(name);
                    
                if (quantity == -1)
                    quantity = 1;

                if (itemId == -1 || !ItemManager.Instance.Items.ContainsKey(itemId))
                {
                    Client.Character.SendServerMessage($"Item {itemId} doesn't exist !", Color.Red);
                    return;
                }

                BasePlayerItem item = null;

                switch (Parameters[0].ToString().ToLower())
                {
                    case "add":
                    {
                        var inventory = target != null && name != "" ? target.Inventory : Client.Character.Inventory;

                        if ((target != null && target.IsInWorld()) || (target == null && name != null))
                        {
                            Client.Character.SendServerMessage($"Target {name} doesn't exist or is not connected !");
                            break;
                        }

                        if ((ItemTypeEnum)ItemManager.Instance.Items[itemId].TypeId == ItemTypeEnum.CERTIFICAT_DE_DRAGODINDE)
                        {
                            for (int i = 0; i < quantity; i++)
                            {
                                item = ItemManager.Instance.CreatePlayerItem(itemId, 1);
                                if (item != null)
                                    inventory.AddItem(item, 1);
                            }

                            if (target != null && name != "")
                                Client.Character.SendServerMessage($"¡{quantity} certificado(s) {itemId} añadidos a {target.Name}!");
                            else
                                Client.Character.SendServerMessage($"¡{quantity} certificado(s) {itemId} añadidos!");
                            break;
                        }

                        item = ItemManager.Instance.CreatePlayerItem(itemId, quantity);
                        if (target != null && name != "")
                        {
                            inventory.AddItem(item, quantity);
                            Client.Character.SendServerMessage($"Item {itemId} has been added to {target.Name} !");
                        }
                        else
                        {
                            inventory.AddItem(item, quantity);
                            Client.Character.SendServerMessage($"Item {itemId} has been added !");
                        }
                        break;
                    }

                    case "remove":
                        if (target != null)
                        {
                            item = target.Inventory.GetItem(itemid, CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED);
                            if (item == null)
                                return;
                            target.Inventory.RemoveItem(item, quantity);
                            Client.Character.SendServerMessage($"Item {itemId} has been removed to {target.Name} !");
                        }
                        else if ((target != null && target.IsInWorld()) || (target == null && name != null))
                            Client.Character.SendServerMessage($"Target {name} doesn't exist or is not connected !");
                        else
                        {
                            item = Client.Character.Inventory.GetItem(itemid, CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED);
                            if (item == null)
                                return;
                            Client.Character.Inventory.RemoveItem(item, quantity);
                            Client.Character.SendServerMessage($"Item {itemId} has been removed !");
                        }
                        break;
                }
            }
        }

        public override string Description
        {
            get { return "Allows add a item in inventory."; }
        }
    }
}
