using Dapper;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Social;
using Sunshine.Protocol.Types;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Characters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Social
{
    public class SocialRelationManager : Singleton<SocialRelationManager>
    {
        public void EnsureTables()
        {
            DatabaseManager.Connection.Execute(@"CREATE TABLE IF NOT EXISTS character_friends (
CharacterId INT NOT NULL,
FriendCharacterId INT NOT NULL,
FriendName VARCHAR(64) NOT NULL,
FriendAccountNickname VARCHAR(64) NULL,
PRIMARY KEY(CharacterId, FriendCharacterId),
INDEX idx_character_friends_name (CharacterId, FriendName)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;");

            DatabaseManager.Connection.Execute(@"CREATE TABLE IF NOT EXISTS character_enemies (
CharacterId INT NOT NULL,
EnemyCharacterId INT NOT NULL,
EnemyName VARCHAR(64) NOT NULL,
PRIMARY KEY(CharacterId, EnemyCharacterId),
INDEX idx_character_enemies_name (CharacterId, EnemyName)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;");

            DatabaseManager.Connection.Execute(@"CREATE TABLE IF NOT EXISTS account_bank_items (
AccountId INT NOT NULL,
ItemUid INT NOT NULL,
TemplateId INT NOT NULL,
Stack INT NOT NULL,
Position INT NOT NULL,
Effects MEDIUMTEXT NULL,
PRIMARY KEY(AccountId, ItemUid),
INDEX idx_account_bank_template (AccountId, TemplateId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;");

            DatabaseManager.Connection.Execute(@"CREATE TABLE IF NOT EXISTS accounts_bank (
AccountId INT NOT NULL PRIMARY KEY,
Kamas BIGINT NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8;");

            try
            {
                DatabaseManager.Connection.Execute("ALTER TABLE characters_items ADD COLUMN ItemUid INT NOT NULL DEFAULT 0");
            }
            catch
            {
            }

            DatabaseManager.Connection.Execute(@"CREATE TABLE IF NOT EXISTS characters_items_presets (
Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
PresetId INT NOT NULL,
OwnerId INT NOT NULL,
SymbolId INT NOT NULL,
SerializedObjects LONGBLOB NULL,
INDEX idx_characters_items_presets_owner (OwnerId),
UNIQUE KEY uq_characters_items_presets_owner_preset (OwnerId, PresetId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;");

            DatabaseManager.Connection.Execute(@"CREATE TABLE IF NOT EXISTS characters_shortcuts_items_presets (
OwnerId INT NOT NULL,
PresetId INT NOT NULL,
Slot INT NOT NULL,
PRIMARY KEY(OwnerId, Slot),
INDEX idx_characters_shortcuts_items_presets_owner (OwnerId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;");
        }

        public IEnumerable<FriendRelationRecord> GetFriends(int characterId)
            => DatabaseManager.Connection.Query<FriendRelationRecord>("SELECT * FROM character_friends WHERE CharacterId=@CharacterId ORDER BY FriendName", new { CharacterId = characterId });

        public IEnumerable<EnemyRelationRecord> GetEnemies(int characterId)
            => DatabaseManager.Connection.Query<EnemyRelationRecord>("SELECT * FROM character_enemies WHERE CharacterId=@CharacterId ORDER BY EnemyName", new { CharacterId = characterId });

        public bool AddFriend(Character owner, Character target)
        {
            if (owner == null || target == null || owner.Id == target.Id)
                return false;

            if (GetFriends(owner.Id).Any(x => x.FriendCharacterId == target.Id))
                return false;

            DatabaseManager.Connection.Execute("INSERT INTO character_friends(CharacterId, FriendCharacterId, FriendName, FriendAccountNickname) VALUES(@CharacterId, @FriendCharacterId, @FriendName, @FriendAccountNickname)",
                new { CharacterId = owner.Id, FriendCharacterId = target.Id, FriendName = target.Name, FriendAccountNickname = target.Account?.Nickname });
            return true;
        }

        public bool DeleteFriend(int ownerId, string name)
            => DatabaseManager.Connection.Execute("DELETE FROM character_friends WHERE CharacterId=@CharacterId AND FriendName=@FriendName", new { CharacterId = ownerId, FriendName = name }) > 0;

        public bool AddEnemy(Character owner, Character target)
        {
            if (owner == null || target == null || owner.Id == target.Id)
                return false;
            if (GetEnemies(owner.Id).Any(x => x.EnemyCharacterId == target.Id))
                return false;
            DatabaseManager.Connection.Execute("INSERT INTO character_enemies(CharacterId, EnemyCharacterId, EnemyName) VALUES(@CharacterId, @EnemyCharacterId, @EnemyName)",
                new { CharacterId = owner.Id, EnemyCharacterId = target.Id, EnemyName = target.Name });
            return true;
        }

        public IEnumerable<FriendInformations> BuildFriendInformations(Character owner)
        {
            foreach (var relation in GetFriends(owner.Id))
            {
                Character online = CharacterManager.Instance.Characters.Values.FirstOrDefault(x => x.Name.Equals(relation.FriendName, StringComparison.OrdinalIgnoreCase) && x.Client != null);
                if (online != null)
                {
                    yield return new FriendOnlineInformations(
                        relation.FriendAccountNickname ?? relation.FriendName,
                        1,
                        0,
                        online.Name,
                        online.Level,
                        (sbyte)online.Alignment.Side,
                        (sbyte)online.Breed,
                        online.Sex,
                        online.Guild != null ? online.Guild.GetBasicGuildInformations() : new BasicGuildInformations(0, string.Empty),
                        0);
                }
                else
                {
                    yield return new FriendInformations(relation.FriendAccountNickname ?? relation.FriendName, 0, 0);
                }
            }
        }

        public IEnumerable<IgnoredInformations> BuildIgnoredInformations(Character owner, IEnumerable<string> ignoredNames)
        {
            foreach (var ignoredName in ignoredNames.OrderBy(x => x))
            {
                Character online = CharacterManager.Instance.Characters.Values.FirstOrDefault(x => x.Name.Equals(ignoredName, StringComparison.OrdinalIgnoreCase) && x.Client != null);
                if (online != null)
                    yield return new IgnoredOnlineInformations(ignoredName, online.Id, online.Name, (sbyte)online.Breed, online.Sex);
                else
                    yield return new IgnoredInformations(ignoredName, 0);
            }
        }

        public IEnumerable<Character> FindOnlineFriendsOf(Character actor)
        {
            if (actor == null)
                yield break;

            foreach (var watcher in CharacterManager.Instance.Characters.Values.Where(x => x != null && x.Client != null))
            {
                bool hasActorInFriendList = DatabaseManager.Connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM character_friends WHERE CharacterId=@WatcherId AND FriendCharacterId=@ActorId",
                    new { WatcherId = watcher.Id, ActorId = actor.Id }) > 0;

                if (hasActorInFriendList)
                    yield return watcher;
            }
        }
    }
}
