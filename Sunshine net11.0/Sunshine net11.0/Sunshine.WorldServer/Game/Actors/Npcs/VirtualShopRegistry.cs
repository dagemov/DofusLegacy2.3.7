using Sunshine.Logs;
using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Npcs;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Characters;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Actors.Npcs
{
    /// <summary>
    /// Indexa las tiendas virtuales del comando .tiendas por su template id (Record.Id),
    /// de forma global e independiente del mapa donde este el jugador.
    ///
    /// Una "tienda" es cualquier NPC con Record.Id en el bloque reservado (>= MinShopTemplateId)
    /// y catalogo en npcs_items. Puede indexarse desde spawn en worlds_npcs o solo desde npcs
    /// (sin presencia visible en mapa).
    /// </summary>
    public class VirtualShopRegistry : Singleton<VirtualShopRegistry>
    {
        /// <summary>Bloque de ids reservado para tiendas virtuales del comando .tiendas.</summary>
        public const int MinShopTemplateId = 9000;

        /// <summary>Mapa ficticio para NPC virtuales sin spawn en worlds_npcs.</summary>
        private const int VirtualSpawnMapId = 0;

        private Dictionary<int, Npc> _shopsById = new Dictionary<int, Npc>();
        private List<Npc> _orderedShops = new List<Npc>();

        /// <summary>Indexa tiendas desde spawns y, si faltan, desde plantillas npcs + npcs_items.</summary>
        public void Initialize()
        {
            var shopsById = new Dictionary<int, Npc>();

            foreach (var npc in NpcManager.Instance.Npcs.Values.SelectMany(x => x))
            {
                if (npc.Record == null || npc.Record.Id < MinShopTemplateId)
                    continue;

                if (npc.Shops == null || npc.Shops.Count == 0)
                    continue;

                if (!shopsById.ContainsKey(npc.Record.Id))
                    shopsById.Add(npc.Record.Id, npc);
            }

            foreach (var template in NpcManager.Instance.GetAllNpcs())
            {
                if (template == null || template.Id < MinShopTemplateId)
                    continue;

                if (shopsById.ContainsKey(template.Id))
                    continue;

                var shops = NpcManager.Instance.GetNpcShops(template.Id);
                if (shops == null || shops.Count == 0)
                    continue;

                shopsById.Add(template.Id, CreateOffMapVirtualNpc(template));
            }

            _shopsById = shopsById;
            _orderedShops = shopsById.Values.OrderBy(x => x.Record.Id).ToList();

            Logger.WriteInfo($"[ShopTrace] VirtualShopRegistry initialized count={_orderedShops.Count}");
            foreach (var shop in _orderedShops.Take(5))
                Logger.WriteInfo($"[ShopTrace]   shop template={shop.Record.Id} name={shop.Record.Name} items={shop.Shops?.Count ?? 0} map={shop.Spawn?.Map}");
            if (_orderedShops.Count > 5)
                Logger.WriteInfo($"[ShopTrace]   ... and {_orderedShops.Count - 5} more");
        }

        private static Npc CreateOffMapVirtualNpc(NpcTemplate template)
        {
            var spawn = new NpcSpawn
            {
                Npc = template.Id,
                Map = VirtualSpawnMapId,
                Cell = 0,
                Direction = 1
            };
            return new Npc(template, spawn);
        }

        /// <summary>Cantidad de tiendas registradas.</summary>
        public int Count => _orderedShops.Count;

        /// <summary>Resuelve una tienda por su template id (Record.Id).</summary>
        public bool TryGetShop(int templateId, out Npc npc)
        {
            return _shopsById.TryGetValue(templateId, out npc);
        }

        /// <summary>
        /// sellerId seguro para ExchangeStartOkNpcShopMessage en tiendas .tienda.
        /// 1) NPC spawneado en el mapa con el mismo template (como tienda normal).
        /// 2) actor id del registry virtual (nunca template 9000+: colisiona con ids runtime en mapas concurridos).
        /// </summary>
        public int ResolveVirtualSellerId(Character character, Npc shopNpc)
        {
            if (shopNpc?.Record == null)
                return shopNpc?.Id ?? 0;

            var templateId = shopNpc.Record.Id;
            var map = character?.Map;

            if (map != null && NpcManager.Instance.Npcs.TryGetValue(map.Id, out var mapNpcs))
            {
                var onMap = mapNpcs.FirstOrDefault(n => n.Record?.Id == templateId);
                if (onMap != null)
                {
                    Logger.WriteInfo(
                        $"[ShopTrace] sellerId=mapActor charId={character.Id} mapId={map.Id} template={templateId} actor={onMap.Id}");
                    return onMap.Id;
                }
            }

            Logger.WriteInfo(
                $"[ShopTrace] sellerId=registryActor charId={character?.Id} mapId={map?.Id ?? 0} template={templateId} actor={shopNpc.Id}");
            return shopNpc.Id;
        }

        /// <summary>Primera tienda (menor Record.Id), la que abre .tiendas por defecto.</summary>
        public Npc GetFirstShop()
        {
            return _orderedShops.FirstOrDefault();
        }

        /// <summary>Lista ordenada de tiendas (para el directorio del desplegable).</summary>
        public IReadOnlyList<Npc> GetOrderedShops()
        {
            return _orderedShops;
        }

        /// <summary>
        /// Directorio (id de template + nombre) para poblar el desplegable cliente.
        /// 100% DB-driven via npcs.Name.
        /// </summary>
        public IEnumerable<KeyValuePair<int, string>> GetDirectory()
        {
            return _orderedShops.Select(x => new KeyValuePair<int, string>(x.Record.Id, x.Record.Name ?? string.Empty));
        }
    }
}
