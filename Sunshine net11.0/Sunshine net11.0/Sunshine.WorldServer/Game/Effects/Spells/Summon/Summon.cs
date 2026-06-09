using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors.Monsters;
using Sunshine.WorldServer.Game.Fights.Bombs;
using Sunshine.WorldServer.Game.Fights.Diagnostics;
using Sunshine.WorldServer.Game.Fights.Mechanics;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using Sunshine.WorldServer.Handlers.Actions;
using Sunshine.WorldServer.Handlers.Context;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Effects.Spells.Summon
{
    [EffectHandler(EffectsEnum.Effect_Summon)]
    [EffectHandler(EffectsEnum.Effect_185)]
    [EffectHandler(EffectsEnum.Effect_SummonBomb)]
    [EffectHandler(EffectsEnum.Effect_SummonSlave)]
    public class Summon : SpellEffectHandler
    {
        public override void Apply()
        {
            if (Caster == null || !Caster.IsAlive || Fight == null)
                return;

            int monsterId = FrigostBossMechanics.ResolveForcedSummonMonsterId(Caster, (int)Effect.DiceNum);

            if (!MonsterManager.Instance.Monsters.TryGetValue(monsterId, out var mTemplate) ||
                !MonsterManager.Instance.MonstersGrade.TryGetValue(monsterId, out var mGrades) ||
                mTemplate == null || mGrades == null || mGrades.Count == 0)
            {
                global::Sunshine.Logs.Logger.WriteError($"Cannot summon monster {monsterId} grade {Effect.DiceFace} for spell {(Spell != null ? Spell.Id : 0)}.");
                if (Caster is CharacterFighter errorCaster && errorCaster.Character != null)
                    errorCaster.Character.SendServerMessage("No se puede invocar esta entidad: plantilla no encontrada.", System.Drawing.Color.Red);
                return;
            }

            int gradeIndex = System.Math.Max(0, System.Math.Min(mGrades.Count - 1, (int)Effect.DiceFace - 1));

            Monster monster;
            try
            {
                monster = new Monster(mTemplate, mGrades[gradeIndex]);
            }
            catch (System.Exception ex)
            {
                global::Sunshine.Logs.Logger.WriteError($"Cannot instantiate summoned monster {monsterId} grade {Effect.DiceFace} for spell {(Spell != null ? Spell.Id : 0)}: {ex}");
                if (Caster is CharacterFighter summonCaster && summonCaster.Character != null)
                    summonCaster.Character.SendServerMessage("No se puede invocar esta entidad: error en la plantilla del monstruo.", System.Drawing.Color.Red);
                return;
            }

            if (Effect.Id == EffectsEnum.Effect_SummonBomb)
            {
                var bombCount = Fight.GetAllFighters()
                    .OfType<BombFighter>()
                    .Count(x => x != null && x.IsAlive && !x.IsExploded && x.Summoner == Caster);

                if (bombCount >= BombFighter.BombLimit)
                {
                    if (Caster is CharacterFighter characterCaster && characterCaster.Character != null)
                        characterCaster.Character.SendServerMessage("No puedes colocar más de 3 bombas.", System.Drawing.Color.Red);

                    return;
                }

                var occupiedTarget = Fight.GetOneFighter(TargetedCell);
                if (occupiedTarget != null && occupiedTarget.IsAlive)
                {
                    TriggerBombOnOccupiedCell(monster, occupiedTarget);
                    return;
                }
            }

            bool needsSummonSlot = Effect.Id != EffectsEnum.Effect_SummonBomb && (monster.Record?.UseSummonSlot ?? true);
            if (needsSummonSlot && !Caster.CanSummon())
            {
                if (Caster is CharacterFighter maxSummonCaster && maxSummonCaster.Character != null)
                    maxSummonCaster.Character.SendServerMessage("Has alcanzado el número máximo de invocaciones.", System.Drawing.Color.Red);
                return;
            }

            // If the target cell is occupied, relocate the summon to the nearest free cell.
            short summonCell = TargetedCell;
            if (Fight.GetOneFighter(summonCell) != null)
            {
                short freeCell = FindNearestFreeCell(summonCell, Caster.Position.Cell);
                if (freeCell >= 0)
                    summonCell = freeCell;
            }

            FightActor summonedActor;
            bool applySlaveStates = false;
            if (Effect.Id == EffectsEnum.Effect_SummonBomb)
            {
                int element = ResolveBombElement(monster, Spell != null ? Spell.Id : 0);
                summonedActor = new BombFighter(monster, Caster, new ObjectPosition(Fight.Map, summonCell, Caster.Position.Direction), element);
            }
            else
            {
                bool summonAsSlave = Effect.Id == EffectsEnum.Effect_SummonSlave
                    || monster?.Record?.Id == SlaveFighter.RoublabotMonsterId
                    || (Spell != null && Spell.Id == SlaveFighter.RoublabotSpellId);

                if (summonAsSlave)
                {
                    summonedActor = new SlaveFighter(monster, Caster, new ObjectPosition(Fight.Map, summonCell, Caster.Position.Direction));
                }
                else if (!monster.CanPlay())
                {
                    summonedActor = new SummonedStaticMonster(monster, Caster, new ObjectPosition(Fight.Map, summonCell, Caster.Position.Direction));
                }
                else
                {
                    summonedActor = new SummonedMonster(monster, Caster, new ObjectPosition(Fight.Map, summonCell, Caster.Position.Direction));
                }

                applySlaveStates = summonedActor is SlaveFighter && (Effect.Id == EffectsEnum.Effect_SummonSlave || monster?.Record?.Id == SlaveFighter.RoublabotMonsterId || (Spell != null && Spell.Id == SlaveFighter.RoublabotSpellId));
            }

            Caster.AddSummon((ISummoned)summonedActor);

            int indexSummoner = Fight.TimeLine.Fighters.IndexOf(Caster);
            if (indexSummoner >= 0)
                Fight.TimeLine.Fighters.Insert(indexSummoner + 1, summonedActor);
            else
                Fight.TimeLine.AddFighter(summonedActor);

            if (Caster.IsAttacker())
                Fight.Team.AddAttacker(Caster, summonedActor);
            else
                Fight.Team.AddDefender(Caster, summonedActor);

            ContextHandler.SendGameFightUpdateTeamMessage(Fight.Clients, Caster.Team);
            ContextHandler.SendGameFightTurnListMessage(Fight.Clients, Fight);
            ActionsHandler.SendGameActionFightSummonMessage(Fight.Clients, (ISummoned)summonedActor);

            bool usesSummonSlot = summonedActor is SummonedMonster summonSlotMonster && summonSlotMonster.Monster.Record.UseSummonSlot;
            FightCombatLogger.LogSummonCreate(Fight, Caster, summonedActor, monsterId, summonCell,
                usesSummonSlot, Caster.SummonedCount, Caster.Stats[StatsEnum.SummonLimit].Total);

            if (applySlaveStates && summonedActor is SlaveFighter slave)
            {
                slave.AddBuff(new StateBuff(Caster, slave, Spell, Effect, 1000, false, SpellStatesEnum.Rooted));
                slave.AddBuff(new StateBuff(Caster, slave, Spell, Effect, 1000, false, SpellStatesEnum.Unlockable));
            }

            Fight.TriggerMarks(summonedActor.Position.Cell, summonedActor, TriggerTypeEnum.MOVE);

            var bomb = summonedActor as BombFighter;
            if (bomb != null)
                Game.Fights.Bombs.BombManager.Instance.CheckWalls(Fight, bomb.Summoner);
        }


        private void TriggerBombOnOccupiedCell(Monster monster, FightActor occupiedTarget)
        {
            if (occupiedTarget == null || !occupiedTarget.IsAlive || occupiedTarget is BombFighter)
                return;

            int element = ResolveBombElement(monster, Spell != null ? Spell.Id : 0);
            var tempBomb = new BombFighter(monster, Caster, new ObjectPosition(Fight.Map, TargetedCell, Caster.Position.Direction), element);

            // Directly casting a bomb on a fighter must trigger the bomb immediately on that occupied cell.
            // It should affect allies, enemies and summons in the normal bomb zone, but it must not start a
            // neighbour-chain because no actual bomb is left on the map after a direct impact cast.
            var targets = BombManager.GetExplosionTargets(tempBomb).ToArray();
            if (targets.Length == 0)
                return;

            foreach (var target in targets)
            {
                var damage = BombManager.CreatePrimaryDamageForTarget(tempBomb, target, 1d, false);
                if (damage != null)
                    target.InflictDamage(damage);
            }
        }

        private short FindNearestFreeCell(short targetCell, short casterCell)
        {
            var map = Fight?.Map;
            if (map == null)
                return targetCell;

            if (Fight.IsCellFree(targetCell))
                return targetCell;

            if (Fight.IsCellFree(casterCell))
                return casterCell;

            short found = SearchFreeCellRing(targetCell);
            if (found >= 0)
                return found;

            found = SearchFreeCellRing(casterCell);
            if (found >= 0)
                return found;

            for (short i = 0; i < map.Cells.Length; i++)
            {
                if (Fight.IsCellFree(i))
                    return i;
            }

            return targetCell;
        }

        private short SearchFreeCellRing(short centerCell)
        {
            var startPoint = MapPoint.GetPoint(centerCell);
            if (startPoint == null)
                return -1;

            for (int radius = 1; radius <= 12; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (System.Math.Abs(dx) != radius && System.Math.Abs(dy) != radius)
                            continue;

                        var point = MapPoint.GetPoint(startPoint.X + dx, startPoint.Y + dy);
                        if (point != null && Fight.IsCellFree(point.CellId))
                            return point.CellId;
                    }
                }
            }
            return -1;
        }

        private int ResolveBombElement(Monster monster, int summonSpellId)
        {
            var spellIds = monster?.Spells != null
                ? monster.Spells.Where(x => x != null).Select(x => x.Id).Distinct().ToArray()
                : new int[0];

            // Most reliable path: the bomb monster already carries its explosion/wall spells.
            if (spellIds.Contains(2829) || spellIds.Contains(2845))
                return 3; // air

            if (spellIds.Contains(2833) || spellIds.Contains(2830))
                return 4; // water

            if (spellIds.Contains(2825) || spellIds.Contains(2822))
                return 2; // fire

            // SaveKrosmoz/Stump 2.3.x rogue summon spells:
            // 2796 = Tornabombe (air), 2797 = Bombe à Eau, 2808 = Explobombe (fire)
            // Keep a legacy fallback for forks that use 2798 for air.
            switch (summonSpellId)
            {
                case 2796:
                case 2798:
                    return 3; // air

                case 2797:
                    return 4; // water

                case 2808:
                    return 2; // fire
            }

            // Monster template fallback. SaveKrosmoz uses 3112/3113/3114.
            switch (monster != null ? monster.Id : 0)
            {
                case 3113:
                case 2969:
                    return 3; // air

                case 3114:
                case 2970:
                    return 4; // water

                case 3112:
                    return 2; // fire

                default:
                    return 2; // fire
            }
        }
    }
}
