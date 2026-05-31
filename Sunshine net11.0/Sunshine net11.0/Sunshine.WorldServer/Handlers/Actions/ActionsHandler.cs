using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Actors.AI;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors.Look;
using Sunshine.WorldServer.Game.Fights.Buffs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Handlers.Actions
{
    public class ActionsHandler : WorldPacketHandler
    {
        [WorldHandler(957)]
        public static void HandleGameActionAcknowledgementMessage(WorldClient client, GameActionAcknowledgementMessage message)
        {

        }

        public static void SendGameActionFightPointsVariationMessage(List<WorldClient> clients, ActionsEnum action, FightActor source, FightActor target, short delta)
        {
            CharacterFighter characterTarget = target as CharacterFighter;

            bool skipSelfClient = source == target
                && characterTarget != null
                && characterTarget.Client != null
                && (action == ActionsEnum.ACTION_CHARACTER_ACTION_POINTS_USE
                    || action == ActionsEnum.ACTION_CHARACTER_MOVEMENT_POINTS_USE);

            foreach (var client in clients)
            {
                if (skipSelfClient && client == characterTarget.Client)
                    continue;

                client.Send(new GameActionFightPointsVariationMessage((short)action, source.Id, target.Id, delta));
            }
        }

        public static void SendGameActionFightLifePointsVariationMessage(List<WorldClient> clients, FightActor source, FightActor target, int delta)
        {
            if (clients == null || target == null)
                return;

            int sourceId = source != null ? source.Id : target.Id;
            int targetId = target.Id;

            if (delta == 0)
            {
                clients.Where(x => x != null).ToList().ForEach(x =>
                    x.Send(new GameActionFightLifePointsVariationMessage((short)ActionsEnum.ACTION_CHARACTER_LIFE_POINTS_LOST, sourceId, targetId, 0)));
                return;
            }

            int remaining = delta;
            while (remaining != 0)
            {
                short chunk;
                if (remaining > short.MaxValue)
                    chunk = short.MaxValue;
                else if (remaining < short.MinValue)
                    chunk = short.MinValue;
                else
                    chunk = (short)remaining;

                var action = chunk > 0
                    ? ActionsEnum.ACTION_CHARACTER_LIFE_POINTS_WIN
                    : ActionsEnum.ACTION_CHARACTER_LIFE_POINTS_LOST;

                clients.Where(x => x != null).ToList().ForEach(x =>
                    x.Send(new GameActionFightLifePointsVariationMessage((short)action, sourceId, targetId, chunk)));

                remaining -= chunk;
            }
        }

        public static void SendGameActionFightReduceDamagesMessage(List<WorldClient> clients, FightActor source, FightActor target, int amount)
        {
            clients.ForEach(x => x.Send(new GameActionFightReduceDamagesMessage(105, source.Id, target.Id, amount)));
        }

        public static void SendGameActionFightSpellCooldownVariationMessage(List<WorldClient> clients, FightActor source, FightActor target, ushort spell, short nbrTurn)
        {
            clients.ForEach(x => x.Send(new GameActionFightSpellCooldownVariationMessage(1045, source.Id, target.Id, spell, nbrTurn)));
        }

        public static void SendGameActionFightShieldPointsVariationMessage(List<WorldClient> clients, FightActor source, FightActor target, short delta)
        {
            clients.ForEach(x => x.Send(new GameActionFightShieldPointsVariationMessage(1041, source.Id, target.Id, delta)));
        }

        public static void SendGameActionFightDispellEffectMessage(List<WorldClient> clients, FightActor source, FightActor target, Buff buff)
        {
            clients.ForEach(x => x.Send(new GameActionFightDispellEffectMessage(514, source.Id, target.Id, buff.Id)));
        }

        public static void SendGameActionFightDeathMessage(List<WorldClient> clients, FightActor fighter, FightActor target)
        {
            clients.ForEach(x => x.Send(new GameActionFightDeathMessage(103, fighter.Id, target.Id)));
        }

        public static void SendGameActionFightDeathMessage(List<WorldClient> clients, FightActor fighter)
        {
            clients.ForEach(x => x.Send(new GameActionFightDeathMessage(103, fighter.Id, fighter.Id)));
        }

        public static void SendGameActionFightKillMessage(List<WorldClient> clients, FightActor fighter)
        {
            clients.ForEach(x => x.Send(new GameActionFightKillMessage(141, fighter.Id, fighter.Id)));
        }

        public static void SendGameActionFightChangeLookMessage(List<WorldClient> clients, FightActor source, FightActor target, ActorLook look)
        {
            clients.ForEach(x => x.Send(new GameActionFightChangeLookMessage(149, source.Id, target.Id, look.GetEntityLook())));
        }

        public static void SendGameActionFightExchangePositionsMessage(List<WorldClient> clients, FightActor caster, FightActor target)
        {
            clients.ForEach(x => x.Send(new GameActionFightExchangePositionsMessage(8, caster.Id, target.Id, caster.Position.Cell, target.Position.Cell)));
        }

        public static void SendGameActionFightReflectSpellMessage(List<WorldClient> clients, FightActor source, FightActor target)
        {
            clients.ForEach(x => x.Send(new GameActionFightReflectSpellMessage(106, source.Id, target.Id)));
        }

        public static void SendGameActionFightTeleportOnSameMapMessage(List<WorldClient> clients, FightActor source, FightActor target, short destination)
        {
            clients.ForEach(x => x.Send(new GameActionFightTeleportOnSameMapMessage(4, source.Id, target.Id, destination)));
        }

        public static void SendGameActionFightSlideMessage(List<WorldClient> clients, FightActor source, FightActor target, short startCell, short endCell)
        {
            clients.ForEach(x => x.Send(new GameActionFightSlideMessage(5, source.Id, target.Id, startCell, endCell)));
        }

        public static void SendSequenceStartMessage(List<WorldClient> clients, FightActor actor, SequenceTypeEnum sequenceType)
        {
            if (clients == null || actor == null)
                return;

            clients.Where(x => x != null).ToList()
                .ForEach(x => x.Send(new SequenceStartMessage((sbyte)sequenceType, actor.Id)));
        }

        public static void SendSequenceEndMessage(List<WorldClient> clients, FightActor actor, SequenceTypeEnum sequenceType, ActionsEnum actionType)
        {
            if (clients == null || actor == null)
                return;

            clients.Where(x => x != null).ToList()
                .ForEach(x => x.Send(new SequenceEndMessage((short)actionType, actor.Id, (sbyte)sequenceType)));
        }

        public static void SendGameActionFightDodgePointLossMessage(List<WorldClient> clients, ActionsEnum action, FightActor source, FightActor target, short amount)
        {
            clients.ForEach(x => x.Send(new GameActionFightDodgePointLossMessage((short)action, source.Id, target.Id, amount)));
        }

        public static void SendGameActionFightInvisibilityMessage(WorldClient client, FightActor source, FightActor target, GameActionFightInvisibilityStateEnum state)
        {
            client.Send(new GameActionFightInvisibilityMessage(150, source.Id, target.Id, (sbyte)state));
        }

        public static void SendGameActionFightSummonMessage(List<WorldClient> clients, ISummoned summon)
        {
            clients.ForEach(x => x.Send(new GameActionFightSummonMessage(181, summon.Summoner.Id, (summon as AIFighter).GetGameFightFighterInformations(x))));
        }

        public static void SendGameActionFightReviveMessage(List<WorldClient> clients, FightActor caster, FightActor actor)
        {
            if (clients == null || caster == null || actor == null)
                return;

            clients.ForEach(x => x.Send(new GameActionFightSummonMessage((short)ActionsEnum.ACTION_CHARACTER_SUMMON_DEAD_ALLY_IN_FIGHT, caster.Id, actor.GetGameFightFighterInformations(x))));
        }

        public static void SendGameActionFightCloseCombatMessage(WorldClient client, FightActor source, short destinationCellId, FightSpellCastCriticalEnum castCritical, bool silentCast, int weaponGenericId)
        {
            var action = ActionsEnum.ACTION_FIGHT_CLOSE_COMBAT;
            switch (castCritical)
            {
                case FightSpellCastCriticalEnum.CRITICAL_FAIL:
                    action = ActionsEnum.ACTION_FIGHT_CLOSE_COMBAT_CRITICAL_MISS;
                    break;
                case FightSpellCastCriticalEnum.CRITICAL_HIT:
                    action = ActionsEnum.ACTION_FIGHT_CLOSE_COMBAT_CRITICAL_HIT;
                    break;
            }

            client.Send(new GameActionFightCloseCombatMessage((short)action, source.Id, destinationCellId, (sbyte)castCritical, silentCast, weaponGenericId));
        }

        public static void SendGameActionFightCloseCombatMessage(List<WorldClient> clients, FightActor source, short destinationCellId, FightSpellCastCriticalEnum castCritical, bool silentCast, int weaponGenericId)
        {
            clients.ForEach(x => SendGameActionFightCloseCombatMessage(x, source, destinationCellId, castCritical, silentCast, weaponGenericId));
        }

        public static void SendGameActionFightReflectDamagesMessage(List<WorldClient> clients, FightActor source, FightActor target, int amount)
        {
            clients.ForEach(x => x.Send(new GameActionFightReflectDamagesMessage(107, source.Id, target.Id, amount)));
        }

        public static void SendGameActionFightCarryCharacterMessage(List<WorldClient> clients, FightActor source, FightActor target)
        {
            clients.ForEach(x => x.Send(new GameActionFightCarryCharacterMessage(50, source.Id, target.Id, target.Position.Cell)));
        }

        public static void SendGameActionFightThrowCharacterMessage(List<WorldClient> clients, FightActor source, FightActor target, short destination)
        {
            clients.ForEach(x => x.Send(new GameActionFightThrowCharacterMessage(51, source.Id, target.Id, destination)));
        }

        public static void SendGameActionFightDropCharacterMessage(List<WorldClient> clients, FightActor source, FightActor target, short destination)
        {
            clients.ForEach(x => x.Send(new GameActionFightDropCharacterMessage(51, source.Id, target.Id, destination)));
        }
    }
}
