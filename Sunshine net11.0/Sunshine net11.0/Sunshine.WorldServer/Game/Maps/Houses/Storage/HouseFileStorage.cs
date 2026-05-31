using Sunshine.Logs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sunshine.WorldServer.Game.Maps.Houses.Storage
{
    public static class HouseFileStorage
    {
        private const char Separator = ';';

        public static string DefinitionsPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "houses.txt");
        public static string StatePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "houses_state.txt");

        public static List<HouseStorageEntry> Load()
        {
            EnsureFiles();
            var definitions = LoadDefinitions();
            var state = LoadState().ToDictionary(x => x.Id, x => x);

            foreach (var house in definitions)
            {
                if (!state.ContainsKey(house.Id))
                    continue;

                var saved = state[house.Id];
                house.OwnerId = saved.OwnerId;
                house.OwnerName = saved.OwnerName;
                house.OnSale = saved.OnSale;
                house.SaleLocked = saved.SaleLocked;
                house.Locked = saved.Locked;
                house.Code = saved.Code;
                house.Price = saved.Price;
                house.ChestCode = saved.ChestCode;
            }

            return definitions;
        }

        public static void Save(IEnumerable<HouseStorageEntry> houses)
        {
            EnsureFiles();
            var lines = new List<string>
            {
                "# id;ownerId;ownerName;onSale;saleLocked;locked;code;price;chestCode"
            };

            foreach (var house in houses.OrderBy(x => x.Id))
            {
                lines.Add(string.Join(Separator.ToString(), new[]
                {
                    house.Id.ToString(),
                    house.OwnerId?.ToString() ?? string.Empty,
                    Escape(house.OwnerName),
                    BoolToString(house.OnSale),
                    BoolToString(house.SaleLocked),
                    BoolToString(house.Locked),
                    Escape(house.Code),
                    house.Price?.ToString() ?? string.Empty,
                    Escape(house.ChestCode)
                }));
            }

            File.WriteAllLines(StatePath, lines);
        }

        private static List<HouseStorageEntry> LoadDefinitions()
        {
            var results = new List<HouseStorageEntry>();
            foreach (var line in ReadDataLines(DefinitionsPath))
            {
                var parts = line.Split(Separator);
                if (parts.Length < 11)
                    continue;

                int id, mapId, enterMapId, enterCellId, endMapIdInstance, endCellIdIInstance, instanceMapCellId, modelId, interactiveId, defaultPrice;
                bool hasChest;
                if (!int.TryParse(parts[0], out id) ||
                    !int.TryParse(parts[1], out mapId) ||
                    !int.TryParse(parts[2], out enterMapId) ||
                    !int.TryParse(parts[3], out enterCellId) ||
                    !int.TryParse(parts[4], out endMapIdInstance) ||
                    !int.TryParse(parts[5], out endCellIdIInstance) ||
                    !int.TryParse(parts[6], out instanceMapCellId) ||
                    !int.TryParse(parts[7], out modelId) ||
                    !int.TryParse(parts[8], out interactiveId) ||
                    !int.TryParse(parts[9], out defaultPrice) ||
                    !TryParseBool(parts[10], out hasChest))
                    continue;

                results.Add(new HouseStorageEntry
                {
                    Id = id,
                    MapId = mapId,
                    EnterMapId = enterMapId,
                    EnterCellId = enterCellId,
                    EndMapIdInstance = endMapIdInstance,
                    EndCellIdIInstance = endCellIdIInstance,
                    InstanceMapCellId = instanceMapCellId,
                    ModelId = modelId,
                    InteractiveId = interactiveId,
                    DefaultPrice = defaultPrice,
                    HasChest = hasChest,
                    InteriorMapsCSV = parts.Length > 11 ? Unescape(parts[11]) : string.Empty,
                    SkillListIdsCSV = parts.Length > 12 ? Unescape(parts[12]) : string.Empty,
                    OwnerName = string.Empty,
                    Code = string.Empty,
                    ChestCode = string.Empty
                });
            }
            return results;
        }

        private static List<HouseStorageEntry> LoadState()
        {
            var results = new List<HouseStorageEntry>();
            foreach (var line in ReadDataLines(StatePath))
            {
                var parts = line.Split(Separator);
                if (parts.Length < 9)
                    continue;

                int id;
                if (!int.TryParse(parts[0], out id))
                    continue;

                int ownerIdValue;
                int priceValue;
                bool onSale, saleLocked, locked;

                if (!TryParseBool(parts[3], out onSale) || !TryParseBool(parts[4], out saleLocked) || !TryParseBool(parts[5], out locked))
                    continue;

                results.Add(new HouseStorageEntry
                {
                    Id = id,
                    OwnerId = int.TryParse(parts[1], out ownerIdValue) ? ownerIdValue : (int?)null,
                    OwnerName = Unescape(parts[2]),
                    OnSale = onSale,
                    SaleLocked = saleLocked,
                    Locked = locked,
                    Code = Unescape(parts[6]),
                    Price = int.TryParse(parts[7], out priceValue) ? priceValue : (int?)null,
                    ChestCode = Unescape(parts[8])
                });
            }
            return results;
        }

        private static IEnumerable<string> ReadDataLines(string path)
        {
            return File.ReadAllLines(path)
                .Select(x => x?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#"));
        }

        private static void EnsureFiles()
        {
            var dataDir = Path.GetDirectoryName(DefinitionsPath);
            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);

            if (!File.Exists(DefinitionsPath))
            {
                File.WriteAllLines(DefinitionsPath, new[]
                {
                    "# id;mapId;enterMapId;enterCellId;endMapIdInstance;endCellIdIInstance;instanceMapCellId;modelId;interactiveId;defaultPrice;hasChest;interiorMapsCSV;skillListIdsCSV",
                    "# 1;2323;5001;250;2323;0;0;186;437644;100000;1;5001;81,84,97,98,100,104,105,106,108"
                });
                Logger.WriteInfo($"houses.txt created in {DefinitionsPath}");
            }

            if (!File.Exists(StatePath))
                File.WriteAllLines(StatePath, new[] { "# id;ownerId;ownerName;onSale;saleLocked;locked;code;price;chestCode" });
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace(";", "\\;");
        }

        private static string Unescape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var result = new System.Text.StringBuilder();
            bool escaped = false;
            foreach (var c in value)
            {
                if (escaped)
                {
                    result.Append(c);
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }

                result.Append(c);
            }
            return result.ToString();
        }

        private static string BoolToString(bool value) => value ? "1" : "0";

        private static bool TryParseBool(string value, out bool result)
        {
            result = value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
            return value == "1" || value == "0" || value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("false", StringComparison.OrdinalIgnoreCase);
        }
    }
}
