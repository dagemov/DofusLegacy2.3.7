using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors.Monsters;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.WorldServer.Game.Fights.Diagnostics;
using Sunshine.WorldServer.Game.Fights.Buffs;
using Sunshine.WorldServer.Game.Fights.Teams;
using Sunshine.WorldServer.Game.Fights.Triggers;
using Sunshine.WorldServer.Game.Fights.Types;
using Sunshine.WorldServer.Game.Maps.Pathfinding;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Handlers.Basic;
using Sunshine.WorldServer.Handlers.Characters;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using Sunshine.WorldServer.Handlers.Characters.Shorcuts;
using Sunshine.WorldServer.Handlers.Context;
using Sunshine.WorldServer.Handlers.Context.Roleplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Handlers.Context
{
    public class ContextHandler : WorldPacketHandler
    {
        [WorldHandler(250)]
        public static void HandleGameContextCreateRequestMessage(WorldClient client, GameContextCreateRequestMessage message)
        {
            ContextHandler.SendGameContextDestroyMessage(client);
            ContextHandler.SendGameContextCreateMessage(client, GameContextEnum.ROLE_PLAY);
            ContextRoleplayHandler.SendCurrentMapMessage(client, client.Character.Map.Id);
        }

        [WorldHandler(255)]
        public static void HandleGameContextQuitMessage(WorldClient client, GameContextQuitMessage message)
        {
            if (client.Character.IsInFight())
                client.Character.LeaveFight();
        }


        [WorldHandler(GameFightLeaveMessage.Id)]
        public static void HandleGameFightLeaveMessage(WorldClient client, GameFightLeaveMessage message)
        {
            if (client?.Character?.IsInFight() == true)
                client.Character.LeaveFight();
        }

        [WorldHandler(950)]
        public static void HandleGameMapMovementRequestMessage(WorldClient client, GameMapMovementRequestMessage message)
        {
            Path path = Path.BuildFromCompressedPath(client.Character.Map, message.keyMovements);

            if (client?.Character?.IsInFight() == true && client.Character.GodMode && client.Character.Fighter != null)
            {
                var fighter = client.Character.Fighter.GetCurrentPlayableFighter();
                short beforeUsedMp = fighter != null ? (short)fighter.Stats.MP.Used : (short)0;
                client.Character.StartMove(path);
                short spent = fighter != null ? (short)(fighter.Stats.MP.Used - beforeUsedMp) : (short)0;
                if (fighter != null && spent > 0)
                    fighter.RegainMP(spent);
                return;
            }

            client.Character.StartMove(path);
        }

        [WorldHandler(945)]
        public static void HandleGameMapChangeOrientationRequestMessage(WorldClient client, GameMapChangeOrientationRequestMessage message)
        {
            client.Character.Direction = (int)message.direction;
            ContextHandler.SendGameMapChangeOrientationMessage(client);
        }

        [WorldHandler(952)]
        public static void HandleGameMapMovementConfirmMessage(WorldClient client, GameMapMovementConfirmMessage message)
        {
            client.Character.StopMove(true);
        }

        [WorldHandler(953)]
        public static void HandleGameMapMovementCancelMessage(WorldClient client, GameMapMovementCancelMessage message)
        {
            client.Character.StopMove(false);
        }

        [WorldHandler(701)]
        public static void HandleGameFightJoinRequestMessage(WorldClient client, GameFightJoinRequestMessage message)
        {
            if (client?.Character == null)
                return;

            if (client.Character.IsInFight() || client.Character.Fighter != null)
            {
                ContextHandler.SendChallengeFightJoinRefusedMessage(client, client.Character, FighterRefusedReasonEnum.IM_OCCUPIED);
                return;
            }

            Fight fight = FightManager.Instance.GetFight(message.fightId);

            if (fight == null)
            {
                ContextHandler.SendChallengeFightJoinRefusedMessage(client, client.Character, FighterRefusedReasonEnum.TOO_LATE);
                return;
            }
            else
            {
                if (fight.Map != client.Character.Map)
                {
                    ContextHandler.SendChallengeFightJoinRefusedMessage(client, client.Character, FighterRefusedReasonEnum.WRONG_MAP);
                }
                else
                {
                    FightActor fighter = fight.GetOneFighter(message.fighterId);
                    if (fighter != null)
                    {
                        if (fight.Type == FightTypeEnum.FIGHT_TYPE_PvT)
                        {
                            if (client.Character.Guild != null && (fight as FightPvT).TaxCollector.Guild.Id == client.Character.Guild.Id)
                                return;
                        }

                        bool joinAttackers = fighter.IsAttacker();
                        if (fight.Team.IsClosed(joinAttackers))
                        {
                            ContextHandler.SendChallengeFightJoinRefusedMessage(client, client.Character, FighterRefusedReasonEnum.INSUFFICIENT_RIGHTS);
                            return;
                        }

                        if (fight.Team.IsPartyOnly(joinAttackers))
                        {
                            var targetCharacter = fighter as CharacterFighter;
                            bool sameParty = targetCharacter != null &&
                                             targetCharacter.Character.Party != null &&
                                             client.Character.Party != null &&
                                             targetCharacter.Character.Party == client.Character.Party;
                            if (!sameParty)
                            {
                                ContextHandler.SendChallengeFightJoinRefusedMessage(client, client.Character, FighterRefusedReasonEnum.INSUFFICIENT_RIGHTS);
                                return;
                            }
                        }

                        client.Character.SetFight(fight.Type, fight);
                        if (fighter.IsAttacker())
                            fight.AddFighter(client.Character.Fighter = new CharacterFighter(client.Character), true);
                        else
                            fight.AddFighter(client.Character.Fighter = new CharacterFighter(client.Character));
                    }
                    else
                        ContextHandler.SendChallengeFightJoinRefusedMessage(client, client.Character, FighterRefusedReasonEnum.WRONG_MAP);
                }
            }
        }

        [WorldHandler(708)]
        public static void HandleGameFightReadyMessage(WorldClient client, GameFightReadyMessage message)
        {
            if (client.Character.IsInFight())
                client.Character.Fighter.SetReadyStatus(message.isReady);
        }

        [WorldHandler(716)]
        public static void HandleGameFightTurnReadyMessage(WorldClient client, GameFightTurnReadyMessage message)
        {
            if (client?.Character?.Fight == null)
                return;

            var fight = client.Character.Fight;
            var actor = client.Character.Fighter;
            Game.Fights.Telemetry.CombatTelemetry.LogTurnEvent(
                "GameFightTurnReadyMessageReceived",
                fight,
                actor,
                detail: $"characterId={client.Character.Id} sessionId={client.Account?.Id ?? 0}");
        }

        [WorldHandler(718)]
        public static void HandleGameFightTurnFinishMessage(WorldClient client, GameFightTurnFinishMessage message)
        {
            if (!client.Character.IsInFight() || client.Character.Fighter == null)
                return;

            if (client.Character.Fighter.IsSlaveTurn())
            {
                client.Character.Fighter.GetSlave()?.EndTurn();
                return;
            }

            if (client.Character.Fighter == client.Character.Fight.FighterPlaying)
                client.Character.Fighter.EndTurn();
        }

        [WorldHandler(GameFightOptionToggleMessage.Id)]
        public static void HandleGameFightOptionToggleMessage(WorldClient client, GameFightOptionToggleMessage message)
        {
            if (client?.Character?.Fight == null || client.Character.Fighter == null)
                return;

            var fight = client.Character.Fight;
            if (fight.State != FightStateEnum.Placement)
                return;

            var option = (FightOptionsEnum)message.option;
            bool isChallenger = client.Character.Fighter.IsAttacker();
            var teamMembers = isChallenger ? fight.Team.Attackers : fight.Team.Defenders;
            if (teamMembers.Count == 0 || teamMembers[0] != client.Character.Fighter)
                return;

            bool state = fight.Team.ToggleOption(isChallenger, option);
            SendGameFightOptionStateUpdateMessage(fight.Clients, fight.Team, option, state, isChallenger);
        }

        [WorldHandler(GameFightPlacementPositionRequestMessage.Id)]
        public static void HandleGameFightPlacementPositionRequestMessage(WorldClient client, GameFightPlacementPositionRequestMessage message)
        {
            if (client?.Character?.Fight == null || client.Character.Fighter == null)
                return;

            var fight = client.Character.Fight;
            if (fight.State != FightStateEnum.Placement)
                return;

            var fighter = client.Character.Fighter;
            fight.Map.EnsureFightCells();

            var allowedCells = fighter.IsAttacker() ? fight.Map.RedCells : fight.Map.BlueCells;
            if (allowedCells == null || !allowedCells.Contains(message.cellId) || !fight.IsCellFree(message.cellId))
                return;

            fighter.Position.Cell = message.cellId;
            SendGameEntitiesDispositionMessage(fight.Clients, new[] { fighter });
        }

        [WorldHandler(5611)]
        public static void HandleShowCellRequestMessage(WorldClient client, ShowCellRequestMessage message)
        {
            if (client.Character.IsInFight())
                client.Character.Fighter.GetCurrentPlayableFighter()?.ShowCell(message.cellId);
        }

        [WorldHandler(1005)]
        public static void HandleGameActionFightCastRequestMessage(WorldClient client, GameActionFightCastRequestMessage message)
        {
            if (client?.Character?.IsInFight() != true || client.Character.Fighter == null)
                return;

            var actingFighter = client.Character.Fighter.GetCurrentPlayableFighter();
            if (actingFighter == null)
                return;

            Spell spell = null;
            if (actingFighter is SlaveFighter slaveFighter)
            {
                if (message.spellId == 0)
                    return;

                spell = slaveFighter.GetSpell(message.spellId);
                if (spell == null)
                {
                    Logs.Logger.WriteError($"Controlled slave {slaveFighter.Id} cannot cast spell {message.spellId}.");
                    return;
                }
            }
            else if (message.spellId == 0)
            {
                spell = new Spell(0, 1);
            }
            else
            {
                var characterSpell = client.Character.Spells.GetSpell(message.spellId);
                if (characterSpell == null)
                {
                    Logs.Logger.WriteError($"Fighter {client.Character.Id} try to cast spell {message.spellId} !");
                    return;
                }

                List<Spell> spells;
                if (!SpellManager.Instance.Spells.TryGetValue(message.spellId, out spells) || spells == null || spells.Count < characterSpell.Level)
                {
                    Logs.Logger.WriteError($"Spell {message.spellId} is missing or incomplete for fighter {client.Character.Id}.");
                    return;
                }

                spell = spells[characterSpell.Level - 1];
            }

            actingFighter.CastSpell(spell, message.cellId);
        }

        public static void SendGameFightHumanReadyStateMessage(WorldClient client, FightActor actor)
        {
            client.Send(new GameFightHumanReadyStateMessage(actor.Id, actor.IsReady));
        }

        public static void SendGameContextDestroyMessage(WorldClient client)
        {
            client.Send(new GameContextDestroyMessage());
        }

        public static void SendGameContextCreateMessage(WorldClient client, GameContextEnum context)
        {
            client.Send(new GameContextCreateMessage((sbyte)context));
        }

        public static void SendGameContextRemoveElementMessage(WorldClient client, RolePlayActor actor)
        {
            client.Send(new GameContextRemoveElementMessage(actor.Id));
        }

        public static void SendGameMapChangeOrientationMessage(WorldClient client)
        {
            for (int i = 0; i < client.Character.Map.Clients.Count; i++)
                client.Character.Map.Clients[i].Send(new GameMapChangeOrientationMessage(new ActorOrientation(client.Character.Id, (sbyte)client.Character.Direction)));
        }

        public static void SendGameMapMovementMessage(List<WorldClient> clients, int actorId, IEnumerable<short> keymovements)
        {
            for (int i = 0; i < clients.Count; i++)
                clients[i].Send(new GameMapMovementMessage(keymovements, actorId));
        }

        public static void SendGameMapMovementMessage(WorldClient client, int actorId, IEnumerable<short> keymovements)
        {
            client.Send(new GameMapMovementMessage(keymovements, actorId));
        }

        public static void SendGameFightStartingMessage(WorldClient client, FightTypeEnum fightType)
        {
            client.Send(new GameFightStartingMessage((sbyte)fightType));
        }

        public static void SendGameFightJoinMessage(WorldClient client, Fight fight)
        {
            client.Send(new GameFightJoinMessage(true, true, false, fight.IsStarted(), fight.GetPlacementTimeLeft(client.Character.Fighter), (sbyte)fight.Type));
        }

        public static void GameFightStartingMessage(WorldClient client, FightTypeEnum fightType)
        {
            client.Send(new GameFightStartingMessage((sbyte)fightType));
        }

        public static void SendGameFightStartMessage(List<WorldClient> clients)
        {
            for (int i = 0; i < clients.Count; i++)
                clients[i].Send(new GameFightStartMessage());
        }

        public static void SendGameFightEndMessage(List<WorldClient> clients, Fight fight, IEnumerable<FightResultListEntry> results)
        {
            int duration = fight != null ? fight.GetFightDurationMilliseconds() : 0;
            for (int i = 0; i < clients.Count; i++)
                clients[i].Send(new GameFightEndMessage(duration, MonsterGroup.ForcedStarBonus, results));
        }

        public static void SendGameFightTurnListMessage(List<WorldClient> clients, Fight fight)
        {
            for (int i = 0; i < clients.Count; i++)
                clients[i].Send(new GameFightTurnListMessage(fight.GetAliveFightersIds(), fight.GetDeadFightersIds()));
        }

        public static void SendGameFightTurnStartMessage(List<WorldClient> clients, FightActor actor)
        {
            if (clients != null && clients.Count > 0 && actor?.Fight != null)
                FightCombatLogger.LogSocket(actor.Fight, nameof(GameFightTurnStartMessage), clients.Count);

            for (int i = 0; i < clients.Count; i++)
                clients[i].Send(new GameFightTurnStartMessage(actor.Id, 35000));
        }

        public static void SendGameFightTurnStartSlaveMessage(List<WorldClient> clients, SlaveFighter actor)
        {
            if (clients == null || actor == null)
                return;

            for (int i = 0; i < clients.Count; i++)
                clients[i].Send(new GameFightTurnStartSlaveMessage(actor.Id, 35000, actor.Summoner != null ? actor.Summoner.Id : 0));
        }

        public static void SendSlaveSwitchContextMessage(WorldClient client, SlaveFighter actor)
        {
            if (client == null || actor == null)
                return;

            client.Send(new SlaveSwitchContextMessage(
                actor.Summoner != null ? actor.Summoner.Id : 0,
                actor.Id,
                actor.GetSpellItems(),
                actor.GetSlaveCharacteristicsInformations()));
        }

        public static void SendGameFightTurnEndMessage(List<WorldClient> clients, FightActor actor)
        {
            for (int i = 0; i < clients.Count; i++)
                clients[i].Send(new GameFightTurnEndMessage(actor.Id));
        }

        public static void SendGameFightTurnReadyRequestMessage(List<WorldClient> clients, FightActor actor)
        {
            for (int i = 0; i < clients.Count; i++)
                clients[i].Send(new GameFightTurnReadyRequestMessage(actor.Id));
        }

        public static void SendChallengeFightJoinRefusedMessage(WorldClient client, Character character, FighterRefusedReasonEnum reason)
        {
            client.Send(new ChallengeFightJoinRefusedMessage(character.Id, (sbyte)reason));
        }

        public static void SendGameFightPlacementPossiblePositionsMessage(WorldClient client, Fight fight)
        {
            if (client == null || fight == null || fight.Map == null)
                return;

            fight.Map.EnsureFightCells();
            client.Send(new GameFightPlacementPossiblePositionsMessage(fight.Map.RedCells ?? new List<short>(), fight.Map.BlueCells ?? new List<short>(), (sbyte)(client.Character.Fighter != null && client.Character.Fighter.IsAttacker() ? 0 : 1)));
        }

        public static void SendGameRolePlayShowChallengeMessage(List<WorldClient> clients, Fight fight)
        {
            for (int i = 0; i < clients.Count; i++)
                clients[i].Send(new GameRolePlayShowChallengeMessage(fight.GetFightCommonInformations));
        }

        public static void GameRolePlayRemoveChallengeMessage(List<WorldClient> clients, Fight fight)
        {
            for (int i = 0; i < clients.Count; i++)
                clients[i].Send(new GameRolePlayRemoveChallengeMessage(fight.Id));
        }

        public static void SendGameFightShowFighterPreparationMessage(List<WorldClient> clients, IEnumerable<FightActor> actors)
        {
            foreach (var actor in actors)
                clients.ForEach(x => x.Send(new GameFightShowFighterMessage(actor.GetGameFightFighterPreparationInformations(x))));
        }

        public static void SendGameFightShowFighterMessage(List<WorldClient> clients, List<FightActor> actors)
        {
            foreach (var actor in actors)
                clients.ForEach(x => x.Send(new GameFightShowFighterMessage(actor.GetGameFightFighterInformations(x))));
        }

        public static void SendGameFightShowFighterMessage(WorldClient client, FightActor actor)
        {
            client.Send(new GameFightShowFighterMessage(actor.GetGameFightFighterInformations(client)));
        }

        public static void SendGameFightSynchronizeMessage(List<WorldClient> clients, IEnumerable<FightActor> actors)
        {
            var safeClients = clients != null ? clients.Where(x => x != null).ToList() : new List<WorldClient>();
            var safeActors = actors != null ? actors.Where(y => y != null && y.Position != null).ToList() : new List<FightActor>();

            safeClients.ForEach(x => x.Send(new GameFightSynchronizeMessage(safeActors.Select(y => y.GetGameFightFighterInformations(x)).ToArray())));
        }

        public static void SendGameFightSynchronizeMessage(List<WorldClient> clients, FightActor actor)
        {
            if (actor == null || actor.Position == null)
                return;

            var safeClients = clients != null ? clients.Where(x => x != null).ToList() : new List<WorldClient>();
            safeClients.ForEach(x => x.Send(new GameFightSynchronizeMessage(new List<GameFightFighterInformations> { actor.GetGameFightFighterInformations(x) })));
        }

        public static void SendGameEntitiesDispositionMessage(List<WorldClient> clients, IEnumerable<FightActor> actors)
        {
            var safeClients = clients != null ? clients.Where(x => x != null).ToList() : new List<WorldClient>();
            var safeActors = actors != null ? actors.Where(y => y != null && y.Position != null).ToList() : new List<FightActor>();

            safeClients.ForEach(x => x.Send(new GameEntitiesDispositionMessage(safeActors.Select(y => y.GetIdentifiedEntityDispositionInformations(x)).ToArray())));
        }

        public static void SendGameContextRefreshEntityLookMessage(List<WorldClient> clients, int actorId, EntityLook look)
        {
            clients.ForEach(x => x.Send(new GameContextRefreshEntityLookMessage(actorId, look)));
        }

        public static void SendGameFightUpdateTeamMessage(List<WorldClient> clients, FightTeam team)
        {
            clients.ForEach(x => x.Send(new GameFightUpdateTeamMessage((short)team.Fight.Id, team.GetFightTeamInformations())));
            clients.ForEach(x => x.Send(new GameFightUpdateTeamMessage((short)team.Fight.Id, team.GetFightTeamInformations(true))));
        }

        public static void SendGameFightOptionStateUpdateMessage(List<WorldClient> clients, FightTeam team, FightOptionsEnum option, bool state, bool challengerTeam)
        {
            var teamId = challengerTeam ? (sbyte)TeamEnum.TEAM_CHALLENGER : (sbyte)TeamEnum.TEAM_DEFENDER;
            clients.ForEach(x => x.Send(new GameFightOptionStateUpdateMessage((short)team.Fight.Id, teamId, (sbyte)option, state)));
        }

        public static void SendGameFightNewRoundMessage(List<WorldClient> clients, int roundNumber)
        {
            clients.ForEach(x => x.Send(new GameFightNewRoundMessage(roundNumber)));
        }

        public static void SendShowCellMessage(IEnumerable<WorldClient> clients, FightActor source, short cellId)
        {
            foreach (var client in clients)
                client.Send(new ShowCellMessage(source.Id, cellId));
        }

        public static void SendGameActionFightSpellCastMessage(List<WorldClient> clients, ActionsEnum actionId, FightActor caster, Spell spell,
            short cellId, FightSpellCastCriticalEnum critical, bool silentCast)
        {
            if (clients != null && clients.Count > 0 && caster?.Fight != null)
                FightCombatLogger.LogSocket(caster.Fight, nameof(GameActionFightSpellCastMessage), clients.Count);

            clients.ForEach(x => x.Send(new GameActionFightSpellCastMessage((short)actionId, caster.Id, cellId, (sbyte)critical, silentCast, (short)spell.Id, spell.Level)));
        }

        public static void SendGameActionFightSpellCastMessage(WorldClient client, ActionsEnum actionId, FightActor caster, Spell spell,
            short cellId, FightSpellCastCriticalEnum critical, bool silentCast)
        {
            client.Send(new GameActionFightSpellCastMessage((short)actionId, caster.Id, cellId, (sbyte)critical, silentCast, (short)spell.Id, spell.Level));
        }

        public static void SendGameActionFightDispellableEffectMessage(List<WorldClient> clients, Buff buff, bool update = false)
        {
            short actionId = update ? buff.GetUpdateActionId() : buff.GetActionId();
            clients.ForEach(x => x.Send(new GameActionFightDispellableEffectMessage(actionId, buff.Caster.Id, buff.GetAbstractFightDispellableEffect())));
        }

        public static void SendGameActionFightDispellSpellMessage(List<WorldClient> clients, short actionId,
            FightActor sourceId, FightActor targetId, ushort spellId)
        {
            clients.ForEach(x => x.Send(new GameActionFightDispellSpellMessage(actionId, sourceId.Id, targetId.Id, spellId)));
        }

        public static void SendGameActionFightMarkCellsMessage(List<WorldClient> clients, MarkTrigger trigger, bool visible = true)
        {
            ActionsEnum actionsEnum;

            if (trigger.Type == GameActionMarkTypeEnum.GLYPH)
                actionsEnum = ActionsEnum.ACTION_FIGHT_ADD_GLYPH_CASTING_SPELL;
            else
                // SaveKrosmoz/Stump 2.3.x sends WALL marks through the TRAP add action.
                // Keeping mark.Type = WALL while using ADD_TRAP is what the client expects
                // to render the roublard bomb walls with their colored cells.
                actionsEnum = ActionsEnum.ACTION_FIGHT_ADD_TRAP_CASTING_SPELL;
            clients.ForEach(x => x.Send(new GameActionFightMarkCellsMessage((short)actionsEnum, trigger.Caster.Id, visible ? trigger.GetGameActionMark() : trigger.GetHiddenGameActionMark())));
        }

        public static void SendGameActionFightMarkCellsMessage(WorldClient client, MarkTrigger trigger, bool visible = true)
        {
            ActionsEnum actionsEnum;

            if (trigger.Type == GameActionMarkTypeEnum.GLYPH)
                actionsEnum = ActionsEnum.ACTION_FIGHT_ADD_GLYPH_CASTING_SPELL;
            else
                // SaveKrosmoz/Stump 2.3.x sends WALL marks through the TRAP add action.
                // Keeping mark.Type = WALL while using ADD_TRAP is what the client expects
                // to render the roublard bomb walls with their colored cells.
                actionsEnum = ActionsEnum.ACTION_FIGHT_ADD_TRAP_CASTING_SPELL;
            client.Send(new GameActionFightMarkCellsMessage((short)actionsEnum, trigger.Caster.Id, visible ? trigger.GetGameActionMark() : trigger.GetHiddenGameActionMark()));
        }

        public static void SendGameActionFightUnmarkCellsMessage(List<WorldClient> clients, MarkTrigger trigger)
        {
            clients.ForEach(x => x.Send(new GameActionFightUnmarkCellsMessage(310, trigger.Caster.Id, trigger.Id)));
        }

        public static void SendGameActionFightTriggerGlyphTrapMessage(WorldClient client, MarkTrigger trigger, FightActor target, Spell triggeredSpell)
        {
            ActionsEnum actionsEnum = (trigger.Type == GameActionMarkTypeEnum.GLYPH) ? ActionsEnum.ACTION_FIGHT_TRIGGER_GLYPH : ActionsEnum.ACTION_FIGHT_TRIGGER_TRAP;
            client.Send(new GameActionFightTriggerGlyphTrapMessage((short)actionsEnum, trigger.Caster.Id, trigger.Id, target.Id, (short)triggeredSpell.Id));
        }

        public static void SendGameActionFightTriggerGlyphTrapMessage(List<WorldClient> clients, MarkTrigger trigger, FightActor target, Spell triggeredSpell)
        {
            ActionsEnum actionsEnum = (trigger.Type == GameActionMarkTypeEnum.GLYPH) ? ActionsEnum.ACTION_FIGHT_TRIGGER_GLYPH : ActionsEnum.ACTION_FIGHT_TRIGGER_TRAP;
            clients.ForEach(x => x.Send(new GameActionFightTriggerGlyphTrapMessage((short)actionsEnum, trigger.Caster.Id, trigger.Id, target.Id, (short)triggeredSpell.Id)));
        }
        [WorldHandler(GameContextReadyMessage.Id)]
        public static void HandleGameContextReadyMessage(WorldClient client, GameContextReadyMessage message)
        {
            if (client?.Character == null || client.Character.IsInFight() || client.Character.Map == null)
                return;

            ContextRoleplayHandler.SendMapComplementaryInformationsDataMessage(client);
            client.Send(new MapFightCountMessage((short)client.Character.Map.Fights.Count));
            SendGameMapChangeOrientationMessage(client);
        }
    }
}
