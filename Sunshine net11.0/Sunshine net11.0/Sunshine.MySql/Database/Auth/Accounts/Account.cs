using System;
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using System.Linq;
using System.Text;
using Dapper;
using System.Threading.Tasks;
using System.Data;

namespace Sunshine.MySql.Database.Auth.Accounts
{
    [Table("accounts")]
    public class Account
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Nickname { get; set; }
        public sbyte Role { get; set; }
        public string SecretQuestion { get; set; }
        public string SecretAnswer { get; set; }
        public bool IsBanned { get; set; }
        public string Ticket { get; set; }
        public string RegisteredIP { get; set; }
        public int vote { get; set; }
        public int heurevote { get; set; }
        public int Tokens { get; set; }
        public int NewTokens { get; set; }

        // Persisted column, written via AccountManager.UpdateVip. Marked Computed so Dapper.Contrib
        // inserts/updates do not break when the `vip` column has not been migrated yet.
        [Computed]
        public bool Vip { get; set; }

    }
}
