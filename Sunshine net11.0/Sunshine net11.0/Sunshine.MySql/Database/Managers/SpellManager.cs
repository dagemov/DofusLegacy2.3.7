using Dapper;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.World.Spells;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Effects;
using Sunshine.WorldServer.Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.Managers
{
    public class SpellManager : Singleton<SpellManager>
    {
        public Dictionary<int, List<Spell>> Spells = new Dictionary<int, List<Spell>>();

        public IEnumerable<SpellTemplate> GetSpellTemplate(int spellId)
        {
            return DatabaseManager.Connection.Query<SpellTemplate>($"SELECT * FROM spells_levels WHERE SpellId = '{spellId}'");
        }

        public IEnumerable<SpellTemplate> GetAllSpellsTemplate()
        {
            return DatabaseManager.Connection.Query<SpellTemplate>($"SELECT * FROM spells_levels");
        }

        public Spell GetSpell(SpellTemplate spellTemplate, sbyte level)
        {
            var effects = ResolveSpellEffects(spellTemplate.Effects, spellTemplate.BinaryEffect, spellTemplate.BinaryEffects);
            var criticalEffects = ResolveSpellEffects(spellTemplate.CriticalEffects, spellTemplate.BinaryCriticalEffect, spellTemplate.BinaryCriticalEffects);
            var spell = new Spell(spellTemplate.SpellId, level)
            {
                Effects = effects,
                CriticalEffects = criticalEffects,
                Template = spellTemplate,
                StatesForbidden = string.IsNullOrWhiteSpace(spellTemplate.StatesForbiddenCSV) ? new short[0] : 
                    spellTemplate.StatesForbiddenCSV.Split(',').Select(x => short.Parse(x)).ToArray(),
                StatesRequired = string.IsNullOrWhiteSpace(spellTemplate.StatesRequiredCSV) ? new short[0] : 
                    spellTemplate.StatesRequiredCSV.Split(',').Select(x => short.Parse(x)).ToArray()
            };
            return spell;
        }

        private List<Effect> ResolveSpellEffects(string serializedEffects, params byte[][] binaryCandidates)
        {
            if (!string.IsNullOrWhiteSpace(serializedEffects))
                return EffectManager.Instance.GetEffects(serializedEffects);

            foreach (var buffer in binaryCandidates)
            {
                if (buffer != null && buffer.Length > 0)
                    return EffectManager.Instance.GetEffects(buffer);
            }

            return new List<Effect>();
        }
    }
}
