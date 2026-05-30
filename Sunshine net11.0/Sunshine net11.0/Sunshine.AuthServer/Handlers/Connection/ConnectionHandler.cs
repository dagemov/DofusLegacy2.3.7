using Sunshine.AuthServer.Client;
using Sunshine.BaseServer.Configuration;
using Sunshine.Logs;
using Sunshine.MySql.Database.Auth;
using Sunshine.MySql.Database.Auth.Accounts;
using Sunshine.MySql.Database.Auth.Worlds;
using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sunshine.AuthServer.Handlers.Connection
{
    public class ConnectionHandler : AuthPacketHandler
    {
        [AuthHandler(4)]
        public static void HandleIdentificationMessage(AuthClient client, IdentificationMessage message)
        {
            if (client == null || message == null)
                return;

            Account account = AccountManager.Instance.GetAccount(message.login?.Trim());
            string passHash = account == null ? null : Utils.GetMD5Hash(account.Password + client.Ticket);
            LogAuthAttempt("Identification", message.login, client.Ticket, account, message.password, passHash);

            if (account == null)
            {
                client.Send(new IdentificationFailedMessage((sbyte)IdentificationFailureReasonEnum.WRONG_CREDENTIALS));
                return;
            }

            if (account.IsBanned)
            {
                client.Send(new IdentificationFailedBannedMessage((sbyte)IdentificationFailureReasonEnum.BANNED, 60));
                return;
            }

            if (AccountManager.Instance.IsBadVersion(message.version))
            {
                client.Send(new IdentificationFailedForBadVersionMessage((sbyte)IdentificationFailureReasonEnum.BAD_VERSION, AccountManager.Instance.requiredVersion));
                return;
            }

            if (message.password != passHash)
            {
                client.Send(new IdentificationFailedMessage((sbyte)IdentificationFailureReasonEnum.WRONG_CREDENTIALS));
                return;
            }

            client.Send(new IdentificationSuccessMessage(account.Role > (int)RoleEnum.Player, false, account.Nickname, account.Id, 0, account.SecretQuestion, 0));
            client.Account = account;
            client.Account.Ticket = client.Ticket;
            AccountManager.Instance.UpdateAccount(client.Account);
            SendServerListMessage(client);
        }

        [AuthHandler(6194)]
        public static void HandleIdentificationWithServerIdMessage(AuthClient client, IdentificationWithServerIdMessage message)
        {
            if (client == null || message == null)
                return;

            Account account = AccountManager.Instance.GetAccount(message.login?.Trim(), true);
            string passHash = account == null ? null : Utils.GetMD5Hash(account.Password + client.Ticket);
            LogAuthAttempt("IdentificationWithServerId", message.login, client.Ticket, account, message.password, passHash);

            if (account == null)
            {
                client.Send(new IdentificationFailedMessage((sbyte)IdentificationFailureReasonEnum.WRONG_CREDENTIALS));
                return;
            }

            if (account.IsBanned)
            {
                client.Send(new IdentificationFailedBannedMessage((sbyte)IdentificationFailureReasonEnum.BANNED, 60));
                return;
            }

            if (AccountManager.Instance.IsBadVersion(message.version))
            {
                client.Send(new IdentificationFailedForBadVersionMessage((sbyte)IdentificationFailureReasonEnum.BAD_VERSION, AccountManager.Instance.requiredVersion));
                return;
            }

            if (message.password != passHash)
            {
                client.Send(new IdentificationFailedMessage((sbyte)IdentificationFailureReasonEnum.WRONG_CREDENTIALS));
                return;
            }

            client.Send(new IdentificationSuccessMessage(account.Role > (int)RoleEnum.Player, false, account.Nickname, account.Id, 0, account.SecretQuestion, 0));
            client.Account = account;
            client.Account.Ticket = client.Ticket;
            AccountManager.Instance.UpdateAccount(client.Account);

            World world = WorldServerManager.Instance.GetWorld(message.serverId);
            SendSelectedServerDataAndDisconnect(client, world, message.serverId);
        }

        [AuthHandler(40)]
        public static void HandleServerSelectionMessage(AuthClient client, ServerSelectionMessage message)
        {
            if (client == null || message == null)
                return;

            if (client.Account == null)
            {
                client.Send(new SelectedServerRefusedMessage(
                    message.serverId,
                    (sbyte)ServerConnectionErrorEnum.SERVER_CONNECTION_ERROR_NO_REASON,
                    (sbyte)ServerStatusEnum.STATUS_UNKNOWN));
                return;
            }

            World world = WorldServerManager.Instance.GetWorld(message.serverId);
            SendSelectedServerDataAndDisconnect(client, world, message.serverId);
        }

        public static void SendServerListMessage(AuthClient client)
        {
            if (client == null || client.Account == null)
                return;

            World[] worlds = WorldServerManager.Instance.GetWorlds() ?? new World[0];
            List<GameServerInformations> serversInformations = new List<GameServerInformations>();

            foreach (var server in worlds.Where(x => x != null))
            {
                serversInformations.Add(new GameServerInformations(
                    (ushort)server.Id,
                    ClampSByte(server.Status),
                    GetServerCompletion(server),
                    server.Selectable && server.Status == (int)ServerStatusEnum.ONLINE,
                    GetCharacterCount(client.Account.Id, server.Id)));
            }

            client.Send(new ServersListMessage(serversInformations));
        }

        public static void SendServerStatusUpdateMessage(AuthClient client)
        {
            if (client == null || client.Account == null)
                return;

            World[] worlds = WorldServerManager.Instance.GetWorlds() ?? new World[0];

            foreach (var server in worlds.Where(x => x != null))
            {
                client.Send(new ServerStatusUpdateMessage(new GameServerInformations(
                    (ushort)server.Id,
                    ClampSByte(server.Status),
                    GetServerCompletion(server),
                    server.Selectable && server.Status == (int)ServerStatusEnum.ONLINE,
                    GetCharacterCount(client.Account.Id, server.Id))));
            }
        }

        private static void SendSelectedServerDataAndDisconnect(AuthClient client, World world, short requestedServerId)
        {
            if (client == null)
                return;

            if (world == null)
            {
                client.Send(new SelectedServerRefusedMessage(
                    requestedServerId,
                    (sbyte)ServerConnectionErrorEnum.SERVER_CONNECTION_ERROR_LOCATION_RESTRICTED,
                    (sbyte)ServerStatusEnum.STATUS_UNKNOWN));
                return;
            }

            if (world.Status != (int)ServerStatusEnum.ONLINE)
            {
                client.Send(new SelectedServerRefusedMessage(
                    (short)world.Id,
                    (sbyte)ServerConnectionErrorEnum.SERVER_CONNECTION_ERROR_DUE_TO_STATUS,
                    ClampSByte(world.Status)));
                return;
            }

            if (client.Account == null)
            {
                client.Send(new SelectedServerRefusedMessage(
                    (short)world.Id,
                    (sbyte)ServerConnectionErrorEnum.SERVER_CONNECTION_ERROR_NO_REASON,
                    ClampSByte(world.Status)));
                return;
            }

            if (world.RequiredRole > client.Account.Role)
            {
                client.Send(new SelectedServerRefusedMessage(
                    (short)world.Id,
                    (sbyte)ServerConnectionErrorEnum.SERVER_CONNECTION_ERROR_ACCOUNT_RESTRICTED,
                    ClampSByte(world.Status)));
                return;
            }

            if (world.Port <= 0 || world.Port > ushort.MaxValue)
            {
                client.Send(new SelectedServerRefusedMessage(
                    (short)world.Id,
                    (sbyte)ServerConnectionErrorEnum.SERVER_CONNECTION_ERROR_NO_REASON,
                    ClampSByte(world.Status)));
                return;
            }

            string address = string.IsNullOrWhiteSpace(world.Address) || world.Address.Trim() == "0.0.0.0"
                ? "127.0.0.1"
                : world.Address.Trim();

            // Dofus 2.3.7.35100 : conserver le ticket Auth original.
            // Ne pas utiliser la génération de ticket SaveKrosmoz / 2.10 ici.
            if (string.IsNullOrWhiteSpace(client.Ticket))
                client.Ticket = Utils.RandomString(32, true);

            client.Account.Ticket = client.Ticket;
            AccountManager.Instance.UpdateAccount(client.Account);

            client.SendAndDisconnect(new SelectedServerDataMessage(
                (short)world.Id,
                address,
                (ushort)world.Port,
                true,
                client.Ticket),
                1200);
        }

        private static sbyte GetServerCompletion(World server)
        {
            if (server == null || server.Status != (int)ServerStatusEnum.ONLINE)
                return 0;

            return 100;
        }

        private static sbyte GetCharacterCount(int accountId, int serverId)
        {
            try
            {
                var characters = AccountManager.Instance.GetCharacters(accountId, serverId);
                int count = characters == null ? 0 : characters.Count(x => x != null);
                return ClampSByte(count);
            }
            catch
            {
                return 0;
            }
        }

        private static sbyte ClampSByte(int value)
        {
            if (value < 0)
                return 0;

            if (value > sbyte.MaxValue)
                return sbyte.MaxValue;

            return (sbyte)value;
        }

        private static bool AuthDebugEnabled => GameConfig.GetBool("AuthDebugHashes", false);

        private static void LogAuthAttempt(string flow, string username, string ticket, Account account, string receivedHash, string expectedHash)
        {
            if (!AuthDebugEnabled)
                return;

            Logger.WriteInfo(
                $"[AUTH-DEBUG] flow={FormatValue(flow)} username={FormatValue(username)} accountFound={account != null} ticketLength={(ticket ?? string.Empty).Length} dbPasswordLength={(account?.Password ?? string.Empty).Length} dbPasswordMd5Like={IsMd5Like(account?.Password)} receivedLength={(receivedHash ?? string.Empty).Length} expected={PreviewHash(expectedHash)} received={PreviewHash(receivedHash)} match={string.Equals(receivedHash ?? string.Empty, expectedHash ?? string.Empty, StringComparison.Ordinal)}");
        }

        private static bool IsMd5Like(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, "^[0-9a-f]{32}$", RegexOptions.IgnoreCase);
        }

        private static string PreviewHash(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "<empty>";

            var trimmed = value.Trim();
            return trimmed.Length <= 10
                ? trimmed
                : $"{trimmed.Substring(0, 6)}...{trimmed.Substring(trimmed.Length - 4)}";
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<empty>" : value.Trim();
        }
    }
}
