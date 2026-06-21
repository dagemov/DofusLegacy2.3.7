using Sunshine.Logs;

using Sunshine.Protocol.Enums;

using Sunshine.WorldServer.Game.Actors.Npcs.Actions;

using Sunshine.WorldServer.Game.Characters;

using Sunshine.WorldServer.Handlers.Characters.Inventory;

using System;
using System.Linq;



namespace Sunshine.WorldServer.Game.Actors.Npcs

{

    /// <summary>

    /// Catalogo .tienda / .tiendas — 9 tiendas fijas por categoria (NPC 9101-9109).

    /// Sin filtro por nivel: todos los items vendibles de la categoria en una sola tienda.

    /// </summary>

    public static class VirtualShopCatalog

    {

        public const int ShopSombrero = 9101;

        public const int ShopCapa = 9102;

        public const int ShopAnilloAmuleto = 9103;

        public const int ShopCinturonBotas = 9104;

        public const int ShopEscudo = 9105;

        public const int ShopConsumible = 9106;

        public const int ShopRecurso = 9107;

        public const int ShopDofusMascota = 9108;

        public const int ShopDiverso = 9109;



        public sealed class ShopSlot

        {

            public int Number { get; set; }

            public string Label { get; set; }

            public string[] Aliases { get; set; }

            public int NpcTemplateId { get; set; }

        }



        public static readonly ShopSlot[] Slots =

        {

            new ShopSlot

            {

                Number = 1,

                Label = "Sombrero",

                Aliases = new[] { "sombrero", "sombreros", "chapeau", "chapeaux", "hat", "hats" },

                NpcTemplateId = ShopSombrero

            },

            new ShopSlot

            {

                Number = 2,

                Label = "Capa",

                Aliases = new[] { "capa", "capas", "cape", "capes" },

                NpcTemplateId = ShopCapa

            },

            new ShopSlot

            {

                Number = 3,

                Label = "Anillo y amuleto",

                Aliases = new[] { "anillo", "anillos", "amuleto", "amuletos", "joya", "joyas", "joyeria", "anneau", "amulette" },

                NpcTemplateId = ShopAnilloAmuleto

            },

            new ShopSlot

            {

                Number = 4,

                Label = "Cinturon y botas",

                Aliases = new[] { "cinturon", "cinturones", "bota", "botas", "ceinture", "bottes" },

                NpcTemplateId = ShopCinturonBotas

            },

            new ShopSlot

            {

                Number = 5,

                Label = "Escudo",

                Aliases = new[] { "escudo", "escudos", "bouclier", "boucliers" },

                NpcTemplateId = ShopEscudo

            },

            new ShopSlot

            {

                Number = 6,

                Label = "Consumible",

                Aliases = new[] { "consumible", "consumibles", "pocion", "pociones", "consommable", "consommables" },

                NpcTemplateId = ShopConsumible

            },

            new ShopSlot

            {

                Number = 7,

                Label = "Recurso",

                Aliases = new[] { "recurso", "recursos", "ressource", "ressources" },

                NpcTemplateId = ShopRecurso

            },

            new ShopSlot

            {

                Number = 8,

                Label = "Dofus y mascota",

                Aliases = new[] { "dofus", "mascota", "mascotas", "familiere", "familier", "familiers", "montura", "monturas", "dragodinde" },

                NpcTemplateId = ShopDofusMascota

            },

            new ShopSlot

            {

                Number = 9,

                Label = "Diverso",

                Aliases = new[] { "diverso", "diversos", "divers", "misc", "varios" },

                NpcTemplateId = ShopDiverso

            }

        };



        public static bool TryResolveSlot(string token, out ShopSlot slot)

        {

            slot = null;

            if (string.IsNullOrWhiteSpace(token))

                return false;



            token = token.Trim().ToLowerInvariant();



            if (int.TryParse(token, out var number))

            {

                slot = Slots.FirstOrDefault(s => s.Number == number);

                return slot != null;

            }



            slot = Slots.FirstOrDefault(s =>

                s.Label.Equals(token, StringComparison.OrdinalIgnoreCase) ||

                s.Aliases.Any(a => a.Equals(token, StringComparison.OrdinalIgnoreCase)));



            return slot != null;

        }



        public static bool TryOpenShop(Character character, ShopSlot slot)

        {

            if (character == null || slot == null)

                return false;



            var templateId = slot.NpcTemplateId;

            if (!VirtualShopRegistry.Instance.TryGetShop(templateId, out var npc))

            {

                Logger.WriteWarning(

                    $"[ShopTrace] .tienda OPEN_FAIL charId={character.Id} slot={slot.Number} label={slot.Label} template={templateId} reason=not_in_registry");

                character.SendServerMessage($"Tienda {slot.Label} no disponible (id {templateId}).");

                return false;

            }



            if (character.IsInFight())

            {

                character.SendServerMessage("No puedes abrir tiendas durante un combate.");

                return false;

            }



            if (character.Dialog != null && !(character.Dialog is NpcBuySellAction))

            {

                character.SendServerMessage("Cierra el dialogo actual antes de abrir una tienda.");

                return false;

            }



            InventoryHandler.SendExchangeLeaveMessage(character.Client, true);

            character.Dialog = null;



            Logger.WriteInfo(

                $"[ShopTrace] .tienda OPEN charId={character.Id} slot={slot.Number} label={slot.Label} template={templateId} items={npc.Shops?.Count ?? 0}");

            npc.InteractWith(NpcActionTypeEnum.ACTION_BUY_SELL, character);

            return true;

        }

    }

}


