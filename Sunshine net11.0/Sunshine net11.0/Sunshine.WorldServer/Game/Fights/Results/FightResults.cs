using Sunshine.MySql.Database.Managers;
using Sunshine.BaseServer.Configuration;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Actors.Characters.Quests;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors.TaxCollectors;
using Sunshine.WorldServer.Game.Fights.Types;
using Sunshine.WorldServer.Handlers.Guilds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sunshine.WorldServer.Game.Actors.Monsters;

namespace Sunshine.WorldServer.Game.Fights.Results
{
    public class FightResults
    {
        private Fight _fight;
        private List<FightResultAdditionalData> _resultAdditional;
        private IEnumerable<FightActor> _monsters;
        private IEnumerable<FightActor> _characters;
        private TaxCollector _taxCollector;

        public FightResults(Fight fight)
        {
            _fight = fight;
            _characters = new List<FightActor>();
            _monsters = new List<FightActor>();
            FightResultsListEntry = new List<FightResultListEntry>();
        }

        public List<FightResultListEntry> FightResultsListEntry { get; set; }

        public void Apply()
        {
            _characters = _fight.GetAllFighters(x => x is CharacterFighter);

            _monsters = _fight.GetAllFighters(x => x is MonsterFighter);

            _taxCollector = (TaxCollector)_fight.Map.RolePlayActors.FirstOrDefault(x => x is TaxCollector);

            if (_fight is FightPvPrism)
            {
                var prismFight = _fight as FightPvPrism;
                var prismFighter = prismFight.Team.Defenders.FirstOrDefault(x => x is PrismFighter) as PrismFighter;
                if (prismFighter != null)
                {
                    if (_fight.Losers.Contains(prismFighter))
                        PrismManager.Instance.MarkDefeated(prismFight.PrismRecord);
                    else
                        PrismManager.Instance.MarkDefended(prismFight.PrismRecord);
                }
            }

            if (_fight.Type == FightTypeEnum.FIGHT_TYPE_PvT)
            {
                _taxCollector = (_fight.Team.Defenders.FirstOrDefault(x => x is TaxCollectorFighter) as TaxCollectorFighter).TaxCollector;

                if (_fight.Losers.Contains(_taxCollector.Fighter)) // Loose
                {
                    TaxCollectorManager.Instance.DeleteTaxCollector(_taxCollector, _taxCollector.Guild);

                    _taxCollector.FightLoot.ClearLoot();

                    while (_taxCollector.Inventory.GetItems().Count() > 0)
                    {
                        foreach (var fighter in _fight.Team.Attackers.Where(x => !(x is ISummoned)))
                        {
                            if (_fight.Winners.Contains(fighter))
                                AddDroppedTaxItems(fighter as CharacterFighter);
                        }
                    }
                }
                else // Win
                    _fight.Map.EnterActor(_taxCollector);

                for (int i = 0; i < _taxCollector.Guild.Members.Count; i++)
                {
                    if (_taxCollector.Guild.Members[i].IsConnected())
                        TaxCollectorHandler.SendTaxCollectorAttackedResultMessage(_taxCollector.Guild.Members[i].Client, _fight.Losers.Contains(_taxCollector.Fighter), _taxCollector);
                }

                _taxCollector.Fighter = null;
            }

            foreach (var fighter in _fight.GetAllFighters().Where(x => !(x is ISummoned)))
            {
                if (_fight.Winners.Contains(fighter))
                    AddWinnerFighter(fighter);
                else
                    AddLoserFighter(fighter);
            }

            if (_taxCollector != null && (_fight.Type != FightTypeEnum.FIGHT_TYPE_AGRESSION &&
                _fight.Type != FightTypeEnum.FIGHT_TYPE_CHALLENGE))
            {
                double expForGuild = 0;

                if (_taxCollector.Fighter == null)
                {
                    _taxCollector.FightLoot.ClearLoot();

                    AddDroppedTaxItems(_taxCollector);

                    expForGuild = AddEarnedTaxExperience(_taxCollector);

                    FightResultsListEntry.Add(new FightResultTaxCollectorListEntry((short)FightOutcomeEnum.RESULT_TAX, _taxCollector.FightLoot.GetLoot(), _taxCollector.Id,
                    _taxCollector.Fighter != null ? _taxCollector.Fighter.IsAlive : true, (byte)_taxCollector.Level, _taxCollector.Guild.GetBasicGuildInformations(), (int)expForGuild));
                }
            }
        }

        private void AddWinnerFighter(FightActor fighter)
        {
            _resultAdditional = new List<FightResultAdditionalData>();

            if (fighter is CharacterFighter)
            {
                AddEarnedKamas(fighter as CharacterFighter);
                AddEarnedExperience(fighter as CharacterFighter);
                AddDroppedItems(fighter as CharacterFighter);
                AddEarnedHonor(fighter as CharacterFighter);

                QuestManager.Instance.VerifyQuest((fighter as CharacterFighter).Character, QuestTypeEnum.QUEST_TYPE_WIN_VS_MONSTER_ONE_FIGHT, _monsters);

                FightResultsListEntry.Add(new FightResultPlayerListEntry((short)FightOutcomeEnum.RESULT_VICTORY, fighter.FightLoot.GetLoot(),
                    fighter.Id, fighter.IsAlive, (byte)fighter.Level, _resultAdditional));
            }
            else
            {
                if (fighter is TaxCollectorFighter)
                {
                    FightResultsListEntry.Add(new FightResultTaxCollectorListEntry((short)FightOutcomeEnum.RESULT_VICTORY, _taxCollector.FightLoot.GetLoot(), _taxCollector.Id,
                    _taxCollector.Fighter != null ? _taxCollector.Fighter.IsAlive : false, (byte)_taxCollector.Level, _taxCollector.Guild.GetBasicGuildInformations(), 0));
                }
                else
                    FightResultsListEntry.Add(new FightResultFighterListEntry((short)FightOutcomeEnum.RESULT_VICTORY, fighter.FightLoot.GetLoot(),
                     fighter.Id, fighter.IsAlive));
            }
        }

        private void AddLoserFighter(FightActor fighter)
        {
            if (fighter is CharacterFighter)
            {
                AddLostedHonor(fighter as CharacterFighter);

                FightResultsListEntry.Add(new FightResultPlayerListEntry((short)FightOutcomeEnum.RESULT_LOST, fighter.FightLoot.GetLoot(),
                    fighter.Id, fighter.IsAlive, (byte)fighter.Level, new List<FightResultAdditionalData>()));
            }
            else
            {
                if (fighter is TaxCollectorFighter)
                {
                    FightResultsListEntry.Add(new FightResultTaxCollectorListEntry((short)FightOutcomeEnum.RESULT_LOST, _taxCollector.FightLoot.GetLoot(), _taxCollector.Id,
                    _taxCollector.Fighter != null ? _taxCollector.Fighter.IsAlive : false, (byte)_taxCollector.Level, _taxCollector.Guild.GetBasicGuildInformations(), 0));
                }
                else
                    FightResultsListEntry.Add(new FightResultFighterListEntry((short)FightOutcomeEnum.RESULT_LOST, fighter.FightLoot.GetLoot(),
                        fighter.Id, fighter.IsAlive));
            }

        }

        private void AddDroppedItems(CharacterFighter fighter)
        {
            if (_fight.Type == FightTypeEnum.FIGHT_TYPE_PvM)
            {
                bool isVip = fighter.Character?.Client?.Account?.Vip == true;
                foreach (MonsterFighter monster in _monsters)
                {
                    foreach (var drop in monster.Drops)
                    {
                        int quantity = FightFormulas.CalculateWinItems(drop, monster.Grade.GradeId, fighter.Stats.Prospecting.Total, isVip);
                        if (quantity > 0)
                        {
                            var item = ItemManager.Instance.CreatePlayerItem(drop.ItemId, quantity);
                            fighter.Character.Inventory.AddItem(item, quantity);
                            fighter.FightLoot.AddItem(drop.ItemId, quantity);
                        }
                    }
                }
            }
        }

        private void AddEarnedKamas(CharacterFighter fighter)
        {
            if (fighter == null || fighter.Character == null || _fight.Type != FightTypeEnum.FIGHT_TYPE_PvM)
                return;

            int kamas = GameRates.RollFightKamas(fighter.Level);
            if (kamas <= 0)
                return;

            bool isVip = fighter.Character?.Client?.Account?.Vip == true;
            if (isVip)
                kamas = (int)System.Math.Min(int.MaxValue, kamas * 2L);

            fighter.Character.Inventory.SetKamas(kamas);
            fighter.FightLoot.AddKamas(kamas);
        }

        private void AddDroppedTaxItems(TaxCollector taxCollector)
        {
            if (_fight.Type == FightTypeEnum.FIGHT_TYPE_PvM)
            {
                foreach (MonsterFighter monster in _monsters)
                {
                    foreach (var drop in monster.Drops)
                    {
                        int quantity = FightFormulas.CalculateWinItems(drop, monster.Grade.GradeId, taxCollector.Guild.Prospecting);
                        if (quantity > 0)
                        {
                            var item = ItemManager.Instance.CreatePlayerItem(drop.ItemId, quantity);
                            taxCollector.Inventory.AddItem(item, quantity);
                            taxCollector.FightLoot.AddItem(drop.ItemId, quantity);
                        }
                    }
                }
            }
        }

        private void AddDroppedTaxItems(CharacterFighter fighter)
        {
            var itemsTax = _taxCollector.Inventory.GetItems().ToList();

            for (var i = 0; i < itemsTax.Count; i++)
            {
                if (itemsTax[i].Stack >= 1)
                {
                    int nbrAttackers = _fight.Team.Attackers.Where(x => !(x is ISummoned)).Count();

                    AsyncRandom rdn = new AsyncRandom();

                    if (nbrAttackers == rdn.Next(1, nbrAttackers))
                    {
                        var item = ItemManager.Instance.CreatePlayerItem(itemsTax[i]);
                        fighter.Character.Inventory.AddItem(item);
                        _taxCollector.Inventory.RemoveItem(itemsTax[i]);
                        fighter.FightLoot.AddItem(itemsTax[i], 1);
                    }
                }
            }
        }

        private double AddEarnedTaxExperience(TaxCollector taxCollector)
        {
            if (_fight.Type == FightTypeEnum.FIGHT_TYPE_PvM)
            {
                long totalExp = _monsters.Sum(x => (x as MonsterFighter).Grade.GradeExp);
                int totalLevelCharacters = _characters.Sum(x => (x as CharacterFighter).Level);
                int totalLevelMonsters = _monsters.Sum(x => (x as MonsterFighter).Level);

                var expAdded = FightFormulas.CalculateWinExp(taxCollector.Guild.Wisdom, 0, MonsterGroup.ForcedStarBonus, totalLevelMonsters,
                    totalLevelCharacters, totalExp, (byte)_characters.Count());

                taxCollector.GatheredExperience += expAdded;

                return expAdded;
            }
            return 0;
        }

        private void AddEarnedExperience(CharacterFighter fighter)
        {
            if (_fight.Type != FightTypeEnum.FIGHT_TYPE_PvM)
                return;

            long totalExp = _monsters.Sum(x => (x as MonsterFighter).Grade.GradeExp);
            int totalLevelCharacters = _characters.Sum(x => (x as CharacterFighter).Level);
            int totalLevelMonsters = _monsters.Sum(x => (x as MonsterFighter).Level);

            long expAdded = FightFormulas.CalculateWinExp(fighter.Stats[StatsEnum.Wisdom].Total, 0, MonsterGroup.ForcedStarBonus, totalLevelMonsters,
                totalLevelCharacters, totalExp, (byte)_characters.Count());
            expAdded = GameRates.ApplyXp(expAdded);

            bool isVipXp = fighter.Character?.Client?.Account?.Vip == true;
            if (isVipXp)
                expAdded = (long)System.Math.Min(expAdded * 2L, long.MaxValue);

            long expForMount = 0;
            bool showMountXp = false;
            var mount = fighter.Character.EquippedMount;
            if (mount != null && fighter.Character.IsRiding && mount.Level < 100)
            {
                int ratio = Math.Max(0, Math.Min(90, (int)mount.Record.GivenExperience));
                showMountXp = ratio > 0;
                if (ratio > 0)
                {
                    long rawShare = (expAdded * ratio) / 100;
                    expAdded -= rawShare;
                    expForMount = mount.AdjustGivenExperience(fighter.Character, rawShare);
                    expForMount = GameRates.ApplyMountXp(expForMount);
                    expForMount = mount.AddExperience(expForMount);
                    MountManager.Instance.Save(mount);
                    RefreshMountInformation(fighter, mount);
                }
            }

            if (fighter.Character.Guild != null)
            {
                sbyte percent = fighter.Character.GuildMember.GivenPercent;
                long expForGuild = (expAdded * percent) / 100;
                fighter.Character.Guild.AddEarnedExperience(expForGuild);
                fighter.Character.GuildMember.GivenExperience += expForGuild;
                expAdded -= expForGuild;
            }

            fighter.Character.AddExperience(expAdded);

            _resultAdditional.Add(new FightResultExperienceData(true, true, true, true, false, showMountXp, false,
                fighter.Character.Experience, fighter.Character.ExperienceLevelFloor, fighter.Character.ExperienceNextLevelFloor, (int)expAdded, 0, (int)expForMount));
        }

        private void RefreshMountInformation(CharacterFighter fighter, Game.Mounts.Mount mount)
        {
            if (fighter?.Character?.Client == null || mount == null)
                return;

            var client = fighter.Character.Client;
            if (fighter.Character.EquippedMount?.Id == mount.Id)
            {
                client.Send(new Protocol.Messages.MountSetMessage(mount.GetClientData()));
                client.Send(new Protocol.Messages.MountXpRatioMessage((sbyte)Math.Max(0, Math.Min(90, (int)mount.Record.GivenExperience))));
            }

            // Ne pas réouvrir le panneau d'informations de monture à la fin du combat.
            // On conserve uniquement la synchro de la monture équipée (set/xp ratio).
        }

        private void AddEarnedHonor(CharacterFighter fighter)
        {
            if (_fight.Type == FightTypeEnum.FIGHT_TYPE_AGRESSION)
            {
                fighter.Character.AddHonor(100);

                _resultAdditional.Add(new FightResultPvpData((byte)fighter.Character.Alignment.Grade, fighter.Character.Alignment.HonorGradeFloor,
                    fighter.Character.Alignment.HonorNextGradeFloor, fighter.Character.Alignment.Honor, 100, fighter.Character.Alignment.Dishonor, 0));
            }
        }

        private void AddLostedHonor(CharacterFighter fighter)
        {
            if (_fight.Type == FightTypeEnum.FIGHT_TYPE_AGRESSION)
            {
                fighter.Character.SubHonor(100);

                _resultAdditional.Add(new FightResultPvpData((byte)fighter.Character.Alignment.Grade, fighter.Character.Alignment.HonorGradeFloor,
                    fighter.Character.Alignment.HonorNextGradeFloor, fighter.Character.Alignment.Honor, -100, fighter.Character.Alignment.Dishonor, 0));
            }
        }
    }
}
