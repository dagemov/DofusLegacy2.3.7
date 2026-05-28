using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.BaseServer.Loaders.World.Spells
{
    public static class SpellsLoader
    {
        public static void Initialize()
        {
            var spells = SpellManager.Instance.GetAllSpellsTemplate().ToList().OrderBy(x => x.SpellId).ToList();
            SpellManager.Instance.Spells.Clear();

            sbyte level = 1;
            List<Spell> spellsList = new List<Spell>();
            for (int i = 0; i < spells.Count; i++)
            {
                if (i != 0 && spells[i].SpellId != spells[i - 1].SpellId)
                {
                    SpellManager.Instance.Spells[spells[i - 1].SpellId] = new List<Spell>(spellsList);
                    spellsList.Clear();
                    level = 1;
                }

                var spell = SpellManager.Instance.GetSpell(spells[i], level);
                spellsList.Add(spell);
                level++;
            }

            if (spells.Count > 0 && spellsList.Count > 0)
                SpellManager.Instance.Spells[spells[^1].SpellId] = new List<Spell>(spellsList);
        }
    }
}
