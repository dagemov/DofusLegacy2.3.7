using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.World;
using System;
using System.Collections.Generic;
using Dapper;
using Dapper.Contrib.Extensions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sunshine.WorldServer.Game;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.MySql.Database.World.Maps;
using Sunshine.Protocol.Tools.Dlm;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Maps.Triggers;
using Sunshine.WorldServer.Game.Maps.Prisms;
using Sunshine.MySql.Database.World.Maps.Triggers;
using Sunshine.Protocol.Tools.Ele;
using Sunshine.MySql.Database.World.Maps.Zaapis;
using Sunshine.WorldServer.Game.Maps.Interactives;

namespace Sunshine.MySql.Database.Managers
{
    public class MapManager : Singleton<MapManager>
    {
        public Dictionary<int, Map> Maps = new Dictionary<int, Map>();

        public Dictionary<int, List<Map>> SubMaps = new Dictionary<int, List<Map>>();

        public Dictionary<int, ZaapiRecord> Zaapis = new Dictionary<int, ZaapiRecord>();

        public Dictionary<byte, Func<WorldServer.Game.Maps.Triggers.Types.Type>> Triggers = new Dictionary<byte, Func<WorldServer.Game.Maps.Triggers.Types.Type>>();

        public Dictionary<int, List<List<short>>> cellsPattern = new Dictionary<int, List<List<short>>>
        {
            {0, new List<List<short>> { new List<short> { 319, 413, 400, 386, 373, 359, 346, 332 }, new List<short> { 268, 255, 241, 228, 214, 201, 187, 174 }} },
            {1, new List<List<short>> { new List<short> { 282, 296, 311, 325, 340, 354, 369, 383 }, new List<short> { 147, 161, 176, 190, 205, 219, 234, 248 }} }
        };

        public IEnumerable<MapRecord> GetAllMaps()
        {
            return DatabaseManager.Connection.Query<MapRecord>("SELECT * FROM worlds_maps");
        }

        public Dictionary<int, MapPositionRecord> GetAllMapPositions()
        {
            return DatabaseManager.Connection.Query<MapPositionRecord>("SELECT * FROM worlds_maps_positions").ToDictionary(x => x.Id);
        }

        public Dictionary<int, ZaapiRecord> GetAllZaapis()
        {
            return DatabaseManager.Connection.Query<ZaapiRecord>("SELECT * FROM worlds_zaapis").ToDictionary(x => x.Id);
        }

        public Dictionary<int, List<Trigger>> GetAllTriggerSpawns()
        {
            Dictionary<int, List<Trigger>> triggers = new Dictionary<int, List<Trigger>>();
            var spawns = DatabaseManager.Connection.Query<TriggerSpawn>($"SELECT * FROM worlds_triggers");
            foreach (var spawn in spawns)
            {
                if (triggers.ContainsKey(spawn.Map))
                    triggers[spawn.Map].Add(new Trigger(spawn));
                else
                    triggers.Add(spawn.Map, new List<Trigger> { new Trigger(spawn) });
            }
            return triggers;
        }

        public Map GetMap(int mapId)
        {
            if (Maps.ContainsKey(mapId))
                return Maps[mapId];
            return null;
        }

        public Map GetMap(string mapId)
        {
            if (Maps.ContainsKey(int.Parse(mapId)))
                return Maps[int.Parse(mapId)];
            return null;
        }

        public Map GetMap(Character character)
        {
            if (character == null)
                return null;

            Map map;
            if (Maps.TryGetValue(character.Record.MapId, out map))
                return map;

            if (Maps.TryGetValue(2323, out map))
                return map;

            return Maps.Values.FirstOrDefault();
        }

        public List<Map> GetMaps(int subArea)
        {
            if (SubMaps.ContainsKey(subArea))
                return SubMaps[subArea];
            return new List<Map>();
        }

        public DlmCellData[] GetCells(byte[] cells)
        {
            return new DlmReader(cells).ReadMap().Cells;
        }

        public Cell GetCell(Character character)
        {
            return new Cell(character);
        }

        public List<short> GetPatternCells(Map map, bool isBlue)
        {
            if (map == null || map.Record == null)
                return new List<short>();

            var rawCells = isBlue ? map.Record.BlueCells : map.Record.RedCells;
            if (string.IsNullOrWhiteSpace(rawCells))
                return new List<short>();

            var cells = new List<short>();

            try
            {
                BigEndianReader reader = new BigEndianReader(Utils.GetHexaToByteArray(rawCells));
                short count = reader.ReadShort();

                for (int i = 0; i < count && reader.BytesAvailable >= 2; i++)
                {
                    short cell = reader.ReadShort();
                    if (cell >= 0 && cell < 560)
                        cells.Add(cell);
                }
            }
            catch
            {
                return new List<short>();
            }

            return cells;
        }

        public List<Element> GetElements(Map map)
        {
            List<Element> elements = new List<Element>();
            if (map.Record.Elements == null || map.Record.Elements == "")
                return elements;
            byte[] data = Utils.GetHexaToByteArray(map.Record.Elements);
            BigEndianReader reader = new BigEndianReader(data);
            while(reader.BytesAvailable > 0)
            {
                var cell = reader.ReadShort();
                var element = reader.ReadUInt();
                elements.Add(new Element(cell, element));
            }
            return elements;
        }

        public Map CreateMapInstance(int mapid, int coordinateSourceMapId = 0)
        {
            var canonicalMap = this.GetMap(mapid);
            if (canonicalMap == null)
                return null;

            var coordinateSourceMap = coordinateSourceMapId > 0 ? this.GetMap(coordinateSourceMapId) : null;

            Map newMap = new Map(canonicalMap.Record)
            {
                Cells = canonicalMap.Cells != null ? (DlmCellData[])canonicalMap.Cells.Clone() : new DlmCellData[560],
                RedCells = canonicalMap.RedCells != null ? new List<short>(canonicalMap.RedCells) : new List<short>(),
                BlueCells = canonicalMap.BlueCells != null ? new List<short>(canonicalMap.BlueCells) : new List<short>(),
                RolePlayActors = canonicalMap.RolePlayActors.Where(x => !(x is Character) && !(x is PrismActor)).ToList(),
                Elements = canonicalMap.Elements != null ? new List<Element>(canonicalMap.Elements) : new List<Element>(),
                Interactives = canonicalMap.Interactives != null ? new List<Interactive>(canonicalMap.Interactives) : new List<Interactive>(),
                Triggers = canonicalMap.Triggers != null ? new List<Trigger>(canonicalMap.Triggers) : new List<Trigger>(),
                Dungeon = canonicalMap.Dungeon,
                Point = coordinateSourceMap != null && coordinateSourceMap.Point != null ? coordinateSourceMap.Point : canonicalMap.Point,
                Position = coordinateSourceMap != null ? coordinateSourceMap.Position : canonicalMap.Position,
                RuntimeInstance = true,
                CanonicalMapId = canonicalMap.Id
            };

            return newMap;
        }

        //public void InsertHarvest(int map, DlmLayer[] layers, EleInstance ele)
        //{
        //    #region Harvest
        //    string cmd = "";
        //    foreach (var layer in layers)
        //    {
        //        foreach (var cell in layer.Cells)
        //        {
        //            foreach (var element in cell.Elements.Where(x => x is DlmGraphicalElement))
        //            {
        //                var elem = element as DlmGraphicalElement;
        //                if (elem.Identifier != 0)
        //                {
        //                    var eleData = ele.GraphicalDatas.FirstOrDefault(x => x.Value.Id == elem.ElementId).Value;
        //                    if (eleData != null)
        //                    {
        //                        switch (eleData.Type)
        //                        {
        //                            case EleGraphicalElementTypes.ENTITY:
        //                                var newEle = eleData as EntityGraphicalElementData;
        //                                switch (newEle.EntityLook)
        //                                {                             
        //                                    case "{224}": // Puit
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 84, 102, 1)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{650}": // Frene
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 1, 6, 2)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{651}": // Chataigner
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 33, 39, 2)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{652}": // Noyer
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 34, 40, 2)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{653}": // Chene
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 8, 10, 2)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{654}": // Erable
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 31, 37, 2)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{655}": // If
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 28, 33, 2)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{656}": // Merisier
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 35, 41, 2)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{657}": // Ebene
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 29, 34, 2)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{658}": // Charme
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 32, 38, 2)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{659}": // Orme
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 30, 35, 2)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{660}": // Ble
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 38, 45, 28)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{661}": // Houblon
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 39, 46, 28)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{662}": // Lin
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);

        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 42, '50,68', '28,26')";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{663}": // Chanvre
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 46, '54,69', '28,26')";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{664}": // Orge
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 43, 53, 28)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{665}": // Seigle
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 44, 52, 28)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{667}": // Malt
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 47, 58, 28)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{677}": // Trefle
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 67, 71, 26)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{678}": // Menthe
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 66, 72, 26)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{679}": // Orchide
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 68, 73, 26)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{680}": // Edelweiss
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 61, 74, 26)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{681}": // Bombu
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 98, 139, 2)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{682}": // Oliviolet
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 101, 141, 2)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{683}": // Riz
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 111, 159, 28)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{684}": // Pandouille
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 112, 160, 28)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{685}": // Bambou
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 108, 154, 2)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{686}": // Bambou sombre
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 109, 155, 2)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{689}": // Kaliptus
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 121, 174, 2)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{701}": // Avoine
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 45, 57, 28)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{1018}": // Petits poissons (rivière)
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 75, 124, 36)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{1019}": // Poissons (rivière)
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 74, 125, 36)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{1020}": // Gros poissons (rivière)
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 76, 126, 36)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{1021}": // Poissons géants (rivière)
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 79, 127, 36)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{1383}": // Poisson de Frigost
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 132, 189, 36)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{1029}": // BambouSacree
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 110, 158, 2)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{1074}": // Bronze
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 55, 26, 24)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{1076}": // Dolomite
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 113, 161, 24)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{1075}": // Cuivre
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 53, 25, 24)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{1077}": // Etain
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 52, 55, 24)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{1072}": // Argent
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 24, 29, 24)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{1081}": // Fer
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 17, 24, 24)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{1079}": // Or
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 25, 30, 24)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{1290}": // Obsidienne
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 135, 192, 24)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{1245}": // Frostiz
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 134, 191, 28)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{1288}": // Perce Neige
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 131, 188, 26)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{1289}": // Tremble
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 133, 190, 2)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{3212}": // Ortie                                            
        //                                        break;
        //                                    case "{3213}": // Sauge
        //                                        break;
        //                                    case "{3222}": // Noisetier
        //                                        break;
        //                                    case "{3223}": // Belladone
        //                                        break;
        //                                    case "{3226}": // Mandragor
        //                                        break;
        //                                    case "{3228}": // Mais
        //                                        break;
        //                                    case "{3230}": // Millet
        //                                        break;
        //                                    case "{3234}": // Ginseng
        //                                        break;
        //                                    case "{1063}": // Kobalt
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 37, 28, 24)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{1078}": // Manganese
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 54, 56, 24)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{1073}": // Bauxite
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 26, 31, 24)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{1080}": // Sillicate
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 0, -1, {elem.Cell.Id})";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        cmd = $"INSERT INTO worlds_interactives(Map, Element, Type, SkillsCSV, Parameters) VALUES({map}, {elem.Identifier}, 114, 162, 24)";
        //                                        DatabaseManager.Connection.Execute(cmd);
        //                                        break;
        //                                    case "{3553}": // Ecu
        //                                        break;
        //                                }
        //                                break;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    #endregion
        //}

        //public void InsertMap(DlmMap map)
        //{
        //    string cmd = $@"INSERT INTO worlds_maps (Id, RelativeId, Version, MapType, SubAreaId, TopNeighbourId, BottomNeighbourId, 
        //                  LeftNeighbourId, RightNeighbourId, ShadowBonusOnEntities, UseLowpassFilter, UseReverb,
        //                    PresetId) VALUES({map.Id}, {map.RelativeId}, {map.Version}, {map.MapType}, {map.SubAreaId}, {map.TopNeighbourId},
        //                    {map.BottomNeighbourId}, {map.LeftNeighbourId}, {map.RightNeighbourId}, {map.ShadowBonusOnEntities}, {map.UseLowPassFilter}, 
        //                    {map.UseReverb}, {map.PresetId})";
        //    DatabaseManager.Connection.Execute(cmd);
        //}

        //public void InsertElement(IDataWriter writer, int map, DlmLayer[] layers)
        //{
        //    foreach (var layer in layers)
        //    {
        //        foreach (var cell in layer.Cells)
        //        {
        //            foreach (var element in cell.Elements.Where(x => x is DlmGraphicalElement))
        //            {
        //                var elem = element as DlmGraphicalElement;
        //                if (elem.Identifier != 0)
        //                {
        //                    writer.WriteShort(elem.Cell.Id);
        //                    writer.WriteUInt(elem.Identifier);
        //                }
        //            }
        //        }
        //    }

        //    if (writer.Data.Length > 0)
        //    {
        //        string cmd = $"UPDATE worlds_maps SET Elements = '{BitConverter.ToString(writer.Data)?.Replace("-", "")}' WHERE Id = '{map}'";
        //        DatabaseManager.Connection.Execute(cmd);
        //    }
        //}
    }
}
