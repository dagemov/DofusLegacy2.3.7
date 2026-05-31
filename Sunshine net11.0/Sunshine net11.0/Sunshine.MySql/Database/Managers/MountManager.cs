using Dapper;
using Dapper.Contrib.Extensions;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.World.Mounts;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;
using Sunshine.Protocol.Utils;
using Sunshine.Protocol.Utils.Extensions;
using Sunshine.WorldServer.Game.Effects;
using Sunshine.WorldServer.Game.Mounts;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.MySql.Database.Managers
{
    public class MountManager : Singleton<MountManager>
    {
        private readonly object _sync = new object();
        private bool _loaded;
        private UniqueIdProvider _idProvider = new UniqueIdProvider();

        public Dictionary<int, MountTemplateRecord> Templates { get; private set; }
        public Dictionary<int, List<MountBonusRecord>> Bonuses { get; private set; }
        public Dictionary<int, Mount> Mounts { get; private set; }

        public MountManager()
        {
            Templates = new Dictionary<int, MountTemplateRecord>();
            Bonuses = new Dictionary<int, List<MountBonusRecord>>();
            Mounts = new Dictionary<int, Mount>();
        }

        private void EnsureLoaded()
        {
            if (_loaded)
                return;

            lock (_sync)
            {
                if (_loaded)
                    return;

                Templates = DatabaseManager.Connection
                    .Query<MountTemplateRecord>("SELECT * FROM mounts_templates")
                    .ToDictionary(x => x.Id, x => x);

                Bonuses = DatabaseManager.Connection
                    .Query<MountBonusRecord>("SELECT * FROM mounts_bonus")
                    .GroupBy(x => x.MountTemplateId)
                    .ToDictionary(x => x.Key, x => x.ToList());

                var records = DatabaseManager.Connection.Query<MountRecord>("SELECT * FROM mounts").ToList();
                var items = DatabaseManager.Connection.Query<MountItemRecord>("SELECT * FROM mounts_items").ToList();
                _idProvider = new UniqueIdProvider(records.Count > 0 ? records.Max(x => x.Id) : 0);

                Mounts = records.ToDictionary(
                    x => x.Id,
                    x => BuildMount(x, items.Where(i => i.MountId == x.Id)));

                _loaded = true;
            }
        }

        private Mount BuildMount(MountRecord record, IEnumerable<MountItemRecord> items)
        {
            MountTemplateRecord template;
            if (!Templates.TryGetValue(record.TemplateId, out template))
                throw new KeyNotFoundException("Mount template not found: " + record.TemplateId);

            List<MountBonusRecord> templateBonuses;
            if (!Bonuses.TryGetValue(record.TemplateId, out templateBonuses))
                templateBonuses = new List<MountBonusRecord>();

            return new Mount(record, template, templateBonuses, BuildItems(items));
        }

        private IEnumerable<ObjectItem> BuildItems(IEnumerable<MountItemRecord> records)
        {
            foreach (var record in records ?? Enumerable.Empty<MountItemRecord>())
            {
                var rawEffects = DeserializeRawEffects(record.SerializedEffects);
                var templateEffects = ItemManager.Instance.Items.ContainsKey(record.ItemId)
                    ? (ItemManager.Instance.Items[record.ItemId].EffectsBase ?? new List<Effect>())
                    : new List<Effect>();
                var objectEffects = rawEffects.Any()
                    ? rawEffects
                    : ((EffectManager.Instance.GetEffects(ToHex(record.SerializedEffects)) ?? templateEffects) ?? new List<Effect>())
                        .Select(x => (ObjectEffect)x.GetObjectEffectInteger())
                        .ToList();

                yield return new ObjectItem(
                    position: 63,
                    objectGID: (short)record.ItemId,
                    powerRate: 0,
                    overMax: false,
                    effects: objectEffects,
                    objectUID: record.Id,
                    quantity: (int)record.Stack);
            }
        }

        private BasePlayerItem BuildPlayerItem(MountItemRecord record)
        {
            if (record == null)
                return null;

            var item = ItemManager.Instance.CreateTypedPlayerItem(record.ItemId);
            if (item == null)
                return null;

            item.Id = record.Id;
            item.Position = CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED;
            item.Stack = (int)record.Stack;
            item.EffectSets = ItemManager.Instance.Items.ContainsKey(record.ItemId)
                ? ItemManager.Instance.Items[record.ItemId].EffectSets
                : null;
            item.RawObjectEffects = DeserializeRawEffects(record.SerializedEffects);
            item.Effects = item.RawObjectEffects.Count > 0
                ? new List<Effect>()
                : (ItemManager.Instance.Items.ContainsKey(record.ItemId)
                    ? (ItemManager.Instance.Items[record.ItemId].EffectsBase?.Clone() ?? new List<Effect>())
                    : new List<Effect>());
            return item;
        }

        public IEnumerable<Mount> GetOwnerMounts(int ownerId)
        {
            return GetOwnerMounts(ownerId, null);
        }

        public IEnumerable<Mount> GetOwnerMounts(int characterOwnerId, int? accountOwnerId)
        {
            EnsureLoaded();

            return Mounts.Values
                .Where(x => x != null && x.Record != null)
                .Where(x => x.Record.OwnerId == characterOwnerId || (accountOwnerId.HasValue && x.Record.OwnerId == accountOwnerId.Value))
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToList();
        }

        private Mount NormalizeListedMount(Mount mount)
        {
            if (mount == null || mount.Record == null)
                return mount;

            bool changed = false;

            if (mount.Record.IsInStable > 0 && mount.Record.PaddockId.HasValue)
            {
                mount.Record.PaddockId = null;
                changed = true;
            }

            var shouldBeManagedMount = mount.Record.IsInStable > 0 || mount.Record.PaddockId.HasValue;
            if (shouldBeManagedMount && mount.Record.StoredSince.HasValue)
            {
                mount.Record.StoredSince = null;
                changed = true;
            }

            if (changed)
                Save(mount);

            return mount;
        }

        public IEnumerable<Mount> GetStableMounts(int ownerId)
        {
            return GetStableMounts(ownerId, null);
        }

        public IEnumerable<Mount> GetStableMounts(int characterOwnerId, int? accountOwnerId)
        {
            return GetOwnerMounts(characterOwnerId, accountOwnerId)
                .Select(NormalizeListedMount)
                .Where(x => x.IsInStable)
                .ToList();
        }

        public IEnumerable<Mount> GetPaddockedMounts(int ownerId, int? paddockId = null)
        {
            return GetPaddockedMounts(ownerId, null, paddockId);
        }

        public IEnumerable<Mount> GetPaddockedMounts(int characterOwnerId, int? accountOwnerId, int? paddockId = null)
        {
            var mounts = GetOwnerMounts(characterOwnerId, accountOwnerId)
                .Select(NormalizeListedMount)
                .Where(x => !x.IsInStable && !x.Record.StoredSince.HasValue);

            if (paddockId.HasValue)
                mounts = mounts.Where(x => x.Record.PaddockId == paddockId.Value);

            return mounts.ToList();
        }

        public IEnumerable<Mount> GetPaddockedMountsByMap(int paddockId)
        {
            EnsureLoaded();

            return Mounts.Values
                .Where(x => x != null && x.Record != null)
                .Select(NormalizeListedMount)
                .Where(x => !x.IsInStable && !x.Record.StoredSince.HasValue && x.Record.PaddockId == paddockId)
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToList();
        }

        public MountTemplateRecord GetTemplate(int id)
        {
            EnsureLoaded();

            MountTemplateRecord template;
            Templates.TryGetValue(id, out template);
            return template;
        }

        public MountTemplateRecord GetTemplateByScrollId(int scrollId)
        {
            EnsureLoaded();
            return Templates.Values.FirstOrDefault(x => x.ScrollId == scrollId);
        }

        public bool IsMountCertificateTemplate(int itemTemplateId)
        {
            EnsureLoaded();
            return Templates.Values.Any(x => x.ScrollId == itemTemplateId);
        }

        public Mount GetMount(int id)
        {
            EnsureLoaded();

            Mount mount;
            Mounts.TryGetValue(id, out mount);
            return mount;
        }

        public IEnumerable<ObjectItem> GetMountInventory(int mountId)
        {
            var mount = GetMount(mountId);
            return mount != null ? mount.Items : Enumerable.Empty<ObjectItem>();
        }

        public IEnumerable<BasePlayerItem> GetMountInventoryPlayerItems(int mountId)
        {
            EnsureLoaded();
            return DatabaseManager.Connection.Query<MountItemRecord>("SELECT * FROM mounts_items WHERE MountId=@MountId ORDER BY ItemId, Id", new { MountId = mountId })
                .Select(BuildPlayerItem)
                .Where(x => x != null)
                .ToList();
        }

        public int GetMountInventoryWeight(int mountId)
        {
            return GetMountInventoryPlayerItems(mountId).Sum(x => x.Template.Weight * x.Stack);
        }

        public bool CanStoreInMount(int mountId, BasePlayerItem item, int quantity)
        {
            var mount = GetMount(mountId);
            if (mount == null || item == null || quantity <= 0)
                return false;

            return GetMountInventoryWeight(mountId) + (item.Template.Weight * quantity) <= mount.MaxPods;
        }

        public void AddMountInventoryItem(int mountId, BasePlayerItem item, int quantity)
        {
            if (item == null || quantity <= 0)
                return;

            EnsureLoaded();

            var records = DatabaseManager.Connection.Query<MountItemRecord>("SELECT * FROM mounts_items WHERE MountId=@MountId", new { MountId = mountId }).ToList();
            var effectsHex = SerializeComparableEffects(item);
            var same = records.FirstOrDefault(x => x.ItemId == item.Template.Id && ToHex(x.SerializedEffects) == effectsHex);

            if (same != null)
            {
                same.Stack += (uint)quantity;
                DatabaseManager.Connection.Update(same);
            }
            else
            {
                var record = new MountItemRecord
                {
                    MountId = mountId,
                    Id = ItemManager.Instance.GenerateId(),
                    ItemId = item.Template.Id,
                    Stack = (uint)quantity,
                    SerializedEffects = HexToBytes(effectsHex)
                };

                DatabaseManager.Connection.Insert(record);
            }

            RefreshMountItems(mountId);
        }

        public void RemoveMountInventoryItem(int mountId, BasePlayerItem item, int quantity)
        {
            if (item == null || quantity <= 0)
                return;

            EnsureLoaded();

            var record = DatabaseManager.Connection.Query<MountItemRecord>("SELECT * FROM mounts_items WHERE MountId=@MountId AND Id=@Id LIMIT 1", new { MountId = mountId, Id = item.Id }).FirstOrDefault();
            if (record == null)
                return;

            if (record.Stack <= quantity)
                DatabaseManager.Connection.Execute("DELETE FROM mounts_items WHERE MountId=@MountId AND Id=@Id", new { MountId = mountId, Id = item.Id });
            else
            {
                record.Stack -= (uint)quantity;
                DatabaseManager.Connection.Update(record);
            }

            RefreshMountItems(mountId);
        }

        private void RefreshMountItems(int mountId)
        {
            var mount = GetMount(mountId);
            if (mount == null)
                return;

            mount.Items.Clear();
            mount.Items.AddRange(BuildItems(DatabaseManager.Connection.Query<MountItemRecord>("SELECT * FROM mounts_items WHERE MountId=@MountId ORDER BY ItemId, Id", new { MountId = mountId })));
        }

        private static List<ObjectEffect> DeserializeRawEffects(byte[] data)
        {
            if (data == null || data.Length == 0)
                return new List<ObjectEffect>();

            return ObjectEffectSerializer.Deserialize(ToHex(data));
        }

        private static string SerializeComparableEffects(BasePlayerItem item)
        {
            if (item == null)
                return string.Empty;

            if (item.RawObjectEffects != null && item.RawObjectEffects.Count > 0)
                return ObjectEffectSerializer.Serialize(item.RawObjectEffects);

            return ObjectEffectSerializer.Serialize((item.Effects ?? new List<Effect>()).Select(x => (ObjectEffect)x.GetObjectEffectInteger()));
        }

        private static string ToHex(byte[] data)
        {
            return data == null || data.Length == 0 ? string.Empty : BitConverter.ToString(data).Replace("-", string.Empty);
        }

        private static byte[] HexToBytes(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return new byte[0];

            return Enumerable.Range(0, hex.Length / 2)
                .Select(x => Convert.ToByte(hex.Substring(x * 2, 2), 16))
                .ToArray();
        }

        public int GenerateId()
        {
            EnsureLoaded();
            return _idProvider.Pop();
        }

        public void Save(Mount mount)
        {
            if (mount == null)
                return;

            EnsureLoaded();

            if (GetMount(mount.Id) == null)
                DatabaseManager.Connection.Insert(mount.Record);
            else
                DatabaseManager.Connection.Update(mount.Record);

            Mounts[mount.Id] = mount;
        }
    }
}