using Sunshine.Logs;
using Sunshine.MySql.Database.Auth.Accounts;
using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Utils;
using Sunshine.Servers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.BaseServer.Commands
{
    public static class BaseCommand
    {
        public static void Execute(string cmd)
        {
            if (cmd.Contains("acc") || cmd.Contains("account"))
            {
                string[] _account = cmd.Split(' ');
                if (_account.ToList().Count > 1)
                {
                    string _user = _account[2];
                    string _pass = Utils.GetMD5Hash(_account[3]);

                    AccountManager.Instance.CreateAccount(new Account
                    {
                        Username = _user,
                        Password = _pass,
                        Nickname = _user,
                        Role = 1,
                        SecretQuestion = "sun",
                        SecretAnswer = "sun",
                        Ticket = ""
                    });
                }
                else
                    Logger.WriteInfo("Command account incorrect");
            }
            else if(cmd.Contains("save"))
                ServersManager.Instance.Save();
        }
    }
}
