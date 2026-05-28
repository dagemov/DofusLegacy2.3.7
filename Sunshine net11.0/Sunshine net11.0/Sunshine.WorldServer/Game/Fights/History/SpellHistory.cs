using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Fights.History
{
    public class SpellHistory
    {
        public Dictionary<int, List<SpellHistoryEntry>> _spellsHistory;

        public SpellHistory(FightActor actor)
        {
            Owner = actor;
            _spellsHistory = new Dictionary<int, List<SpellHistoryEntry>>();
        }

        public FightActor Owner { get; }

        public int CurrentRound { get { return Owner.Fight.TimeLine.RoundNumber; } }

        public void RegisterCastedSpell(Spell spell, FightActor target)
        {
            if (target == null)
                return;

            if (_spellsHistory.ContainsKey(spell.Id))
                _spellsHistory[spell.Id].Add(new SpellHistoryEntry(spell, Owner, target, CurrentRound));
            else
                _spellsHistory.Add(spell.Id, new List<SpellHistoryEntry> { new SpellHistoryEntry(spell, Owner, target, CurrentRound) });
        }

        public bool CanCastSpell(Spell spell, short targetedCell)
        {
            if (_spellsHistory.Count == 0)
                return true;

            if (!_spellsHistory.ContainsKey(spell.Id))
                return true;

            SpellHistoryEntry spellHistoryEntry = _spellsHistory[spell.Id].LastOrDefault();

            if (spellHistoryEntry == null)
                return true;

            if (this.CurrentRound < (long)((ulong)spell.Template.InitialCooldown))
                return false;
            else
            {
                if (spellHistoryEntry.IsGlobalCooldownActive(this.CurrentRound))
                    return false;
                else
                {
                    SpellHistoryEntry[] array = _spellsHistory[spell.Id].Where(x => x.CastRound == this.CurrentRound).ToArray();
                        
                    if (array.Length == 0)
                        return true;
                    else
                    {
                        if (spell.Template.MaxCastPerTurn > 0u && (long)array.Length >= (long)((ulong)spell.Template.MaxCastPerTurn))
                            return false;
                        else
                        {
                            FightActor target = this.Owner.Fight.GetOneFighter(targetedCell);
                            if (target == null)
                                return true;
                            else
                            {
                                int num = array.Count((SpellHistoryEntry entry) => entry.Target != null && entry.Target.Id == target.Id);
                                return (spell.Template.MaxCastPerTarget <= 0u || (long)num < (long)((ulong)spell.Template.MaxCastPerTarget));
                            }
                        }
                    }
                }
                }
            }
    }
}
