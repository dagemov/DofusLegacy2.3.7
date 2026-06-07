using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.World;
using Sunshine.Protocol.Types;
using System;
using System.Collections.Generic;
using Dapper;
using Dapper.Contrib.Extensions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sunshine.MySql.Database.Auth;
using Sunshine.Protocol.Utils;
using Sunshine.MySql.Database.World.Characters;
using Sunshine.MySql.Database.Auth.Worlds;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Actors.Characters;
using Sunshine.MySql.Database.World.Breeds;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Game.Actors.Characters.Spells;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.MySql.Database.World.Characters.Shortcuts;
using Sunshine.WorldServer.Game.Actors.Characters.Shortcuts;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors.Stats;
using Sunshine.Logs;
using Sunshine.MySql.Database.World.Characters.Items;
using Sunshine.WorldServer.Game.Effects;
using Sunshine.WorldServer.Game.Actors.Characters.Inventory;
using Sunshine.MySql.Database.World.Items;
using Sunshine.Protocol.IO;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Game.Mounts;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Actors.Characters.Jobs;
using Sunshine.WorldServer.Game.Actors.Characters.Quests;
using Sunshine.MySql.Database.World.Characters.Quests;
using MySql.Data.MySqlClient;

namespace Sunshine.MySql.Database.Managers
{
    public class CharacterManager : Singleton<CharacterManager>
    {
        public Dictionary<int, Character> Characters = new Dictionary<int, Character>();

        public readonly Dictionary<StatsBoostTypeEnum, StatsEnum> StatsRelations = new Dictionary<StatsBoostTypeEnum, StatsEnum>
        {
                {StatsBoostTypeEnum.Strength, StatsEnum.Strength},
                {StatsBoostTypeEnum.Agility, StatsEnum.Agility},
                {StatsBoostTypeEnum.Chance, StatsEnum.Chance},
                {StatsBoostTypeEnum.Wisdom, StatsEnum.Wisdom},
                {StatsBoostTypeEnum.Intelligence, StatsEnum.Intelligence},
                {StatsBoostTypeEnum.Vitality, StatsEnum.Vitality},
        };

        public void CreateCharacter(int accountId, CharacterRecord character)
        {
            CharacterPaddockBootstrap.EnsurePaddockIdColumn();
            var zaapIds = DatabaseManager.Connection.Query<int>("SELECT DISTINCT Map FROM worlds_interactives WHERE Type = 16 ORDER BY Map").ToList();
            character.Zaaps = string.Join(",", zaapIds);

            DatabaseManager.Connection.Insert(character);
            AccountManager accountManager = Singleton<AccountManager>.Instance;
            BreedManager breedManager = Singleton<BreedManager>.Instance;
            CreateCharacterStats(new CharacterStatsRecord { OwnerId = character.Id, AP = 6, MP = 3, Health = 55 });
            CreateCharacterAlign(new CharacterAlignmentRecord { OwnerId = character.Id, Grade = 1 });
            CreateCharacterSpells(character.Id, breedManager.GetSpells(character.Breed));
            CreateCharacterShortcutSpells(breedManager.GetSpellShortcuts(character.Id, character.Breed));
            accountManager.InsertCharacter(new WorldCharacter { Account = accountId, Owner = character.Id, World = 18 });
            Logger.WriteInfo($"Character {character.Name} was created...");
        }

        private void CreateCharacterJobs(int characterId)
        {
            // Les métiers ne doivent plus être auto-appris à la création du personnage.
        }

        private void CreateCharacterStats(CharacterStatsRecord characterStats)
        {
            DatabaseManager.Connection.Insert(characterStats);
        }

        public void CreateCharacterAlign(CharacterAlignmentRecord characteralign)
        {
            DatabaseManager.Connection.Insert(characteralign);
        }

        public void CreateCharacterSpells(int characterId, IEnumerable<BreedSpell> breedSpell)
        {
            string cmd = $"INSERT INTO characters_spells (OwnerId, Spell, Level) VALUES({characterId}, @Spell, 1)";
            DatabaseManager.Connection.Execute(cmd, breedSpell);
        }

        public void CreateCharacterShortcutSpells(IEnumerable<SpellShortcut> spellShortcut)
        {
            string cmd = "INSERT INTO characters_shortcuts_spells (OwnerId, Spell, Slot) VALUES(@OwnerId, @Spell, @Slot)";
            DatabaseManager.Connection.Execute(cmd, spellShortcut);
        }

        public IEnumerable<CharacterRecord> GetAllCharacters()
        {
            CharacterPaddockBootstrap.EnsurePaddockIdColumn();
            return DatabaseManager.Connection.Query<CharacterRecord>("SELECT * FROM characters");
        }

        public CharacterStatsRecord GetCharacterStats(int characterId)
        {
            CharacterPaddockBootstrap.EnsurePaddockIdColumn();
            var stats = DatabaseManager.Connection.QueryFirstOrDefault<CharacterStatsRecord>($"SELECT * FROM characters_stats WHERE OwnerId = '{characterId}'");

            if (stats != null)
                return stats;

            var characterRecord = DatabaseManager.Connection.QueryFirstOrDefault<CharacterRecord>($"SELECT * FROM characters WHERE Id = '{characterId}'");
            if (characterRecord == null)
                return null;

            stats = BuildDefaultCharacterStats(characterRecord);
            CreateCharacterStats(stats);
            Logger.Write($"[ World ] Missing characters_stats row for character {characterRecord.Id} ({characterRecord.Name}), a default record has been created.");
            return stats;
        }

        private CharacterStatsRecord BuildDefaultCharacterStats(CharacterRecord character)
        {
            short level = character?.Level > 0 ? character.Level : (short)1;
            int baseHealth = Math.Max(55, 50 + (level * 5));

            return new CharacterStatsRecord
            {
                OwnerId = character?.Id ?? 0,
                AP = 6,
                MP = 3,
                Health = baseHealth,
                Vitality = 0,
                Strength = 0,
                Chance = 0,
                Wisdom = 0,
                Intelligence = 0,
                Agility = 0,
                DamageTaken = 0,
            };
        }

        public CharacterAlignmentRecord GetCharacterAlign(int characterId)
        {
            return DatabaseManager.Connection.QueryFirstOrDefault<CharacterAlignmentRecord>($"SELECT * FROM characters_alignments WHERE OwnerId = '{characterId}'");
        }

        public List<CharacterSpellRecord> GetCharacterSpells(int characterId)
        {
            return DatabaseManager.Connection.Query<CharacterSpellRecord>($"SELECT * FROM characters_spells WHERE OwnerId = '{characterId}'").ToList();
        }

        public List<T> GetShortcuts<T>(int characterId) where T : class
        {
            if (typeof(T).Name == "SpellShortcut")
                return DatabaseManager.Connection.Query<T>($"SELECT * FROM characters_shortcuts_spells WHERE OwnerId = '{characterId}'").ToList();

            if (typeof(T).Name == "PresetShortcut")
                return DatabaseManager.Connection.Query<T>($"SELECT * FROM characters_shortcuts_items_presets WHERE OwnerId = '{characterId}'").ToList();

            return DatabaseManager.Connection.Query<T>($"SELECT * FROM characters_shortcuts_items WHERE OwnerId = '{characterId}'").ToList();
        }

        public List<CharacterPresetRecord> GetCharacterPresets(int characterId)
        {
            var presets = DatabaseManager.Connection.Query<CharacterPresetRecord>($"SELECT * FROM characters_items_presets WHERE OwnerId = '{characterId}' ORDER BY PresetId").ToList();

            foreach (var preset in presets)
                preset.EnsureDeserialized();

            return presets;
        }

        public Dictionary<int, List<BasePlayerItem>> GetCharacterItems(int characterId)
        {
            var chrItems = DatabaseManager.Connection.Query<CharacterItemRecord>(
                $"SELECT * FROM characters_items WHERE OwnerId = '{characterId}'");

            Dictionary<int, List<BasePlayerItem>> items = new Dictionary<int, List<BasePlayerItem>>();

            foreach (var chr in chrItems)
            {
                BasePlayerItem playerItem = ItemManager.Instance.CreateTypedPlayerItem(chr.Item);

                if (playerItem == null)
                    continue;

                playerItem.Id = chr.ItemUid > 0 ? chr.ItemUid : ItemManager.Instance.GenerateId();
                playerItem.Position = (CharacterInventoryPositionEnum)chr.Position;
                playerItem.Stack = chr.Stack;
                playerItem.Effects = new List<Effect>();
                playerItem.RawObjectEffects = new List<ObjectEffect>();

                if (ItemManager.Instance.Items.ContainsKey(chr.Item))
                    playerItem.EffectSets = ItemManager.Instance.Items[chr.Item].EffectSets;

                // 1) on tente le nouveau format (ObjectEffect protocolaires)
                var rawEffects = ObjectEffectSerializer.Deserialize(chr.Effects);

                if (rawEffects != null && rawEffects.Count > 0)
                {
                    playerItem.RawObjectEffects = rawEffects;

                    // si ce sont des effets entiers classiques, on reconstruit aussi Effects
                    foreach (var raw in rawEffects.OfType<ObjectEffectInteger>())
                    {
                        playerItem.Effects.Add(new Effect(
                            (Protocol.Enums.EffectsEnum)raw.actionId,
                            0,
                            0,
                            raw.value,
                            0,
                            0,
                            0,
                            Protocol.Enums.SpellShapeEnum.P,
                            0,
                            0));
                    }
                }
                else
                {
                    // 2) fallback ancien format Sunshine
                    playerItem.Effects = EffectManager.Instance.GetEffects(chr.Effects) ?? new List<Effect>();
                }

                MountCertificateFactory.TryNormalizeImportedCertificate(playerItem, characterId);

                if (MountManager.Instance.IsMountCertificateTemplate(playerItem.Template.Id) &&
                    !MountCertificateFactory.IsActiveCertificateItem(playerItem, characterId, false))
                    continue;

                playerItem = ItemManager.Instance.FinalizePlayerItem(playerItem);

                if (items.ContainsKey(chr.Item))
                    items[chr.Item].Add(playerItem);
                else
                    items.Add(chr.Item, new List<BasePlayerItem> { playerItem });
            }

            return items;
        }

        public List<CharacterJobRecord> GetCharacterJobs(int characterId)
        {
            return DatabaseManager.Connection.Query<CharacterJobRecord>($"SELECT * FROM characters_jobs WHERE OwnerId = '{characterId}'").ToList();
        }

        public List<CharacterQuestRecord> GetCharacterQuests(int characterId)
        {
            return DatabaseManager.Connection.Query<CharacterQuestRecord>($"SELECT * FROM characters_quests WHERE OwnerId = '{characterId}'").ToList();
        }

        public Dictionary<short, List<CharacterQuestStepRecord>> GetCharacterQuestSteps(int characterId)
        {
            var stepsChr = DatabaseManager.Connection.Query<CharacterQuestStepRecord>($"SELECT * FROM characters_quests_steps WHERE OwnerId = '{characterId}'").ToList();
            Dictionary<short, List<CharacterQuestStepRecord>> steps = new Dictionary<short, List<CharacterQuestStepRecord>>();
            foreach (var step in stepsChr)
            {
                if (steps.ContainsKey(step.Quest))
                    steps[step.Quest].Add(step);
                else
                    steps.Add(step.Quest, new List<CharacterQuestStepRecord> { step });
            }
            return steps;
        }

        public Dictionary<short, List<CharacterQuestObjectiveRecord>> GetCharacterQuestObjectives(int characterId)
        {
            var objsChr = DatabaseManager.Connection.Query<CharacterQuestObjectiveRecord>($"SELECT * FROM characters_quests_objectives WHERE OwnerId = '{characterId}'").ToList();
            Dictionary<short, List<CharacterQuestObjectiveRecord>> objs = new Dictionary<short, List<CharacterQuestObjectiveRecord>>();
            foreach (var obj in objsChr)
            {
                if (objs.ContainsKey(obj.Step))
                    objs[obj.Step].Add(obj);
                else
                    objs.Add(obj.Step, new List<CharacterQuestObjectiveRecord> { obj });
            }
            return objs;
        }

        public List<CharacterQuestObjectiveRecord> GetCharacterQuestObjectivesByChrId(int characterId)
        {
            return DatabaseManager.Connection.Query<CharacterQuestObjectiveRecord>($"SELECT * FROM characters_quests_objectives WHERE OwnerId = '{characterId}'").ToList();

        }


        public void SaveSelectionRemodel(Character character)
        {
            if (character == null || character.Record == null)
                return;

            CharacterPaddockBootstrap.EnsurePaddockIdColumn();

            try
            {
                DatabaseManager.Connection.Execute(@"
                    UPDATE characters
                    SET Name = @Name,
                        Breed = @Breed,
                        Sex = @Sex,
                        EntityLook = @EntityLook,
                        CustomLook = @CustomLook,
                        CanUseRename = @CanUseRename,
                        CanUseRecolor = @CanUseRecolor,
                        CanUseRelook = @CanUseRelook
                    WHERE Id = @Id",
                    new
                    {
                        character.Record.Id,
                        character.Record.Name,
                        character.Record.Breed,
                        character.Record.Sex,
                        character.Record.EntityLook,
                        character.Record.CustomLook,
                        character.Record.CanUseRename,
                        character.Record.CanUseRecolor,
                        character.Record.CanUseRelook
                    });
            }
            catch (Exception e)
            {
                Logger.WriteError(e.ToString());
            }
        }

        public void Save(Character character)
        {
            CharacterPaddockBootstrap.EnsurePaddockIdColumn();
            MySqlTransaction transaction = null;

            try
            {
                RefreshAccountTokensBeforeSave(character);

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < character.Zaaps.Count; i++)
                    builder.Append(i == character.Zaaps.Count - 1 ? character.Zaaps[i].Id.ToString()
                                                                  : character.Zaaps[i].Id + ",");
                character.Record.Zaaps = builder.ToString();

                using (var connection = DatabaseManager.CreateConnection())
                {
                    connection.Open();
                    transaction = connection.BeginTransaction();

                    connection.Update(character.Record, transaction);
                    SaveStats(connection, transaction, character.Id, character.Stats);
                    SaveAlignment(connection, transaction, character.Id, character.Alignment);
                    SaveSpells(connection, transaction, character.Id, character.Spells);
                    SaveShortcuts(connection, transaction, character.Id, character.Shortcuts);
                    SaveItems(connection, transaction, character.Id, character.Inventory);
                    SavePresets(connection, transaction, character.Id, character.Inventory);
                    SaveJobs(connection, transaction, character.Id, character.Jobs);
                    SaveQuests(connection, transaction, character.Id, character.Quests);

                    transaction.Commit();
                }

                if (character.Account != null)
                {
                    AccountManager.Instance.UpdateAccountTokens(character.Account);

                    if (character.Client != null && character.Client.Account != null)
                    {
                        character.Client.Account.Tokens = character.Account.Tokens;
                        character.Client.Account.NewTokens = character.Account.NewTokens;
                    }
                }
            }
            catch (Exception e)
            {
                try
                {
                    transaction?.Rollback();
                }
                catch
                {
                }

                Logger.WriteError(e.ToString());
            }
        }

        private void RefreshAccountTokensBeforeSave(Character character)
        {
            if (character?.Account == null)
                return;

            if (character.Inventory != null)
            {
                character.Inventory.MergePendingTokens(false);
                return;
            }

            var latestAccount = AccountManager.Instance.GetAccountById(character.Account.Id);
            if (latestAccount == null)
                return;

            character.Account.Tokens = Math.Max(0, latestAccount.Tokens);
            character.Account.NewTokens = Math.Max(0, latestAccount.NewTokens);

            if (character.Client?.Account != null)
            {
                character.Client.Account.Tokens = character.Account.Tokens;
                character.Client.Account.NewTokens = character.Account.NewTokens;
            }
        }

        private void SaveStats(MySqlConnection connection, MySqlTransaction transaction, int characterId, StatsFields characterStats)
        {
            connection.Execute("DELETE FROM characters_stats WHERE OwnerId = @OwnerId", new { OwnerId = characterId }, transaction);
            connection.Insert(characterStats.Record, transaction);
        }

        private void SaveAlignment(MySqlConnection connection, MySqlTransaction transaction, int characterId, CharacterAlignment characterAlign)
        {
            connection.Execute("DELETE FROM characters_alignments WHERE OwnerId = @OwnerId", new { OwnerId = characterId }, transaction);
            connection.Insert(characterAlign.Record, transaction);
        }

        private void SaveSpells(MySqlConnection connection, MySqlTransaction transaction, int characterId, SpellInventory characterSpells)
        {
            connection.Execute("DELETE FROM characters_spells WHERE OwnerId = @OwnerId", new { OwnerId = characterId }, transaction);
            connection.Insert(characterSpells.GetSpells(), transaction);
        }

        private void SaveShortcuts(MySqlConnection connection, MySqlTransaction transaction, int characterId, ShortcutBar shortcutBar)
        {
            connection.Execute("DELETE FROM characters_shortcuts_spells WHERE OwnerId = @OwnerId", new { OwnerId = characterId }, transaction);
            if (shortcutBar.SpellShortcuts.Count > 0)
                connection.Insert(shortcutBar.SpellShortcuts, transaction);

            connection.Execute("DELETE FROM characters_shortcuts_items WHERE OwnerId = @OwnerId", new { OwnerId = characterId }, transaction);
            if (shortcutBar.ItemShortcuts.Count > 0)
                connection.Insert(shortcutBar.ItemShortcuts, transaction);

            connection.Execute("DELETE FROM characters_shortcuts_items_presets WHERE OwnerId = @OwnerId", new { OwnerId = characterId }, transaction);
            if (shortcutBar.PresetShortcuts.Count > 0)
                connection.Insert(shortcutBar.PresetShortcuts, transaction);
        }

        private void SaveItems(MySqlConnection connection, MySqlTransaction transaction, int characterId, Inventory inventory)
        {
            connection.Execute("DELETE FROM characters_items WHERE OwnerId = @OwnerId", new { OwnerId = characterId }, transaction);

            var itemsRecord = inventory.GetItems()
                .Where(x => x != null && x.Template != null && x.Template.Id != Inventory.DefaultTokenTemplateId)
                .Where(x => !MountManager.Instance.IsMountCertificateTemplate(x.Template.Id) ||
                            MountCertificateFactory.IsActiveCertificateItem(x, characterId))
                .ToList();

            foreach (var item in itemsRecord)
            {
                string serializedEffects;

                if (item.RawObjectEffects != null && item.RawObjectEffects.Count > 0)
                {
                    serializedEffects = ObjectEffectSerializer.Serialize(item.RawObjectEffects);
                }
                else
                {
                    serializedEffects = EffectManager.Instance.SetEffects(new BigEndianWriter(), item.Effects);
                }

                var chrItem = new CharacterItemRecord
                {
                    OwnerId = characterId,
                    ItemUid = item.Id,
                    Item = item.Template.Id,
                    Position = (byte)item.Position,
                    Stack = item.Stack,
                    Effects = serializedEffects
                };

                connection.Insert(chrItem, transaction);
            }
        }

        private void SavePresets(MySqlConnection connection, MySqlTransaction transaction, int characterId, Inventory inventory)
        {
            connection.Execute("DELETE FROM characters_items_presets WHERE OwnerId = @OwnerId", new { OwnerId = characterId }, transaction);

            foreach (var preset in inventory.GetPresetRecords())
            {
                preset.OwnerId = characterId;
                preset.SetObjects(preset.Objects);
                connection.Insert(preset, transaction);
            }
        }

        private void SaveJobs(MySqlConnection connection, MySqlTransaction transaction, int characterId, JobsCollection jobs)
        {
            connection.Execute("DELETE FROM characters_jobs WHERE OwnerId = @OwnerId", new { OwnerId = characterId }, transaction);
            foreach (var job in jobs.GetJobs())
                connection.Insert(job, transaction);
        }

        private void SaveQuests(MySqlConnection connection, MySqlTransaction transaction, int characterId, QuestsCollection quests)
        {
            connection.Execute("DELETE FROM characters_quests WHERE OwnerId = @OwnerId", new { OwnerId = characterId }, transaction);
            connection.Execute("DELETE FROM characters_quests_steps WHERE OwnerId = @OwnerId", new { OwnerId = characterId }, transaction);
            connection.Execute("DELETE FROM characters_quests_objectives WHERE OwnerId = @OwnerId", new { OwnerId = characterId }, transaction);

            var questRecords = quests.GetQuests()
                .GroupBy(x => new { x.OwnerId, x.Quest })
                .Select(g => g.First())
                .ToList();

            foreach (var quest in questRecords)
            {
                var steps = quests.GetQuestsSteps(quest.Quest)
                    .GroupBy(x => new { x.OwnerId, x.Quest, x.Step })
                    .Select(g => g.First())
                    .ToList();

                foreach (var step in steps)
                    connection.Insert(step, transaction);

                connection.Insert(quest, transaction);
            }

            var objectiveRecords = quests.GetQuestsObjectives()
                .GroupBy(x => new { x.OwnerId, x.Step, x.Objective })
                .Select(g => g.First())
                .ToList();

            foreach (var objective in objectiveRecords)
                connection.Insert(objective, transaction);
        }

        public void DeleteCharacterOnAccount(WorldClient client, Character character)
        {
            if (character == null)
                return;

            DeleteCharacterOnAccount(client, character.Id);
        }

        public void DeleteCharacterOnAccount(WorldClient client, int characterId)
        {
            Characters.TryGetValue(characterId, out var character);
            var characterName = character?.Name;

            if (character != null)
                character.DetachForDeletion(client);

            try
            {
                DeleteCharacterFromDatabase(characterId);
            }
            catch (Exception ex)
            {
                Logger.WriteError($"[ Character ] Failed to delete character {characterId} ({characterName ?? "unknown"}) from database: {ex}");
                throw;
            }

            Characters.Remove(characterId);
            Logger.Write($"[ Character ] {(characterName ?? characterId.ToString())} (Id={characterId}) deleted from database and server memory; name is available again.");
        }

        private void DeleteCharacterFromDatabase(int characterId)
        {
            CharacterDopeulBootstrap.EnsureCooldownTable();

            using var connection = DatabaseManager.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                ExecuteCharacterCascadeDelete(connection, transaction, characterId);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static void ExecuteCharacterCascadeDelete(MySqlConnection connection, MySqlTransaction transaction, int characterId)
        {
            var ownerParams = new { OwnerId = characterId };
            var characterParams = new { CharacterId = characterId };

            connection.Execute("DELETE FROM characters_items WHERE OwnerId = @OwnerId", ownerParams, transaction);
            connection.Execute("DELETE FROM characters_items_merchant WHERE OwnerId = @OwnerId", ownerParams, transaction);
            connection.Execute("DELETE FROM characters_stats WHERE OwnerId = @OwnerId", ownerParams, transaction);
            connection.Execute("DELETE FROM characters_spells WHERE OwnerId = @OwnerId", ownerParams, transaction);
            connection.Execute("DELETE FROM characters_alignments WHERE OwnerId = @OwnerId", ownerParams, transaction);
            connection.Execute("DELETE FROM characters_shortcuts_items WHERE OwnerId = @OwnerId", ownerParams, transaction);
            connection.Execute("DELETE FROM characters_shortcuts_spells WHERE OwnerId = @OwnerId", ownerParams, transaction);
            connection.Execute("DELETE FROM characters_shortcuts_items_presets WHERE OwnerId = @OwnerId", ownerParams, transaction);
            connection.Execute("DELETE FROM characters_items_presets WHERE OwnerId = @OwnerId", ownerParams, transaction);
            connection.Execute("DELETE FROM characters_jobs WHERE OwnerId = @OwnerId", ownerParams, transaction);
            connection.Execute("DELETE FROM characters_quests WHERE OwnerId = @OwnerId", ownerParams, transaction);
            connection.Execute("DELETE FROM characters_quests_objectives WHERE OwnerId = @OwnerId", ownerParams, transaction);
            connection.Execute("DELETE FROM characters_quests_steps WHERE OwnerId = @OwnerId", ownerParams, transaction);
            connection.Execute(
                "DELETE FROM character_friends WHERE CharacterId = @CharacterId OR FriendCharacterId = @CharacterId",
                characterParams,
                transaction);
            connection.Execute(
                "DELETE FROM character_enemies WHERE CharacterId = @CharacterId OR EnemyCharacterId = @CharacterId",
                characterParams,
                transaction);
            connection.Execute("DELETE FROM characters_dopeul_cooldown WHERE CharacterId = @CharacterId", characterParams, transaction);
            connection.Execute("DELETE FROM world_maps_merchant WHERE CharacterId = @CharacterId", characterParams, transaction);
            connection.Execute("DELETE FROM worlds_characters WHERE Owner = @OwnerId", ownerParams, transaction);
            connection.Execute("DELETE FROM characters WHERE Id = @OwnerId", ownerParams, transaction);
        }

        public Character GetCharacter(string name)
        {
            var character = Characters.FirstOrDefault(x => x.Value.Name == name && x.Value.IsInWorld());
            return character.Value ?? null;
        }

        public Character GetCharacter(int id)
        {
            var character = Characters.FirstOrDefault(x => x.Value.Id == id && x.Value.IsInWorld());
            return character.Value ?? null;
        }

        public Character GetCharacter(int map, short cell)
        {
            var character = Characters.FirstOrDefault(x => x.Value.Map.Id == map && x.Value.Cell.Id == cell
                                                                                 && x.Value.IsInWorld());
            return character.Value ?? null;
        }

        public Character GetCharacterByAccount(int id)
        {
            var character = Characters.FirstOrDefault(x => x.Value.Account.Id == id && x.Value.IsInWorld());
            return character.Value ?? Characters.FirstOrDefault(x => x.Value.Account.Id == id).Value;
        }

        public bool DoesNameExist(string name)
        {
            if (AccountManager.Instance.IsCharacterNameTaken(name))
                return true;

            return Characters.Values.Any(entry =>
                entry != null && string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public string GenerateName()
        {
            string text;
            do
            {
                System.Random random = new System.Random();
                int num = random.Next(5, 10);
                text = string.Empty;
                bool flag = random.Next(0, 2) == 0;
                text += Utils.GetChar(flag, random).ToString(System.Globalization.CultureInfo.InvariantCulture).ToUpper();
                flag = !flag;
                for (int i = 0; i < num - 1; i++)
                {
                    text += Utils.GetChar(flag, random);
                    flag = !flag;
                }
            }
            while (DoesNameExist(text));
            return text;
        }
    }
}
