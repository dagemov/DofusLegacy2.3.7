using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.Auth;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Dapper;
using Dapper.Contrib.Extensions;
using Sunshine.Protocol.Utils;
using Sunshine.Protocol.Types;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Sunshine.MySql.Database.World;
using Sunshine.MySql.Database.Auth.Accounts;
using Sunshine.MySql.Database.World.Characters;
using Sunshine.MySql.Database.Auth.Worlds;

namespace Sunshine.MySql.Database.Managers
{
    public class AccountManager : Singleton<AccountManager>
    {
        public Protocol.Types.Version requiredVersion = new Protocol.Types.Version(2, 3, 7, 35100, 0, 0);

        public Account GetAccount(string username, bool isForServer = false)
        {
            try
            {
                AccountVipBootstrap.EnsureVipColumn();
                if (isForServer)
                {
                    var connectedCharacter = CharacterManager.Instance.Characters.Values
                        .FirstOrDefault(x => x?.Account != null && string.Equals(x.Account.Username, username, StringComparison.OrdinalIgnoreCase));

                    if (connectedCharacter?.Account != null)
                        return connectedCharacter.Account;
                }
                return DatabaseManager.Connection.QueryFirstOrDefault<Account>("SELECT * FROM accounts WHERE LOWER(Username) = LOWER(@Username) LIMIT 1", new { Username = (username ?? string.Empty).Trim() });
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Account GetAccountByTicket(string ticket)
        {
            try
            {
                return DatabaseManager.Connection.QueryFirstOrDefault<Account>("SELECT * FROM accounts WHERE Ticket = @Ticket LIMIT 1", new { Ticket = ticket });
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Account GetAccount(int characterId)
        {
            var worldCharacter = DatabaseManager.Connection.QueryFirstOrDefault<WorldCharacter>($"SELECT * FROM worlds_characters WHERE Owner = {characterId}");
            if (worldCharacter == null)
            {
                Logs.Logger.WriteError($"Aucun lien worlds_characters pour le personnage {characterId}");
                return null;
            }

            var account = DatabaseManager.Connection.QueryFirstOrDefault<Account>($"SELECT * FROM accounts WHERE Id = '{worldCharacter.Account}'");
            if (account == null)
                Logs.Logger.WriteError($"Aucun compte trouvé pour le personnage {characterId} (accountId={worldCharacter.Account})");

            return account;
        }

        public Account GetAccountById(int accountId)
        {
            try
            {
                return DatabaseManager.Connection.QueryFirstOrDefault<Account>("SELECT * FROM accounts WHERE Id = @Id", new { Id = accountId });
            }
            catch (Exception)
            {
                return null;
            }
        }

        public CharacterRecord[] GetCharacters(int accountId)
        {
            CharacterPaddockBootstrap.EnsurePaddockIdColumn();
            var characters = new List<CharacterRecord>();
            var worldCharacter = DatabaseManager.Connection.Query<WorldCharacter>($"SELECT * FROM worlds_characters WHERE Account = '{accountId}'");
            foreach (var chr in worldCharacter)
            {
                CharacterRecord _character = DatabaseManager.Connection.QueryFirstOrDefault<CharacterRecord>($"SELECT * FROM characters WHERE Id = '{chr.Owner}'");
                characters.Add(_character);
            }
            return characters.ToArray();
        }

        public CharacterRecord[] GetCharacters(int accountId, int serverId)
        {
            CharacterPaddockBootstrap.EnsurePaddockIdColumn();
            List<CharacterRecord> characters = new List<CharacterRecord>();
            var worldCharacter = DatabaseManager.Connection.Query<WorldCharacter>("SELECT * FROM worlds_characters WHERE Account = @Account AND World = @World", new { Account = accountId, World = serverId });
            foreach (var chr in worldCharacter)
            {
                CharacterRecord _character = DatabaseManager.Connection.QueryFirstOrDefault<CharacterRecord>($"SELECT * FROM characters WHERE Id = '{chr.Owner}'");
                characters.Add(_character);
            }
            return characters.ToArray();
        }

        public CharacterRecord GetCharacter(int characterId)
        {
            CharacterPaddockBootstrap.EnsurePaddockIdColumn();
            return DatabaseManager.Connection.QueryFirstOrDefault<CharacterRecord>($"SELECT * FROM characters WHERE Id = '{characterId}'");
        }

        public CharacterRecord GetFirstCharacter(int accountId)
        {
            CharacterPaddockBootstrap.EnsurePaddockIdColumn();
            var character = DatabaseManager.Connection.QueryFirstOrDefault<WorldCharacter>($"SELECT * FROM worlds_characters WHERE Account = '{accountId}'");
            return character == null ? null : DatabaseManager.Connection.QueryFirstOrDefault<CharacterRecord>($"SELECT * FROM characters WHERE Id = '{character.Owner}'");
        }

        public CharacterRecord GetCharacter(string name)
        {
            CharacterPaddockBootstrap.EnsurePaddockIdColumn();
            return DatabaseManager.Connection.QueryFirstOrDefault<CharacterRecord>(
                "SELECT * FROM characters WHERE LOWER(Name) = LOWER(@Name) LIMIT 1",
                new { Name = (name ?? string.Empty).Trim() });
        }

        public bool IsCharacterNameTaken(string name, int? exceptCharacterId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            CharacterPaddockBootstrap.EnsurePaddockIdColumn();
            var existingId = DatabaseManager.Connection.QueryFirstOrDefault<int?>(
                "SELECT Id FROM characters WHERE LOWER(Name) = LOWER(@Name) LIMIT 1",
                new { Name = name.Trim() });

            if (!existingId.HasValue)
                return false;

            return !exceptCharacterId.HasValue || existingId.Value != exceptCharacterId.Value;
        }

        public void InsertCharacter(WorldCharacter worldCharacter)
        {
            string cmd = "INSERT INTO worlds_characters (Account, Owner, World) values (@Account, @Owner, @World)";
            DatabaseManager.Connection.Execute(cmd, worldCharacter);
        }

        public void CreateAccount(Account account)
        {
            DatabaseManager.Connection.Insert(account);
            Logs.Logger.WriteInfo($"account {account.Username} was create...");
        }

        public void UpdateAccount(Account account)
        {
            const string cmd = @"UPDATE accounts SET Ticket = @Ticket, RegisteredIP = @RegisteredIP, Tokens = @Tokens, NewTokens = @NewTokens WHERE Id = @Id";
            DatabaseManager.Connection.Execute(cmd, account);
        }

        public void UpdateAccountTokens(Account account)
        {
            if (account == null)
                return;

            const string cmd = @"UPDATE accounts SET Tokens = @Tokens, NewTokens = @NewTokens WHERE Id = @Id";
            DatabaseManager.Connection.Execute(cmd, account);
        }

        public void UpdateVip(int accountId, bool vip)
        {
            AccountVipBootstrap.EnsureVipColumn();
            DatabaseManager.Connection.Execute("UPDATE accounts SET Vip = @Vip WHERE Id = @Id", new { Vip = vip, Id = accountId });
        }

        public bool IsBadVersion(Protocol.Types.Version clientVersion)
        {
            if (Equals(requiredVersion, clientVersion))
                return false;

            return false;
        }
    }
}
