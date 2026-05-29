using Dapper;
using Dapper.Contrib.Extensions;
using MySql.Data.MySqlClient;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.World.Items;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Actors.Characters.Inventory;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Game.Items.Custom;
using Sunshine.Protocol.Utils.Extensions;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Mounts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.Managers
{
    public class ItemManager : Singleton<ItemManager>
    {
        private UniqueIdProvider _idProvider = new UniqueIdProvider();
        private readonly object _idProviderLock = new object();
        private bool _idProviderInitialized;

        public Dictionary<int, ItemRecord> Items = new Dictionary<int, ItemRecord>();
        public Dictionary<int, LivingObjectRecord> LivingObjects = new Dictionary<int, LivingObjectRecord>();

        public void LoadLivingObjects()
        {
            LivingObjects = new Dictionary<int, LivingObjectRecord>();

            try
            {
                var entries = DatabaseManager.Connection.Query<LivingObjectRecord>("SELECT * FROM items_livingobjects");
                foreach (var entry in entries)
                {
                    if (entry == null || entry.Id <= 0 || LivingObjects.ContainsKey(entry.Id))
                        continue;

                    LivingObjects.Add(entry.Id, entry);
                }
            }
            catch
            {
                LivingObjects = new Dictionary<int, LivingObjectRecord>();
            }
        }

        private void EnsureIdProviderInitialized()
        {
            if (_idProviderInitialized)
                return;

            lock (_idProviderLock)
            {
                if (_idProviderInitialized)
                    return;

                var highestId = 0;

                try
                {
                    if (!string.IsNullOrWhiteSpace(DatabaseManager.ConnectionString))
                    {
                        using (var connection = DatabaseManager.CreateConnection())
                        {
                            connection.Open();

                            highestId = Math.Max(highestId, GetMaxItemUid(connection, "characters_items"));
                            highestId = Math.Max(highestId, GetMaxItemUid(connection, "account_bank_items"));
                            highestId = Math.Max(highestId, GetMaxItemUid(connection, "characters_items_merchant"));
                            highestId = Math.Max(highestId, GetMaxItemUid(connection, "house_chest_items"));
                            highestId = Math.Max(highestId, GetMaxItemUid(connection, "world_trashes_items"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logs.Logger.WriteError($"[ItemManager] Failed to initialize item uid provider: {ex}");
                }

                _idProvider = new UniqueIdProvider(highestId);
                _idProviderInitialized = true;
            }
        }

        private static int GetMaxItemUid(MySqlConnection connection, string tableName)
        {
            if (connection == null || string.IsNullOrWhiteSpace(tableName))
                return 0;

            const string existsSql = @"SELECT COUNT(*)
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = @TableName
  AND COLUMN_NAME = 'ItemUid';";

            var exists = connection.ExecuteScalar<int>(existsSql, new { TableName = tableName });
            if (exists <= 0)
                return 0;

            return connection.ExecuteScalar<int>($"SELECT COALESCE(MAX(ItemUid), 0) FROM `{tableName}`");
        }

        private int PopUniqueItemId()
        {
            EnsureIdProviderInitialized();
            return _idProvider.Pop();
        }

        public LivingObjectRecord TryGetLivingObjectRecord(int id)
        {
            return LivingObjects.ContainsKey(id) ? LivingObjects[id] : null;
        }

        public bool TryGetLivingObjectId(BasePlayerItem item, out int livingObjectId)
        {
            livingObjectId = 0;

            if (item == null)
                return false;

            var effect = item.Effects != null
                ? item.Effects.FirstOrDefault(x => x != null && x.Id == EffectsEnum.Effect_LivingObjectId)
                : null;

            if (effect != null && effect.Value > 0)
            {
                livingObjectId = effect.Value;
                return true;
            }

            var raw = item.RawObjectEffects != null
                ? item.RawObjectEffects.OfType<ObjectEffectInteger>().FirstOrDefault(x => x.actionId == (short)EffectsEnum.Effect_LivingObjectId)
                : null;

            if (raw != null && raw.value > 0)
            {
                livingObjectId = raw.value;
                return true;
            }

            return false;
        }

        private BasePlayerItem CopyItemState(BasePlayerItem source, BasePlayerItem target)
        {
            target.Id = source.Id;
            target.Position = source.Position;
            target.Stack = source.Stack;
            target.Price = source.Price;
            target.Selled = source.Selled;
            target.Effects = source.Effects != null ? source.Effects.Select(x => x != null ? x.Clone() : null).Where(x => x != null).ToList() : new List<Effect>();
            target.EffectSets = source.EffectSets;
            target.RawObjectEffects = ObjectEffectSerializer.Clone(source.RawObjectEffects);
            return target;
        }

        public BasePlayerItem FinalizePlayerItem(BasePlayerItem item)
        {
            if (item == null || item.Template == null)
                return item;

            BasePlayerItem current = item;
            int livingObjectId;

            if ((ItemTypeEnum)item.Template.TypeId == ItemTypeEnum.OBJET_VIVANT)
            {
                var livingItem = item as LivingObjectItem;
                if (livingItem == null)
                    livingItem = (LivingObjectItem)CopyItemState(item, new LivingObjectItem(item.Template));

                livingItem.InitializeFromCurrentState();
                current = livingItem;
            }
            else if (TryGetLivingObjectId(item, out livingObjectId) && livingObjectId > 0)
            {
                var boundItem = item as BoundLivingObjectItem;
                if (boundItem == null)
                    boundItem = (BoundLivingObjectItem)CopyItemState(item, new BoundLivingObjectItem(item.Template));

                boundItem.InitializeFromCurrentState();
                current = boundItem;
            }
            else if (item is BoundLivingObjectItem)
            {
                var typedItem = CreateTypedPlayerItem(item.Template.Id) ?? new BasePlayerItem(item.Template);
                current = CopyItemState(item, typedItem);
            }

            current.EnsureRuntimeEffects();
            return current;
        }

        public IEnumerable<ItemRecord> GetAllItemsTemplate()
        {
            var items = DatabaseManager.Connection.Query<ItemTemplate>("SELECT * FROM items");
            var weapons = DatabaseManager.Connection.Query<WeaponTemplate>("SELECT * FROM items_weapons");
            return new List<ItemRecord>().Concat(items).Concat(weapons);
        }

        public IEnumerable<ItemSetTemplate> GetAllItemSetsTemplate()
        {
            return DatabaseManager.Connection.Query<ItemSetTemplate>("SELECT * FROM items_sets");
        }

        public ItemSetTemplate GetItemSetTemplate(int itemSet)
        {
            return DatabaseManager.Connection.QueryFirstOrDefault<ItemSetTemplate>($"SELECT * FROM items_sets WHERE Id = '{itemSet}'");
        }

        public BasePlayerItem CreatePlayerItem(int itemId, int quantity = 1, bool isJetMax = false)
        {
            if (!Items.ContainsKey(itemId))
                return null;

            if (MountManager.Instance.IsMountCertificateTemplate(itemId))
            {
                var certificate = MountCertificateFactory.CreateGeneratedCertificate(itemId);
                if (certificate != null)
                    return certificate;
            }

            var item = CreateTypedPlayerItem(itemId);
            if (item == null)
                return null;

            item.Id = PopUniqueItemId();
            item.Position = CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED;
            item.Stack = quantity;
            item.Effects = Items[itemId].EffectsBase.Clone();
            item.EffectSets = Items[itemId].EffectSets;
            item.RawObjectEffects = new List<ObjectEffect>();

            if (item.Effects != null)
                item.Effects.ForEach(x => x.GenerateEffect(isJetMax));

            item = FinalizePlayerItem(item);
            return item;
        }

        public BasePlayerItem CreatePlayerItem(int itemId, List<Effect> effects, int quantity = 1)
        {
            return CreatePlayerItem(itemId, effects, null, quantity);
        }

        public BasePlayerItem CreatePlayerItem(int itemId, List<Effect> effects, IEnumerable<ObjectEffect> rawEffects, int quantity = 1)
        {
            if (!Items.ContainsKey(itemId))
                return null;

            var item = CreateTypedPlayerItem(itemId);
            if (item == null)
                return null;

            item.Id = PopUniqueItemId();
            item.Position = CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED;
            item.Stack = quantity;
            item.Effects = effects?.Clone() ?? new List<Effect>();
            item.EffectSets = Items[itemId].EffectSets;
            item.RawObjectEffects = ObjectEffectSerializer.Clone(rawEffects);

            item = FinalizePlayerItem(item);
            return item;
        }

        public BasePlayerItem CreatePlayerItem(BasePlayerItem source, int quantity = 1)
        {
            if (source == null)
                return null;

            return CreatePlayerItem(source.Template.Id, source.Effects, source.RawObjectEffects, quantity);
        }


        public BasePlayerItem CreateTypedPlayerItem(int itemId)
        {
            if (!Items.ContainsKey(itemId))
                return null;

            var template = Items[itemId];
            if ((ItemTypeEnum)template.TypeId == ItemTypeEnum.CERTIFICAT_DE_DRAGODINDE)
                return new MountCertificate(template);

            if ((ItemTypeEnum)template.TypeId == ItemTypeEnum.OBJET_VIVANT)
                return new LivingObjectItem(template);

            if (PrismManager.Instance.IsPrismItem(template.Id))
                return new PrismItem(template);

            return new BasePlayerItem(template);
        }

        public int GenerateId()
        {
            return PopUniqueItemId();
        }
    }
}
