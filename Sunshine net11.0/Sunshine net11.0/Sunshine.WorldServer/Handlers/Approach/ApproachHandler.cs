using Sunshine.MySql.Database.Auth;
using Sunshine.MySql.Database.Auth.Accounts;
using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Handlers.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Handlers.Approach
{
    public class ApproachHandler : WorldPacketHandler
    {
        [WorldHandler(110)]
        public static void HandleAuthenticationTicketMessage(WorldClient client, AuthenticationTicketMessage message)
        {
            Account account = AccountManager.Instance.GetAccountByTicket(message.ticket);
            
            if (account == null)
            {
                client.Send(new AuthenticationTicketRefusedMessage());
                return;
            }

            var clientConnected = WorldServerManager.Instance.GetWorldClient(account);

            if (clientConnected != null)
                clientConnected.Disconnect();

            client.Account = account;
            client.Send(new AuthenticationTicketAcceptedMessage());
            BasicHandler.SendBasicTimeMessage(client);
            client.Send(new AccountCapabilitiesMessage(client.Account.Id, false, 32767, 32767));
            client.Send(new TrustStatusMessage(true));
        }
    }
}
