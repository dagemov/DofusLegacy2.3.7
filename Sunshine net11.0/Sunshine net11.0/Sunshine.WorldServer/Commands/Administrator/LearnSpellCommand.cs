using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Client;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Commands.Moderator
{
    [CommandHandler("spell add", RoleEnum.Moderator)]
    public class LearnSpellCommand : WorldCommand
    {
        public override void Execute()
        {
            if (Parameters.Length < 2)
                Client.Character.SendServerMessage("Unknown spell !", Color.Red);
            else
            {
                int spellId = int.TryParse((string)Parameters[1], out int spell) ? int.Parse((string)Parameters[1]) : -1;
                if(spellId != -1 && SpellManager.Instance.Spells.ContainsKey(spellId))
                     Client.Character.Spells.LearnSpell((short)spellId);
                else
                    Client.Character.SendServerMessage("Spell doesn't exist!", Color.Red);
            }
        }

        public override string Description
        {
            get { return "Allows learn spell."; }
        }
    }
}
