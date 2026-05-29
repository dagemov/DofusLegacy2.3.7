using Dapper;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.World.Maps.Houses;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Game.Items;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Guilds;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Characters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Sunshine.MySql.Database.Managers;

namespace Sunshine.WorldServer.Game.Maps.Houses
{
    public class House
    {
        public const int MaxChestWeight = 1000;
        public const long MaxChestKamas = 2000000000L;
        public const int HouseCodeLength = 5;
        public const int ChestCodeLength = 8;

        public WorldMapHouseRecord Record { get; private set; }
        public bool IsDirty { get; set; }

        private readonly Dictionary<int, List<BasePlayerItem>> _chestItems = new Dictionary<int, List<BasePlayerItem>>();

        private static readonly HashSet<int> ExteriorSkillIds = new HashSet<int> { 81, 84, 97, 98, 100, 108 };
        private static readonly HashSet<int> ChestSkillIds = new HashSet<int> { 104, 105, 106 };

        public House(WorldMapHouseRecord record)
        {
            Record = record;
            LoadChestState();
        }

        public int Id => Record.Id;
        public int InteractiveId => Record.InteractiveId;
        public int HouseInteractiveMapId => Record.HouseInteractiveMap > 0 ? Record.HouseInteractiveMap : Record.EnterMapId;
        public int HouseInteractiveElementId => Record.HouseInteractiveElementId > 0 ? Record.HouseInteractiveElementId : Record.ElementId;
        public int HouseInteractiveType => -1;
        public string HouseInteractiveSkillsCSV => !string.IsNullOrWhiteSpace(Record.HouseInteractiveSkillsCSV) ? Record.HouseInteractiveSkillsCSV : Record.SkillsCSV ?? string.Empty;
        public string HouseInteractiveParametersCSV => Record.HouseInteractiveParametersCSV ?? string.Empty;
        public Map Map => Record.Map;
        public Map EnterMap => Record.EnterMap;
        public short EnterCellId => (short)Record.EnterCellId;
        public Map EndMapInstance => Record.EndMapInstance;
        public short EndCellIdInstance => (short)Math.Max(0, Record.EndCellIdIInstance);
        public short InstanceMapCellId => (short)Math.Max(0, Record.InstanceMapCellId);

        public int? OwnerId
        {
            get => Record.OwnerId;
            set
            {
                Record.OwnerId = value;
                IsDirty = true;
            }
        }

        public string OwnerName
        {
            get => Record.OwnerName ?? string.Empty;
            set
            {
                Record.OwnerName = value;
                IsDirty = true;
            }
        }

        public bool OnSale
        {
            get => Record.OnSale;
            set
            {
                Record.OnSale = value;
                IsDirty = true;
            }
        }

        public bool SaleLocked
        {
            get => Record.SaleLocked;
            set
            {
                Record.SaleLocked = value;
                IsDirty = true;
            }
        }

        public bool Locked
        {
            get => Record.Locked;
            set
            {
                Record.Locked = value;
                IsDirty = true;
            }
        }

        public string Code
        {
            get => Record.Code ?? string.Empty;
            set
            {
                Record.Code = NormalizeDigits(value, HouseCodeLength);
                IsDirty = true;
            }
        }

        public int Price
        {
            get => Record.Price ?? Record.DefaultPrice;
            set
            {
                Record.Price = value;
                IsDirty = true;
            }
        }

        public int ModelId => Record.ModelId;
        public bool HasChest => Record.HasChest;

        public long ChestKamas
        {
            get => Record.ChestKamas;
            set
            {
                Record.ChestKamas = value;
                IsDirty = true;
            }
        }

        public string ChestCode
        {
            get => Record.ChestCode ?? string.Empty;
            set
            {
                Record.ChestCode = NormalizeDigits(value, ChestCodeLength);
                IsDirty = true;
            }
        }

        public int? GuildId
        {
            get => Record.GuildId;
            set
            {
                Record.GuildId = value;
                IsDirty = true;
            }
        }

        public uint GuildShareParams
        {
            get => Record.GuildShareParams ?? 0;
            set
            {
                Record.GuildShareParams = value;
                IsDirty = true;
            }
        }

        public bool IsOwner(Character character)
        {
            if (character == null || !OwnerId.HasValue)
                return false;

            // Compatibilité Sunshine/Stump/migrations existantes :
            // selon les sources de données, OwnerId peut référencer soit l'Account.Id,
            // soit le Character.Id. On accepte donc les deux représentations.
            if (character.Account != null && OwnerId.Value == character.Account.Id)
                return true;

            if (OwnerId.Value == character.Id)
                return true;

            // Fallback souple pour d'anciennes données où seul le nom a été conservé.
            if (!string.IsNullOrWhiteSpace(OwnerName))
            {
                if (string.Equals(OwnerName, character.Name, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (character.Account != null &&
                    string.Equals(OwnerName, character.Account.Nickname, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public bool HasGuildRight(HouseRightsBitEnum right)
        {
            return (GuildShareParams & (uint)right) != 0;
        }

        public bool IsGuildShared()
        {
            return GuildId.HasValue && HasGuildRight(HouseRightsBitEnum.HOUSE_SHARE_GUILD);
        }

        public bool IsGuildMember(Character character)
        {
            return character?.Guild != null &&
                   GuildId.HasValue &&
                   character.Guild.Id == GuildId.Value;
        }

        public bool IsGuildMemberAllowed(Character character)
        {
            return IsGuildMember(character) &&
                   HasGuildRight(HouseRightsBitEnum.HOUSE_ALLOW_GUILD_MEMBER_ACCESS);
        }

        public bool IsGuildMemberAllowedChest(Character character)
        {
            return IsGuildMember(character) &&
                   HasGuildRight(HouseRightsBitEnum.HOUSE_ALLOW_GUILD_MEMBER_ACCESS_CHEST);
        }

        public bool ShouldDisplayGuildEmblem(Character viewer)
        {
            if (!GuildId.HasValue)
                return false;

            if (HasGuildRight(HouseRightsBitEnum.HOUSE_GUILD_DOORS_LIST_OTHERS))
                return true;

            return IsGuildMember(viewer) &&
                   HasGuildRight(HouseRightsBitEnum.HOUSE_GUILD_DOORS_LIST_GUILD);
        }

        public bool ContainsInteriorMap(int mapId)
        {
            return Record != null &&
                   Record.Interiors != null &&
                   Record.Interiors.Contains((uint)mapId);
        }

        public bool IsPrimaryInteriorMap(int mapId)
        {
            return EnterMap != null && EnterMap.Id == mapId;
        }

        public bool MatchesInteriorExitTrigger(int mapId, short cellId)
        {
            // For exiting an instanced house, only the canonical interior map (EnterMapId)
            // must be considered. Using InterorMapsIdsCSV here can resolve the wrong house
            // when several houses share helper interior maps and the same exit trigger cell.
            if (!IsPrimaryInteriorMap(mapId))
                return false;

            if (InstanceMapCellId <= 0)
                return false;

            return InstanceMapCellId == cellId;
        }
        public Map GetExitMap()
        {
            return EndMapInstance ?? Map;
        }

        public short ResolveExitCell(Map targetMap = null)
        {
            targetMap = targetMap ?? GetExitMap();
            if (targetMap == null)
                return -1;

            short configuredCell = EndCellIdInstance;
            if (configuredCell > 0 && configuredCell < targetMap.Cells.Length && targetMap.Cells[configuredCell].Walkable)
                return configuredCell;

            var exteriorElement = Map?.Elements?.FirstOrDefault(x => x.Id == (uint)InteractiveId);
            if (Map != null && targetMap.Id == Map.Id && exteriorElement != null)
            {
                short anchorCell = exteriorElement.Cell;
                var nearCell = targetMap.Cells
                    .Where(x => x.Walkable)
                    .OrderBy(x => Math.Abs(x.Id - anchorCell))
                    .FirstOrDefault();

                if (!nearCell.Equals(default(Protocol.Tools.Dlm.DlmCellData)))
                    return nearCell.Id;
            }

            var fallbackCell = targetMap.Cells.FirstOrDefault(x => x.Walkable);
            return fallbackCell.Equals(default(Protocol.Tools.Dlm.DlmCellData)) ? (short)-1 : fallbackCell.Id;
        }

        public bool TryGetExitDestination(out int mapId, out short cellId)
        {
            mapId = 0;
            cellId = -1;

            var exitMap = GetExitMap();
            if (exitMap == null)
                return false;

            var exitCell = ResolveExitCell(exitMap);
            if (exitCell < 0)
                return false;

            mapId = exitMap.Id;
            cellId = exitCell;
            return true;
        }
        public HouseInformations GetHouseInformations()
        {
            return new HouseInformations(
                OnSale,
                SaleLocked,
                Record.Id,
                new[] { Record.InteractiveId },
                OwnerName ?? "?",
                (short)ModelId
            );
        }

        public HouseInformationsInside GetHouseInformationsInside()
        {
            short worldX = 0;
            short worldY = 0;

            if (Map != null && Map.Position != null)
            {
                worldX = (short)Map.Position.X;
                worldY = (short)Map.Position.Y;
            }

            return new HouseInformationsInside(
                Id,
                (short)ModelId,
                OwnerId < 0 ? 0 : (int)OwnerId,
                OwnerName ?? string.Empty,
                worldX,
                worldY,
                (uint)Math.Max(0, Price),
                Locked
            );
        }
        public bool CanEnter(Character character)
        {
            if (character == null)
                return false;

            if (IsOwner(character))
                return true;

            if (IsGuildMemberAllowed(character))
                return true;

            if (RequiresCode(character))
                return false;

            if (HasGuildRight(HouseRightsBitEnum.HOUSE_FORBID_OTHERS_ACCESS))
                return false;

            return true;
        }

        public bool RequiresCode(Character character)
        {
            if (character == null)
                return false;

            if (!Locked || string.IsNullOrWhiteSpace(Code))
                return false;

            if (IsOwner(character))
                return false;

            if (IsGuildMemberAllowed(character))
                return false;

            if (HasGuildRight(HouseRightsBitEnum.HOUSE_FORBID_OTHERS_ACCESS))
                return false;

            return true;
        }

        public bool CanOpenChest(Character character)
        {
            if (character == null)
                return false;

            if (IsOwner(character))
                return true;

            if (IsGuildMemberAllowedChest(character))
                return true;

            if (RequiresChestCode(character))
                return false;

            if (HasGuildRight(HouseRightsBitEnum.HOUSE_FORBID_OTHERS_ACCESS_CHEST))
                return false;

            return true;
        }

        public bool RequiresChestCode(Character character)
        {
            if (character == null)
                return false;

            if (string.IsNullOrWhiteSpace(ChestCode))
                return false;

            if (IsOwner(character))
                return false;

            if (IsGuildMemberAllowedChest(character))
                return false;

            if (HasGuildRight(HouseRightsBitEnum.HOUSE_FORBID_OTHERS_ACCESS_CHEST))
                return false;

            return true;
        }

        public bool CheckCode(string code)
        {
            return NormalizeDigits(code, HouseCodeLength) == NormalizeDigits(Code, HouseCodeLength) &&
                   !string.IsNullOrWhiteSpace(Code);
        }

        public bool CheckChestCode(string code)
        {
            return NormalizeDigits(code, ChestCodeLength) == NormalizeDigits(ChestCode, ChestCodeLength) &&
                   !string.IsNullOrWhiteSpace(ChestCode);
        }

        public bool SetHouseCode(string code)
        {
            code = NormalizeDigits(code, HouseCodeLength);
            if (code.Length != HouseCodeLength)
                return false;

            Code = code;
            Locked = true;
            return true;
        }

        public bool SetChestCode(string code)
        {
            code = NormalizeDigits(code, ChestCodeLength);
            if (code.Length != ChestCodeLength)
                return false;

            ChestCode = code;
            return true;
        }

        public void UnlockChest()
        {
            ChestCode = string.Empty;
        }

        private void LoadChestState()
        {
            try
            {
                _chestItems.Clear();

                const string sql = @"SELECT HouseId, ItemUid, TemplateId, Stack, Position, Effects
FROM house_chest_items
WHERE HouseId = @HouseId
ORDER BY TemplateId, ItemUid";

                var rows = DatabaseManager.Connection.Query(sql, new { HouseId = Id });

                foreach (var row in rows)
                {
                    int templateId = (int)row.TemplateId;
                    int uid = (int)row.ItemUid;
                    int stack = (int)row.Stack;
                    int position = (int)row.Position;
                    string effectsRaw = row.Effects == null ? string.Empty : (string)row.Effects;

                    if (!ItemManager.Instance.Items.ContainsKey(templateId))
                        continue;

                    var item = new BasePlayerItem(templateId)
                    {
                        Id = uid,
                        Stack = stack,
                        Position = (CharacterInventoryPositionEnum)position,
                        Effects = DeserializeEffects(effectsRaw),
                        EffectSets = new List<List<Effect>>()
                    };

                    if (_chestItems.ContainsKey(templateId))
                        _chestItems[templateId].Add(item);
                    else
                        _chestItems.Add(templateId, new List<BasePlayerItem> { item });
                }
            }
            catch
            {
            }
        }

        private void SaveChestState()
        {
            try
            {
                using (var transaction = DatabaseManager.Connection.BeginTransaction())
                {
                    DatabaseManager.Connection.Execute(
                        "DELETE FROM house_chest_items WHERE HouseId = @HouseId",
                        new { HouseId = Id },
                        transaction);

                    const string insertSql = @"INSERT INTO house_chest_items
(HouseId, ItemUid, TemplateId, Stack, Position, Effects)
VALUES
(@HouseId, @ItemUid, @TemplateId, @Stack, @Position, @Effects);";

                    foreach (var item in GetChestItems().OrderBy(x => x.Template.Id).ThenBy(x => x.Id))
                    {
                        DatabaseManager.Connection.Execute(insertSql, new
                        {
                            HouseId = Id,
                            ItemUid = item.Id,
                            TemplateId = item.Template.Id,
                            Stack = item.Stack,
                            Position = (int)item.Position,
                            Effects = SerializeEffects(item.Effects)
                        }, transaction);
                    }

                    transaction.Commit();
                }
            }
            catch
            {
            }
        }

        private string SerializeEffects(IEnumerable<Effect> effects)
        {
            if (effects == null)
                return string.Empty;

            return string.Join(",", effects.Select(x => string.Join(
                ":",
                ((int)x.Id).ToString(CultureInfo.InvariantCulture),
                x.DiceNum.ToString(CultureInfo.InvariantCulture),
                x.DiceFace.ToString(CultureInfo.InvariantCulture),
                x.Value.ToString(CultureInfo.InvariantCulture),
                x.Delay.ToString(CultureInfo.InvariantCulture),
                x.Duration.ToString(CultureInfo.InvariantCulture),
                ((int)x.Target).ToString(CultureInfo.InvariantCulture),
                ((int)x.ZoneShape).ToString(CultureInfo.InvariantCulture),
                x.ZoneMinSize.ToString(CultureInfo.InvariantCulture),
                x.ZoneSize.ToString(CultureInfo.InvariantCulture))));
        }

        private List<Effect> DeserializeEffects(string value)
        {
            var results = new List<Effect>();

            if (string.IsNullOrWhiteSpace(value))
                return results;

            foreach (var chunk in value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = chunk.Split(':');
                if (parts.Length < 10)
                    continue;

                int id;
                uint diceNum;
                uint diceFace;
                int amount;
                int delay;
                int duration;
                int target;
                int zoneShape;
                uint zoneMinSize;
                uint zoneSize;

                if (!int.TryParse(parts[0], out id) ||
                    !uint.TryParse(parts[1], out diceNum) ||
                    !uint.TryParse(parts[2], out diceFace) ||
                    !int.TryParse(parts[3], out amount) ||
                    !int.TryParse(parts[4], out delay) ||
                    !int.TryParse(parts[5], out duration) ||
                    !int.TryParse(parts[6], out target) ||
                    !int.TryParse(parts[7], out zoneShape) ||
                    !uint.TryParse(parts[8], out zoneMinSize) ||
                    !uint.TryParse(parts[9], out zoneSize))
                    continue;

                results.Add(new Effect((EffectsEnum)id, diceNum, diceFace, amount, delay, duration, (SpellTargetType)target, (SpellShapeEnum)zoneShape, zoneMinSize, zoneSize));
            }

            return results;
        }

        public void ClearChestState()
        {
            _chestItems.Clear();
            ChestKamas = 0;
            SaveChestState();
        }

        public IEnumerable<BasePlayerItem> GetChestItems()
        {
            var result = new List<BasePlayerItem>();
            foreach (var items in _chestItems.Values)
                result.AddRange(items);
            return result;
        }

        public int GetChestWeight()
        {
            return GetChestItems().Sum(x => x.Weight * Math.Max(1, x.Stack));
        }

        public bool CanStoreInChest(BasePlayerItem item, int quantity)
        {
            if (item == null || quantity <= 0)
                return false;

            long addedWeight = (long)item.Weight * quantity;
            return GetChestWeight() + addedWeight <= MaxChestWeight;
        }

        public BasePlayerItem GetChestItemUid(int uid)
        {
            foreach (var items in _chestItems.Values)
            {
                var item = items.FirstOrDefault(x => x.Id == uid);
                if (item != null)
                    return item;
            }
            return null;
        }

        public BasePlayerItem GetChestItem(int templateId, CharacterInventoryPositionEnum position, List<Effect> effects)
        {
            if (!_chestItems.ContainsKey(templateId))
                return null;

            return _chestItems[templateId].FirstOrDefault(x =>
                x.Position == position &&
                IsSameEffects(x.Effects, effects));
        }

        private bool IsSameEffects(List<Effect> effects, List<Effect> secondEffects)
        {
            if (effects == null && secondEffects == null)
                return true;

            if (effects == null || secondEffects == null)
                return false;

            if (effects.Count != secondEffects.Count)
                return false;

            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].Value != secondEffects[i].Value)
                    return false;
            }

            return true;
        }

        public void AddChestItem(BasePlayerItem item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
                return;

            if (!CanStoreInChest(item, quantity))
                return;

            item.Position = CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED;

            var sameItem = GetChestItem(item.Template.Id, item.Position, item.Effects);
            if (sameItem != null)
            {
                _chestItems[item.Template.Id].Remove(sameItem);
                sameItem.Stack += quantity;
                _chestItems[item.Template.Id].Add(sameItem);
            }
            else
            {
                item.Stack = quantity;

                if (_chestItems.ContainsKey(item.Template.Id))
                    _chestItems[item.Template.Id].Add(item);
                else
                    _chestItems.Add(item.Template.Id, new List<BasePlayerItem> { item });
            }

            SaveChestState();
        }

        public void RemoveChestItem(BasePlayerItem item, int quantity = 1)
        {
            var sameItem = GetChestItem(item.Template.Id, item.Position, item.Effects);

            if (sameItem != null)
            {
                _chestItems[item.Template.Id].Remove(sameItem);

                if (sameItem.Stack - quantity <= 0)
                {
                    if (_chestItems[item.Template.Id].Count == 0)
                        _chestItems.Remove(item.Template.Id);
                }
                else
                {
                    sameItem.Stack -= quantity;
                    _chestItems[item.Template.Id].Add(sameItem);
                }
            }
            else
            {
                if (_chestItems.ContainsKey(item.Template.Id))
                {
                    _chestItems[item.Template.Id].Remove(item);

                    if (_chestItems[item.Template.Id].Count == 0)
                        _chestItems.Remove(item.Template.Id);
                }
            }

            SaveChestState();
        }

        public IEnumerable<int> GetSkillListIds()
        {
            // Les skills internes de world_maps_house.SkillsCSV ne doivent pas être exposés
            // comme skills de porte extérieure. Les portes viennent uniquement de worlds_interactives.
            return Enumerable.Empty<int>();
        }

        public HouseInformations GetInformations(WorldClient client = null)
        {
            if (GuildId.HasValue && ShouldDisplayGuildEmblem(client?.Character))
            {
                var guild = GuildManager.Instance.GetGuild(GuildId.Value);
                if (guild != null)
                {
                    return new HouseInformationsExtended(
                        OnSale,
                        SaleLocked,
                        Id,
                        new[] { InteractiveId },
                        OwnerName ?? string.Empty,
                        (short)ModelId,
                        guild.GetGuildInformations());
                }
            }

            return new HouseInformations(OnSale, SaleLocked, Id, new[] { InteractiveId }, OwnerName ?? string.Empty, (short)ModelId);
        }

        public HouseInformationsForGuild GetInformationsForGuild()
        {
            short worldX = 0;
            short worldY = 0;

            if (Map != null && Map.Point != null)
            {
                worldX = (short)Map.Point.X;
                worldY = (short)Map.Point.Y;
            }

            return new HouseInformationsForGuild(
                Id,
                ModelId,
                OwnerName ?? string.Empty,
                worldX,
                worldY,
                GetSkillListIds(),
                GuildShareParams);
        }

        public HouseInformationsInside GetInformationsInside()
        {
            short worldX = 0;
            short worldY = 0;

            if (Map != null && Map.Point != null)
            {
                worldX = (short)Map.Point.X;
                worldY = (short)Map.Point.Y;
            }

            var price = Price > 0 ? Price : Record.DefaultPrice;

            return new HouseInformationsInside(
                Id,
                (short)ModelId,
                OwnerId ?? 0,
                OwnerName,
                worldX,
                worldY,
                (uint)Math.Max(0, price),
                Locked);
        }

        public HouseInformationsForSell GetInformationsForSell()
        {
            short worldX = 0;
            short worldY = 0;
            short subAreaId = 0;

            if (Map != null)
            {
                if (Map.Point != null)
                {
                    worldX = (short)Map.Point.X;
                    worldY = (short)Map.Point.Y;
                }

                subAreaId = (short)Map.SubAreaId;
            }

            return new HouseInformationsForSell(
                ModelId,
                OwnerName ?? string.Empty,
                false,
                worldX,
                worldY,
                subAreaId,
                0,
                (sbyte)(HasChest ? 1 : 0),
                GetSkillListIds(),
                Locked,
                Price > 0 ? Price : Record.DefaultPrice);
        }

        public void ResetOwnership()
        {
            OwnerId = null;
            OwnerName = string.Empty;
            OnSale = false;
            SaleLocked = false;
            Locked = false;
            Code = string.Empty;
            ChestCode = string.Empty;
            GuildId = null;
            GuildShareParams = 0;
            ClearChestState();
        }

        public static string NormalizeDigits(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var digits = new string(value.Where(char.IsDigit).ToArray());

            if (digits.Length > maxLength)
                digits = digits.Substring(0, maxLength);

            return digits;
        }

        public IEnumerable<int> GetVisibleSkills(WorldClient client)
        {
            var result = new HashSet<int>();
            var allSkills = new HashSet<int>(GetSkillListIds() ?? Enumerable.Empty<int>());

            foreach (var defaultSkill in ExteriorSkillIds)
                allSkills.Add(defaultSkill);

            if (HasChest)
            {
                foreach (var chestSkill in ChestSkillIds)
                    allSkills.Add(chestSkill);
            }


            if (client?.Character == null)
                return result;

            bool isOwner = IsOwner(client.Character);
            bool isInside = client.Character.Map != null && ContainsInteriorMap(client.Character.Map.Id);

            foreach (var skillId in allSkills)
            {
                switch (skillId)
                {
                    case 84: // entrer
                        if (!isInside)
                            result.Add(skillId);
                        break;

                    case 97: // acheter
                        if (!isInside && OnSale && !isOwner)
                            result.Add(skillId);
                        break;

                    case 98: // vendre
                        if (isOwner && !OnSale)
                            result.Add(skillId);
                        break;

                    case 108: // modifier prix
                        if (isOwner && OnSale)
                            result.Add(skillId);
                        break;

                    case 81:
                    case 100: // verrouiller / déverrouiller maison
                        if (isOwner && isInside)
                            result.Add(skillId);
                        break;

                    case 104: // ouvrir coffre
                        if (isInside)
                            result.Add(skillId);
                        break;

                    case 105: // verrouiller / configurer coffre
                        if (isInside && isOwner)
                            result.Add(skillId);
                        break;

                    case 106: // déverrouiller coffre (masqué côté client)
                        break;

                    default:
                        result.Add(skillId);
                        break;
                }
            }

            return result;
        }

        public IEnumerable<int> GetVisibleExteriorSkills(WorldClient client)
        {
            return GetVisibleSkills(client)
                .Where(x => ExteriorSkillIds.Contains(x))
                .ToArray();
        }

        public IEnumerable<int> GetVisibleChestSkills(WorldClient client)
        {
            var result = new List<int>();

            if (client?.Character?.Map == null)
                return result;

            // Les coffres de maison sont codés dans worlds_interactives :
            // Type = 85, SkillsCSV = 104,105.
            // Donc on ne doit PAS dépendre de world_maps_house.HasChest
            // ni de ChestTemplateId pour afficher l'interactive coffre.
            if (!ContainsInteriorMap(client.Character.Map.Id))
                return result;

            // Skill 104 = ouvrir coffre.
            // On l'affiche toujours si le coffre existe dans worlds_interactives.
            // SkillHouseChestOpen gère ensuite les droits et le code coffre.
            result.Add(104);

            // Skill 105 = changer/verrouiller le code coffre.
            // Propriétaire uniquement.
            if (IsOwner(client.Character))
                result.Add(105);

            return result;
        }
    }
}