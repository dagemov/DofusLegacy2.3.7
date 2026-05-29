using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors.AI;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Bombs;
using Sunshine.WorldServer.Handlers.Actions;
using Sunshine.WorldServer.Handlers.Context;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Effects.Spells.Moves
{
    internal static class ForcedMovementHelper
    {
        public static List<WorldClient> GetSlideClients(FightActor actor)
        {
            if (actor?.Fight?.Clients == null)
                return new List<WorldClient>();

            return actor.Fight.Clients
                .Where(x => x != null && x.Character != null && actor.IsVisibleFor(x.Character))
                .ToList();
        }

        public static List<WorldClient> SendSlide(FightActor source, FightActor target, short startCell, short endCell)
        {
            if (source == null || target?.Fight?.Clients == null)
                return new List<WorldClient>();

            var slideClients = GetSlideClients(target);
            ActionsHandler.SendGameActionFightSlideMessage(slideClients, source, target, startCell, endCell);
            return slideClients;
        }

        public static void RefreshNonSlideClients(FightActor target, IEnumerable<WorldClient> slideClients)
        {
            if (target?.Fight?.Clients == null)
                return;

            var visibleClients = slideClients != null
                ? new HashSet<WorldClient>(slideClients.Where(x => x != null))
                : new HashSet<WorldClient>();

            var otherClients = target.Fight.Clients
                .Where(x => x != null && !visibleClients.Contains(x))
                .ToList();

            if (otherClients.Count <= 0)
                return;

            foreach (var client in otherClients)
            {
                if (client?.Character != null && target.IsVisibleFor(client.Character))
                    ContextHandler.SendGameFightShowFighterMessage(client, target);
            }

            ContextHandler.SendGameEntitiesDispositionMessage(otherClients, new[] { target });
        }

        public static void RefreshControlledTargetClient(FightActor source, FightActor target, IEnumerable<WorldClient> slideClients)
        {
            if (target?.Fight?.Clients == null || target is not CharacterFighter characterTarget || characterTarget.Client == null)
                return;

            if (target.Fight.FighterPlaying is not AIFighter)
                return;

            var client = characterTarget.Client;
            if (slideClients != null && !slideClients.Any(x => x == client))
                return;

            // The controlled client does not always reconcile its own fighter position from a slide alone,
            // especially when the move is triggered by a monster. Send a lightweight disposition refresh
            // only to the owner to lock the final cell without forcing a global fight synchronize.
            ContextHandler.SendGameEntitiesDispositionMessage(new List<WorldClient> { client }, new[] { target });
        }

        public static void RefreshAfterForcedMove(FightActor source, FightActor target, IEnumerable<WorldClient> slideClients)
        {
            RefreshNonSlideClients(target, slideClients);
            RefreshControlledTargetClient(source, target, slideClients);
        }

        public static void DropCarriedActorIfNeeded(FightActor actor)
        {
            if (actor == null || !actor.IsCarrying || actor.Carrying == null || actor.Fight == null)
                return;

            var carried = actor.Carrying;
            ActionsHandler.SendGameActionFightDropCharacterMessage(actor.Fight.Clients, actor, carried, actor.Position.Cell);
            carried.Position.Cell = actor.Position.Cell;
            actor.BreakCarryLink(false);

            var otherClients = actor.Fight.Clients
                .Where(x => x != null)
                .ToList();

            if (otherClients.Count > 0)
                ContextHandler.SendGameEntitiesDispositionMessage(otherClients, new[] { actor, carried });

            if (carried is BombFighter movedBomb)
                BombManager.Instance.CheckWalls(actor.Fight, movedBomb.Summoner);
        }
    }
}
