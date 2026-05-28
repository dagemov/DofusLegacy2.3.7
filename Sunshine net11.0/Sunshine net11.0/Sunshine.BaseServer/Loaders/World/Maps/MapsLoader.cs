using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World;
using Sunshine.MySql.Database.World.Maps;
using Sunshine.Protocol.Tools.D2p;
using Sunshine.Protocol.Tools.Dlm;
using Sunshine.WorldServer.Game;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Maps.Dungeons;
using Sunshine.WorldServer.Game.Maps.Interactives;
using Sunshine.WorldServer.Game.Maps.Triggers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sunshine.BaseServer.Loaders.World.Maps
{
    public static class MapsLoader
    {
        public static void Initialize()
        {
            Logs.Logger.Write("[ World ] Loading Maps...");

            var mapsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "maps", "maps0.d2p");
            var d2PFile = new D2pFile(mapsPath);
            HouseTableBootstrap.EnsureTableAndSeed();

            var maps = MapManager.Instance.GetAllMaps().ToList();
            var mapPositions = MapManager.Instance.GetAllMapPositions();
            var interactives = InteractiveManager.Instance.GetAllInteractiveSpawns();
            var triggers = MapManager.Instance.GetAllTriggerSpawns();
            var dungeons = DungeonManager.Instance.GetAllDungeons();
            MapManager.Instance.Zaapis = MapManager.Instance.GetAllZaapis();

            var mapData = d2PFile.Entries.ToDictionary(entry => int.Parse(entry.FileName.Remove(entry.FileName.IndexOf('.'))));

            foreach (var mapRecord in maps)
            {
                var map = new Map(mapRecord);

                if (mapData.TryGetValue(mapRecord.Id, out var data))
                {
                    var bytes = data.Container.ReadFile(data);
                    var dlmMap = new DlmReader(bytes).ReadMap();

                    map.Cells = dlmMap.Cells;
                    map.Elements = MapManager.Instance.GetElements(map);
                    map.Interactives = interactives.ContainsKey(mapRecord.Id) ? interactives[mapRecord.Id] : new List<Interactive>();
                    map.Triggers = triggers.ContainsKey(mapRecord.Id) ? triggers[mapRecord.Id] : new List<Trigger>();
                    map.Dungeon = dungeons.ContainsKey(mapRecord.Id) ? new Dungeon(dungeons[mapRecord.Id]) : null;
                    map.Point = mapPositions.ContainsKey(mapRecord.Id) ? new MapPoint(mapPositions[mapRecord.Id].PosX, mapPositions[mapRecord.Id].PosY) : null;
                    map.GeneratePatternCells();
                }

                if (!MapManager.Instance.SubMaps.ContainsKey(mapRecord.SubAreaId))
                    MapManager.Instance.SubMaps.Add(mapRecord.SubAreaId, new List<Map> { map });
                else
                    MapManager.Instance.SubMaps[mapRecord.SubAreaId].Add(map);

                MapManager.Instance.Maps.Add(mapRecord.Id, map);
            }

            PrismManager.Instance.Initialize();

            Logs.Logger.Write($"[ World ] {maps.Count} Maps Loaded");
        }
    }
}
