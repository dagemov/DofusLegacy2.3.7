using Dapper;
using Dapper.Contrib.Extensions;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Maps.Houses;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.Protocol.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Maps.Houses
{
    public class HouseManager : Singleton<HouseManager>
    {
        public Dictionary<int, House> Houses { get; private set; } = new Dictionary<int, House>();

        public void Load()
        {
            HouseTableBootstrap.EnsureTableAndSeed();

            var records = DatabaseManager.Connection
                .Query<WorldMapHouseRecord>(@"
SELECT
    h.*,
    CASE
        WHEN h.`Map` IS NULL OR h.`Map` = 0 THEN h.`EnterMapId`
        ELSE h.`Map`
    END AS `HouseInteractiveMap`,
    h.`ElementId` AS `HouseInteractiveElementId`,
    -1 AS `HouseInteractiveType`,
    h.`SkillsCSV` AS `HouseInteractiveSkillsCSV`,
    h.`ParametersCSV` AS `HouseInteractiveParametersCSV`
FROM `world_maps_house` h")
                .ToArray();

            Houses = records
                .Where(IsValid)
                .ToDictionary(x => x.Id, x => new House(x));

            Logs.Logger.WriteInfo($"{Houses.Count} houses loaded from world_maps_house");
        }

        private bool IsValid(WorldMapHouseRecord record)
        {
            if (record == null)
                return false;

            if (record.Map == null)
            {
                Logs.Logger.WriteError($"[HOUSES] House {record.Id} ignored: exterior map {record.MapId} not found.");
                return false;
            }

            if (record.EnterMap == null)
            {
                Logs.Logger.WriteError($"[HOUSES] House {record.Id} ignored: interior map {record.EnterMapId} not found.");
                return false;
            }

            if (record.Map.Interactives == null ||
                !record.Map.Interactives.Any(x => x != null && x.Element == record.InteractiveId && !x.IsHouseInteractive))
            {
                Logs.Logger.WriteError($"[HOUSES] House {record.Id} ignored: exterior door interactive {record.InteractiveId} not found on map {record.MapId}.");
                return false;
            }

            return true;
        }

        public IEnumerable<House> GetHouses()
        {
            return Houses.Values;
        }

        public House GetHouse(int id)
        {
            House house;
            return Houses.TryGetValue(id, out house) ? house : null;
        }

        public IEnumerable<House> GetHousesByMap(int mapId)
        {
            return Houses.Values.Where(x => x.Map != null && x.Map.Id == mapId);
        }

        public House GetHouseByInteractive(int interactiveId)
        {
            return Houses.Values.FirstOrDefault(x => x.InteractiveId == interactiveId);
        }

        public House GetHouseByExteriorMapId(int mapId)
        {
            return Houses.Values.FirstOrDefault(x =>
                x.Map != null &&
                x.Map.Id == mapId);
        }

        public bool CanDisplayHouseInteractive(Character character, int houseId, int mapId, int elementId)
        {
            return CanAccessHouseInteractive(character, houseId, mapId, elementId, false);
        }

        public bool CanUseHouseInteractive(Character character, int houseId, int mapId, int elementId)
        {
            return CanAccessHouseInteractive(character, houseId, mapId, elementId, false);
        }

        private bool CanAccessHouseInteractive(Character character, int houseId, int mapId, int elementId, bool allowExterior)
        {
            var house = GetHouse(houseId);
            if (house == null)
                return false;

            if (allowExterior && IsHouseExteriorInteractive(house, mapId, elementId))
                return true;

            var isHouseInteriorMap = house.ContainsInteriorMap(mapId) || house.HouseInteractiveMapId == mapId;
            if (!isHouseInteriorMap)
                return false;

            if (elementId > 0 && house.HouseInteractiveElementId > 0 && elementId != house.HouseInteractiveElementId)
                return false;

            var linkedHouse = character?.LastTargetedHouse;
            if (linkedHouse != null)
                return linkedHouse.Id == house.Id;

            var linkedHouseId = character != null && character.Record != null && character.Record.HouseId.HasValue
                ? character.Record.HouseId.Value
                : 0;
            return linkedHouseId == house.Id;
        }

        private bool IsHouseExteriorInteractive(House house, int mapId, int elementId)
        {
            if (house == null || house.Map == null || house.Map.Id != mapId)
                return false;

            if (elementId <= 0)
                return true;

            return house.InteractiveId == elementId;
        }


        public House GetHouseByExteriorInteractive(int mapId, int interactiveId)
        {
            return Houses.Values.FirstOrDefault(x =>
                x.Map != null &&
                x.Map.Id == mapId &&
                x.InteractiveId == interactiveId);
        }

        public House ResolveChestHouse(Character character, int interactiveElement = 0)
        {
            if (character == null || character.Map == null)
                return null;

            var currentMapId = character.Map.Id;
            var preferredHouse = character.LastTargetedHouse;

            // Les coffres de maison sont codés dans worlds_interactives.
            // Ne pas dépendre de world_maps_house.HasChest / Type / ElementId ici,
            // sinon le coffre peut devenir introuvable après changement de map.
            if (preferredHouse != null && preferredHouse.ContainsInteriorMap(currentMapId))
                return preferredHouse;

            var linkedHouseId = character.Record != null && character.Record.HouseId.HasValue
                ? character.Record.HouseId.Value
                : 0;

            if (linkedHouseId > 0)
            {
                var linkedHouse = GetHouse(linkedHouseId);
                if (linkedHouse != null && linkedHouse.ContainsInteriorMap(currentMapId))
                    return linkedHouse;
            }

            var resolved = ResolveInteriorHouse(character, interactiveElement);
            if (resolved != null && resolved.ContainsInteriorMap(currentMapId))
                return resolved;

            var matches = GetHousesByInteriorMapId(currentMapId)
                .Where(x => x != null)
                .ToList();

            if (matches.Count == 0)
                return null;

            if (preferredHouse != null)
            {
                var sameHouse = matches.FirstOrDefault(x => x.Id == preferredHouse.Id);
                if (sameHouse != null)
                    return sameHouse;
            }

            if (matches.Count == 1)
                return matches[0];

            return null;
        }
        public IEnumerable<House> GetHousesByInteriorMapId(int mapId)
        {
            return Houses.Values.Where(x => x.ContainsInteriorMap(mapId));
        }

        public House GetHouseByInteriorMapId(int mapId)
        {
            return GetHousesByInteriorMapId(mapId).FirstOrDefault();
        }

        public IEnumerable<House> GetHousesByPrimaryInteriorMapId(int mapId)
        {
            return Houses.Values.Where(x => x.IsPrimaryInteriorMap(mapId));
        }


        public House ResolveInteriorHouse(Character character, int interactiveElement = 0)
        {
            if (character == null || character.Map == null)
                return null;

            int mapId = character.Map.Id;
            short currentCell = character.Cell != null ? character.Cell.Id : (short)-1;
            var preferredHouse = character.LastTargetedHouse;
            if (preferredHouse != null && preferredHouse.ContainsInteriorMap(mapId))
                return preferredHouse;

            var matches = GetHousesByInteriorMapId(mapId).ToList();
            if (matches.Count == 0)
                return null;

            if (currentCell > 0)
            {
                var byTriggerCell = matches.FirstOrDefault(x => x.MatchesInteriorExitTrigger(mapId, currentCell));
                if (byTriggerCell != null)
                    return byTriggerCell;
            }

            if (matches.Count == 1)
                return matches[0];

            if (interactiveElement > 0)
            {
                var byInteractive = matches.FirstOrDefault(x =>
                    x.HouseInteractiveElementId == interactiveElement);
                if (byInteractive != null)
                    return byInteractive;
            }

            return preferredHouse != null && matches.Contains(preferredHouse) ? preferredHouse : null;
        }

        public House ResolveInteriorHouseByTriggerCell(int interiorMapId, short cellId, Character character = null)
        {
            House preferredHouse = character?.LastTargetedHouse;
            if (preferredHouse != null &&
                preferredHouse.MatchesInteriorExitTrigger(interiorMapId, cellId))
            {
                return preferredHouse;
            }

            // Exit trigger resolution must be based on the house's canonical interior map
            // (EnterMapId) to avoid conflicts with InterorMapsIdsCSV helper maps.
            //
            // Important: if several houses share the same EnterMapId and the same exit trigger
            // cell, never pick one arbitrarily. Prefer the house identity already tracked on the
            // character (LastTargetedHouse / house Id). Without that, returning the first match
            // can teleport the player through another house's exterior.
            var matches = GetHousesByPrimaryInteriorMapId(interiorMapId)
                .Where(x => x.MatchesInteriorExitTrigger(interiorMapId, cellId))
                .ToList();

            if (matches.Count == 0)
                return null;

            if (preferredHouse != null)
            {
                var sameHouse = matches.FirstOrDefault(x => x.Id == preferredHouse.Id);
                if (sameHouse != null)
                    return sameHouse;
            }

            if (matches.Count == 1)
                return matches[0];

            return null;
        }

        public IEnumerable<HouseInformations> GetHousesInformationsByMap(int mapId, Client.WorldClient client = null)
        {
            return GetHousesByMap(mapId)
                .Where(x =>
                    x.Map != null &&
                    x.Map.Interactives != null &&
                    x.Map.Interactives.Any(i => i.Element == x.InteractiveId && !i.IsHouseInteractive))
                .Select(x => x.GetInformations(client))
                .ToArray();
        }

        public bool AccountAlreadyOwnsHouse(int accountId)
        {
            return Houses.Values.Any(x =>
                x.OwnerId.HasValue &&
                x.OwnerId.Value == accountId);
        }

        public List<House> GetHousesToSell()
        {
            return Houses.Values
                .Where(x => x.OnSale)
                .ToList();
        }

        public void Save()
        {
            foreach (var house in Houses.Values.Where(x => x.IsDirty))
            {
                DatabaseManager.Connection.Update(house.Record);
                house.IsDirty = false;
            }
        }
    }
}