using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors.Monsters;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Handlers.Characters;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using Sunshine.WorldServer.Handlers.Characters.Shorcuts;
using Sunshine.WorldServer.Handlers.Context;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Fighters
{
    public class SlaveFighter : SummonedMonster
    {
        public const int ControlSummonsStateId = 3000;
        public const int RoublabotMonsterId = 3120;
        public const int RoublabotSpellId = 430;

        private bool _deathHandled;
        private bool _summonerContextRestored;

        public bool IsAutoUnspawning { get; set; }

        public SlaveFighter(Monster monster, FightActor summoner, ObjectPosition position)
            : base(monster, summoner, position)
        {
            if (IsRoublabot)
                ForceRoublabotStats();
        }

        public bool IsRoublabot => Monster?.Record?.Id == RoublabotMonsterId;

        public bool IsControlledBySummoner()
        {
            if (Summoner is not CharacterFighter characterFighter || characterFighter.Client == null)
                return false;

            return IsRoublabot || characterFighter.HasState((SpellStatesEnum)ControlSummonsStateId);
        }

        public bool MustAutoUnspawnAfterTurn => IsRoublabot;

        public Spell GetSpell(short spellId)
        {
            return GetPlayableSpells().FirstOrDefault(x => x != null && x.Id == spellId);
        }

        public IEnumerable<Spell> GetPlayableSpells()
        {
            return Monster?.Spells?
                .Where(x => x != null)
                .GroupBy(x => x.Id)
                .Select(x => x.OrderByDescending(y => y.Level).First())
                .OrderBy(x => x.Id)
                ?? Enumerable.Empty<Spell>();
        }

        public IEnumerable<SpellItem> GetSpellItems()
        {
            return GetPlayableSpells().Select(x => new SpellItem(63, (short)x.Id, x.Level));
        }

        public IEnumerable<Shortcut> GetSpellShortcuts()
        {
            var slot = 0;
            return GetPlayableSpells()
                .Select(x => (Shortcut)new ShortcutSpell(slot++, (short)x.Id));
        }

        public void SendControlContext()
        {
            if (Summoner is not CharacterFighter characterFighter || characterFighter.Client == null)
                return;

            _summonerContextRestored = false;

            // vide d'abord la barre actuelle pour éviter le mélange visuel
            for (int slot = 0; slot < 40; slot++)
                ShortcutHandler.SendShortcutBarRemovedMessage(
                    characterFighter.Client,
                    ShortcutBarEnum.SPELL,
                    slot);

            ContextHandler.SendSlaveSwitchContextMessage(characterFighter.Client, this);
            ShortcutHandler.SendShortcutBarContentMessage(characterFighter.Client, ShortcutBarEnum.SPELL, GetSpellShortcuts());
        }

        public void RestoreSummonerContext()
        {
            if (_summonerContextRestored)
                return;

            if (Summoner is not CharacterFighter characterFighter || characterFighter.Client == null || characterFighter.Character == null)
                return;

            _summonerContextRestored = true;

            var client = characterFighter.Client;

            if (Fight != null)
            {
                ContextHandler.SendGameFightTurnListMessage(
                    new List<WorldClient> { client },
                    Fight);

                ContextHandler.SendGameFightSynchronizeMessage(
                    new List<WorldClient> { client },
                    Fight.TimeLine?.Fighters ?? Enumerable.Empty<FightActor>());
            }

            // 1ère passe immédiate
            ForceRefreshSummonerSpellUi(characterFighter);

            // 2ème passe différée : corrige le cas où le client garde le rendu du slave
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(150);

                    if (characterFighter.Client == client && characterFighter.Character != null)
                        ForceRefreshSummonerSpellUi(characterFighter);
                }
                catch
                {
                }
            });
        }

        private void ForceRefreshSummonerSpellUi(CharacterFighter characterFighter)
        {
            if (characterFighter?.Client == null || characterFighter.Character == null)
                return;

            var client = characterFighter.Client;

            // Vide complètement la barre SPELL côté client
            for (int slot = 0; slot < 40; slot++)
                ShortcutHandler.SendShortcutBarRemovedMessage(client, ShortcutBarEnum.SPELL, slot);

            // Rejoue l'ordre proche d'un vrai chargement de perso
            ShortcutHandler.SendShortcutBarContentMessage(client, ShortcutBarEnum.SPELL);
            ShortcutHandler.SendShortcutBarContentMessage(client, ShortcutBarEnum.OBJECT);
            InventoryHandler.SendSpellListMessage(client, true);
            InventoryHandler.SendInventoryContentMessage(client);
            InventoryHandler.SendInventoryWeightMessage(client);
            CharacterHandler.SendCharacterStatsListMessage(client);

            // Force le redraw slot par slot
            var spellShortcuts = characterFighter.Character.Shortcuts?.SpellShortcuts?
                .Where(x => x != null)
                .OrderBy(x => x.Slot)
                .ToArray();

            if (spellShortcuts != null)
            {
                foreach (var shortcut in spellShortcuts)
                    ShortcutHandler.SendShortcutBarRefreshMessage(client, ShortcutBarEnum.SPELL, shortcut);
            }

            characterFighter.Character.RefreshStats();
        }

        public CharacterCharacteristicsInformations GetSlaveCharacteristicsInformations()
        {
            if (Summoner is not CharacterFighter characterFighter || characterFighter.Character == null)
                return new CharacterCharacteristicsInformations();

            var character = characterFighter.Character;
            return new CharacterCharacteristicsInformations(
                character.Experience,
                character.ExperienceLevelFloor,
                character.ExperienceNextLevelFloor,
                (int)character.Inventory.Kamas,
                character.StatsPoints,
                character.SpellsPoints,
                character.GetActorExtendedAlignmentInformations(),
                Stats.Health.Total,
                Stats.Health.TotalMax,
                1000,
                1000,
                (short)Stats.AP.Total,
                (short)Stats.MP.Total,
                Stats[StatsEnum.Initiative],
                Stats[StatsEnum.Prospecting],
                Stats[StatsEnum.AP],
                Stats[StatsEnum.MP],
                Stats[StatsEnum.Strength],
                Stats[StatsEnum.Vitality],
                Stats[StatsEnum.Wisdom],
                Stats[StatsEnum.Chance],
                Stats[StatsEnum.Agility],
                Stats[StatsEnum.Intelligence],
                Stats[StatsEnum.Range],
                Stats[StatsEnum.SummonLimit],
                Stats[StatsEnum.DamageReflection],
                Stats[StatsEnum.CriticalHit],
                0,
                Stats[StatsEnum.CriticalMiss],
                Stats[StatsEnum.HealBonus],
                Stats[StatsEnum.DamageBonus],
                Stats[StatsEnum.WeaponDamageBonus],
                Stats[StatsEnum.DamageBonusPercent],
                Stats[StatsEnum.TrapBonus],
                Stats[StatsEnum.TrapBonusPercent],
                Stats[StatsEnum.PermanentDamagePercent],
                Stats[StatsEnum.TackleBlock],
                Stats[StatsEnum.TackleEvade],
                Stats[StatsEnum.APAttack],
                Stats[StatsEnum.MPAttack],
                Stats[StatsEnum.PushDamageBonus],
                Stats[StatsEnum.CriticalDamageBonus],
                Stats[StatsEnum.NeutralDamageBonus],
                Stats[StatsEnum.EarthDamageBonus],
                Stats[StatsEnum.WaterDamageBonus],
                Stats[StatsEnum.AirDamageBonus],
                Stats[StatsEnum.FireDamageBonus],
                Stats[StatsEnum.DodgeAPProbability],
                Stats[StatsEnum.DodgeMPProbability],
                Stats[StatsEnum.NeutralResistPercent],
                Stats[StatsEnum.EarthResistPercent],
                Stats[StatsEnum.WaterResistPercent],
                Stats[StatsEnum.AirResistPercent],
                Stats[StatsEnum.FireResistPercent],
                Stats[StatsEnum.NeutralElementReduction],
                Stats[StatsEnum.EarthElementReduction],
                Stats[StatsEnum.WaterElementReduction],
                Stats[StatsEnum.AirElementReduction],
                Stats[StatsEnum.FireElementReduction],
                Stats[StatsEnum.PushDamageReduction],
                Stats[StatsEnum.CriticalDamageReduction],
                Stats[StatsEnum.PvpNeutralResistPercent],
                Stats[StatsEnum.PvpEarthResistPercent],
                Stats[StatsEnum.PvpWaterResistPercent],
                Stats[StatsEnum.PvpAirResistPercent],
                Stats[StatsEnum.PvpFireResistPercent],
                Stats[StatsEnum.PvpNeutralElementReduction],
                Stats[StatsEnum.PvpEarthElementReduction],
                Stats[StatsEnum.PvpWaterElementReduction],
                Stats[StatsEnum.PvpAirElementReduction],
                Stats[StatsEnum.PvpFireElementReduction],
                new List<CharacterSpellModification>());
        }

        public override GameFightFighterInformations GetGameFightFighterInformations(WorldClient client = null)
        {
            return new GameFightMonsterInformations(
                Id,
                Look.GetEntityLook(),
                GetEntityDispositionInformations(client),
                IsAttacker() ? (sbyte)TeamEnum.TEAM_CHALLENGER : (sbyte)TeamEnum.TEAM_DEFENDER,
                IsAlive,
                BuildGameFightMinimalStats(client),
                (short)Monster.Record.Id,
                (sbyte)Monster.Grade.GradeId);
        }

        public override GameFightFighterInformations GetGameFightFighterPreparationInformations(WorldClient client = null)
        {
            return new GameFightMonsterInformations(
                Id,
                Look.GetEntityLook(),
                GetEntityDispositionInformations(client),
                IsAttacker() ? (sbyte)TeamEnum.TEAM_CHALLENGER : (sbyte)TeamEnum.TEAM_DEFENDER,
                IsAlive,
                BuildGameFightMinimalStatsPreparation(client),
                (short)Monster.Record.Id,
                (sbyte)Monster.Grade.GradeId);
        }

        public override void Die(FightActor byFighter = null)
        {
            if (_deathHandled)
                return;

            _deathHandled = true;

            var fight = Fight;
            var killer = byFighter ?? Summoner ?? this;
            bool mustDisappearCompletely = IsRoublabot;

            if (Stats != null)
                Stats.Health.Taken = Stats.Health.TotalMax;

            if (mustDisappearCompletely)
                CleanupFromFight(fight);

            Summoner?.RemoveSummon(this);

            if (IsAutoUnspawning && fight != null && fight.FighterPlaying == this)
                fight.FighterPlaying = null;

            base.OnDead(this, killer);
            IsAutoUnspawning = false;

            if (fight != null && mustDisappearCompletely)
            {
                fight.TimeLine?.Leavers?.Remove(this);
                ContextHandler.SendGameFightUpdateTeamMessage(fight.Clients, fight.Team);
                ContextHandler.SendGameFightTurnListMessage(fight.Clients, fight);
                ContextHandler.SendGameFightSynchronizeMessage(fight.Clients, fight.TimeLine?.Fighters ?? Enumerable.Empty<FightActor>());
            }

            RestoreSummonerContext();
        }

        public override void OnDead(FightActor target, FightActor fighter)
        {
            if (target != this)
            {
                base.OnDead(target, fighter);
                return;
            }

            if (_deathHandled)
                return;

            Die(fighter ?? Summoner ?? this);
        }

        private void CleanupFromFight(Fight fight)
        {
            if (fight == null)
                return;

            fight.TimeLine?.Fighters?.Remove(this);
            fight.TimeLine?.Leavers?.Remove(this);
            fight.Team?.Attackers?.Remove(this);
            fight.Team?.Defenders?.Remove(this);
            fight.RemoveTriggers(this);
        }

        private void ForceRoublabotStats()
        {
            if (!IsRoublabot || Stats == null)
                return;

            Stats[StatsEnum.Vitality].Base = 0;
            Stats[StatsEnum.Vitality].Equiped = 0;
            Stats[StatsEnum.Vitality].Context = 0;
            Stats.Health.Base = 1;
            Stats.Health.Equiped = 0;
            Stats.Health.Context = 0;
            Stats.Health.PermanentTaken = 0;
            Stats.Health.Taken = 0;
        }

        private GameFightMinimalStats BuildGameFightMinimalStats(WorldClient client = null)
        {
            return new GameFightMinimalStats(
                Stats.Health.Total,
                Stats.Health.TotalMax,
                Stats.Health.Base,
                Stats[StatsEnum.PermanentDamagePercent].TotalMax,
                Stats[StatsEnum.Shield].TotalMax,
                (short)Stats.AP.Total,
                (short)Stats.MP.Total,
                Summoner?.Id ?? 0,
                (short)Stats[StatsEnum.NeutralResistPercent].TotalMax,
                (short)Stats[StatsEnum.EarthResistPercent].TotalMax,
                (short)Stats[StatsEnum.WaterResistPercent].TotalMax,
                (short)Stats[StatsEnum.AirResistPercent].TotalMax,
                (short)Stats[StatsEnum.FireResistPercent].TotalMax,
                (short)Stats[StatsEnum.DodgeAPProbability].TotalMax,
                (short)Stats[StatsEnum.DodgeMPProbability].TotalMax,
                (short)Stats[StatsEnum.TackleBlock].TotalMax,
                (short)Stats[StatsEnum.TackleEvade].TotalMax,
                client == null ? (sbyte)Visibility : (sbyte)GetVisibleFor(client.Character));
        }

        private GameFightMinimalStatsPreparation BuildGameFightMinimalStatsPreparation(WorldClient client = null)
        {
            return new GameFightMinimalStatsPreparation(
                Stats.Health.Total,
                Stats.Health.TotalMax,
                Stats.Health.Base,
                Stats[StatsEnum.PermanentDamagePercent].TotalMax,
                Stats[StatsEnum.Shield].TotalMax,
                (short)Stats.AP.Total,
                (short)Stats.MP.Total,
                Summoner?.Id ?? 0,
                (short)Stats[StatsEnum.NeutralResistPercent].TotalMax,
                (short)Stats[StatsEnum.EarthResistPercent].TotalMax,
                (short)Stats[StatsEnum.WaterResistPercent].TotalMax,
                (short)Stats[StatsEnum.AirResistPercent].TotalMax,
                (short)Stats[StatsEnum.FireResistPercent].TotalMax,
                (short)Stats[StatsEnum.DodgeAPProbability].TotalMax,
                (short)Stats[StatsEnum.DodgeMPProbability].TotalMax,
                (short)Stats[StatsEnum.TackleBlock].TotalMax,
                (short)Stats[StatsEnum.TackleEvade].TotalMax,
                client == null ? (sbyte)Visibility : (sbyte)GetVisibleFor(client.Character),
                Stats[StatsEnum.Initiative].TotalMax);
        }
    }
}
