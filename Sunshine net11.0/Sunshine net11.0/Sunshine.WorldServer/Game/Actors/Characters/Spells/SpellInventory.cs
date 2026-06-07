using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Characters;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Characters.Spells
{
    public class SpellInventory
    {
        private Character _character;
        private List<CharacterSpellRecord> _spells;

        public SpellInventory(Character character)
        {
            _character = character;
            _spells = CharacterManager.Instance.GetCharacterSpells(character.Id);
        }

        public IEnumerable<CharacterSpellRecord> GetSpells()
        {
            return _spells;
        }

        public CharacterSpellRecord GetSpell(short spellId)
        {
            return _spells.FirstOrDefault(x => x.Spell == spellId);
        }

        public CharacterSpellRecord GetSpell(short spellId, sbyte level)
        {
            return _spells.FirstOrDefault(x => x.Spell == spellId && x.Level >= level);
        }

        public void LearnSpell(short spellId)
        {
            CharacterSpellRecord spell = new CharacterSpellRecord { OwnerId = _character.Id, Spell = spellId, Level = 1 };
            if(!this.HasSpell(spell.Spell))
            {
                _spells.Add(spell);
                InventoryHandler.SendSpellUpgradeSuccessMessage(_character.Client, spell.GetSpellItem());
            }
        }

        /// <summary>
        /// QA helper: grants every spell known to SpellManager without per-spell network packets.
        /// Returns spells added, 0 if none, -1 if blocked (e.g. in fight).
        /// </summary>
        public int LearnAllAvailableSpellsForQa()
        {
            if (_character.IsInFight())
                return -1;

            var spellManager = SpellManager.Instance;
            if (spellManager?.Spells == null || spellManager.Spells.Count == 0)
                return 0;

            int added = 0;
            foreach (var spellId in spellManager.Spells.Keys)
            {
                if (spellId <= 0 || spellId > short.MaxValue)
                    continue;

                short id = (short)spellId;
                if (HasSpell(id))
                    continue;

                _spells.Add(new CharacterSpellRecord
                {
                    OwnerId = _character.Id,
                    Spell = id,
                    Level = 1
                });
                added++;
            }

            if (added > 0)
                InventoryHandler.SendSpellListMessage(_character.Client, true);

            return added;
        }

        public void LearnSpell(CharacterSpellRecord spell)
        {
            if (!this.HasSpell(spell.Spell))
            {
                _spells.Add(spell);
                InventoryHandler.SendSpellUpgradeSuccessMessage(_character.Client, spell.GetSpellItem());
            }
        }

        public bool HasSpell(CharacterSpellRecord spell)
        {
            return this._spells.Contains(spell);
        }

        public bool HasSpell(short spellId)
        {
            return this._spells.FirstOrDefault(x => x.Spell == spellId) != null;
        }

        public bool HasSpell(short spellId, short obtainLevel)
        {
            if (!this.HasSpell(spellId))
                return false;

            var breedSpells = BreedManager.Instance.BreedSpells[_character.Breed];

            return breedSpells.FirstOrDefault(x => x.Spell == spellId && x.ObtainLevel <= obtainLevel) != null;
        }

        public bool CanBoostSpell(CharacterSpellRecord spell)
        {
            if (spell == null)
                return false;
            if (spell.Level >= 6)
                return false;
            if (spell.Level > _character.SpellsPoints)
                return false;
            if (_character.IsInFight())
                return false;

            if (!SpellManager.Instance.Spells.TryGetValue(spell.Spell, out var spellLevels) || spellLevels == null)
                return false;

            int nextLevelIndex = spell.Level;
            if (nextLevelIndex < 0 || nextLevelIndex >= spellLevels.Count)
                return false;

            var nextLevelSpell = spellLevels[nextLevelIndex];
            if (nextLevelSpell?.Template == null)
                return false;

            if (nextLevelSpell.Template.MinPlayerLevel > _character.Level)
                return false;

            return true;
        }

        public void BoostSpell(int spellId)
        {
            var spell = _spells.FirstOrDefault(x => x.Spell == spellId);
            if (spell != null)
            {
                if (!this.CanBoostSpell(spell))
                {
                    InventoryHandler.SendSpellUpgradeFailureMessage(_character.Client);
                    return;
                }
                else
                {
                    _spells.Remove(spell);
                    _character.SpellsPoints -= spell.Level;
                    spell.Level += 1;
                    _spells.Add(spell);
                    InventoryHandler.SendSpellUpgradeSuccessMessage(_character.Client, spell.GetSpellItem());
                    return;
                }
            }
            else
            {
                InventoryHandler.SendSpellUpgradeFailureMessage(_character.Client);
                return;
            }
        }

        public void ResetAllSpellLevelsToBase()
        {
            foreach (var spell in _spells)
                spell.Level = 1;
        }
    }
}
