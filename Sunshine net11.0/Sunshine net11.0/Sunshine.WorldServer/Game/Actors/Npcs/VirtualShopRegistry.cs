using Sunshine.Logs;
using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Actors.Npcs
{
    /// <summary>
    /// Indexa las tiendas virtuales del comando .tiendas por su template id (Record.Id),
    /// de forma global e independiente del mapa donde este el jugador.
    ///
    /// Una "tienda" es cualquier NPC ya spawneado cuyo Record.Id pertenece al bloque
    /// reservado (>= MinShopTemplateId) y que tiene catalogo de venta (npcs_items).
    /// Esto la mantiene 100% DB-driven: anadir un npc 90xx + spawn + filas en npcs_items
    /// lo convierte en tienda automaticamente, sin tocar codigo.
    ///
    /// Se construye una sola vez al arranque (despues de NpcsLoader). Tras eso es de
    /// solo lectura, por lo que las consultas concurrentes de muchos clientes no
    /// necesitan locks ni golpean la DB.
    /// </summary>
    public class VirtualShopRegistry : Singleton<VirtualShopRegistry>
    {
        /// <summary>Bloque de ids reservado para tiendas virtuales del comando .tiendas.</summary>
        public const int MinShopTemplateId = 9000;

        private Dictionary<int, Npc> _shopsById = new Dictionary<int, Npc>();
        private List<Npc> _orderedShops = new List<Npc>();

        /// <summary>Indexa las tiendas a partir de los NPC ya spawneados. Llamar tras NpcsLoader.</summary>
        public void Initialize()
        {
            var shopsById = new Dictionary<int, Npc>();

            foreach (var npc in NpcManager.Instance.Npcs.Values.SelectMany(x => x))
            {
                if (npc.Record == null || npc.Record.Id < MinShopTemplateId)
                    continue;

                if (npc.Shops == null || npc.Shops.Count == 0)
                    continue;

                // Primer spawn gana si hubiera duplicados del mismo template.
                if (!shopsById.ContainsKey(npc.Record.Id))
                    shopsById.Add(npc.Record.Id, npc);
            }

            _shopsById = shopsById;
            _orderedShops = shopsById.Values.OrderBy(x => x.Record.Id).ToList();

            Logger.WriteInfo($"[ShopTrace] VirtualShopRegistry initialized count={_orderedShops.Count}");
            foreach (var shop in _orderedShops.Take(5))
                Logger.WriteInfo($"[ShopTrace]   shop template={shop.Record.Id} name={shop.Record.Name} items={shop.Shops?.Count ?? 0} map={shop.Spawn?.Map}");
            if (_orderedShops.Count > 5)
                Logger.WriteInfo($"[ShopTrace]   ... and {_orderedShops.Count - 5} more");
        }

        /// <summary>Cantidad de tiendas registradas.</summary>
        public int Count => _orderedShops.Count;

        /// <summary>Resuelve una tienda por su template id (Record.Id).</summary>
        public bool TryGetShop(int templateId, out Npc npc)
        {
            return _shopsById.TryGetValue(templateId, out npc);
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
