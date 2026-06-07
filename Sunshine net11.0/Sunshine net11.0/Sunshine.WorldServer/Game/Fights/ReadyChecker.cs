using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Telemetry;
using Sunshine.WorldServer.Handlers.Context;

namespace Sunshine.WorldServer.Game.Fights
{
    public sealed class ReadyChecker
    {
        private readonly Action _success;
        private readonly Action<CharacterFighter[]> _failure;
        private readonly ConcurrentDictionary<int, CharacterFighter> _laggers;
        private readonly object _stateLock;
        private Timer _timer;
        private Fight _fight;
        private int _fightId;
        private int _round;
        private int _turnOwnerId;
        private string _turnOwnerName;
        private string _turnOwnerType;
        private long _startedAtUtcTicks;
        private volatile bool _cancelled;
        private int _completionStarted;

        public bool Started => _timer != null;

        public ReadyChecker(Action success, Action<CharacterFighter[]> failure)
        {
            _success = success;
            _failure = failure;
            _laggers = new ConcurrentDictionary<int, CharacterFighter>();
            _stateLock = new object();
        }

        public void Cancel()
        {
            lock (_stateLock)
            {
                _cancelled = true;
                Interlocked.Exchange(ref _completionStarted, 1);
                StopUnsafe();
                _laggers.Clear();
                _fight = null;
                _fightId = default;
                _round = default;
                _turnOwnerId = default;
                _turnOwnerName = string.Empty;
                _turnOwnerType = string.Empty;
                _startedAtUtcTicks = default;
            }
        }

        public void Start(FightActor turnOwner, CharacterFighter[] fighters)
        {
            if (turnOwner?.Fight == null)
                return;

            var fight = turnOwner.Fight;
            var turnOwnerType = CombatTelemetry.ResolveActorType(turnOwner);
            var turnOwnerName = CombatTelemetry.ResolveActorName(turnOwner);

            lock (_stateLock)
            {
                if (Started)
                    return;

                _cancelled = false;
                Interlocked.Exchange(ref _completionStarted, 0);
                _fight = fight;
                _fightId = fight.Id;
                _round = fight.TimeLine?.RoundNumber ?? 0;
                _turnOwnerId = turnOwner.Id;
                _turnOwnerName = turnOwnerName;
                _turnOwnerType = turnOwnerType;
                _startedAtUtcTicks = DateTime.UtcNow.Ticks;
                _laggers.Clear();

                foreach (var fighter in fighters)
                {
                    if (fighter != null)
                        _laggers.TryAdd(fighter.Id, fighter);
                }

                var timeout = CombatReadyCheckerSettings.TimeoutMs;
                _timer = new Timer(OnTimerElapsed, null, timeout, Timeout.Infinite);
            }

            CombatTelemetry.LogReadyCheckerEvent(
                "ReadyCheckerStarted",
                fight,
                turnOwner,
                reason: $"waiters={fighters.Length} timeoutMs={CombatReadyCheckerSettings.TimeoutMs}",
                waiters: fighters);

            if (fighters.Length == 0)
            {
                Complete(success: true, reason: "EMPTY");
                return;
            }

            foreach (var fighter in fighters)
            {
                if (fighter?.Character?.Client != null)
                    ContextHandler.SendGameFightTurnReadyRequestMessage(fighter.Character.Client, turnOwner);
            }
        }

        public void ToggleReady(CharacterFighter fighter)
        {
            if (!Started || fighter == null)
                return;

            if (!_laggers.TryRemove(fighter.Id, out _))
                return;

            var remaining = _laggers.Values.ToArray();
            var elapsedMs = ElapsedMs();

            CombatTelemetry.LogReadyCheckerEvent(
                "ReadyCheckerAck",
                _fight,
                _fight?.FighterPlaying,
                actorOverride: fighter,
                elapsedMs: elapsedMs,
                reason: $"remaining={remaining.Length}",
                waiters: remaining);

            if (remaining.Length == 0)
                Complete(success: true, reason: "ACK");
        }

        private void OnTimerElapsed(object state)
        {
            var laggers = _laggers.Values.ToArray();
            CombatTelemetry.LogReadyCheckerEvent(
                "ReadyCheckerTimeout",
                _fight,
                _fight?.FighterPlaying,
                elapsedMs: ElapsedMs(),
                reason: $"laggers={laggers.Length}",
                waiters: laggers);

            Complete(success: false, reason: "TIMEOUT", laggers);
        }

        private void Complete(bool success, string reason, CharacterFighter[] laggers = null)
        {
            if (!TryPrepareCompletion(out var fight, out laggers))
                return;

            if (success)
                _success();
            else
                _failure(laggers ?? Array.Empty<CharacterFighter>());
        }

        private bool TryPrepareCompletion(out Fight fight, out CharacterFighter[] laggers)
        {
            fight = null;
            laggers = Array.Empty<CharacterFighter>();

            lock (_stateLock)
            {
                if (_cancelled || Interlocked.CompareExchange(ref _completionStarted, 1, 0) != 0)
                    return false;

                StopUnsafe();
                if (_cancelled)
                    return false;

                laggers = _laggers.Values.ToArray();
                fight = _fight;
                return fight != null;
            }
        }

        private long ElapsedMs()
        {
            if (_startedAtUtcTicks == 0)
                return 0;

            var elapsed = DateTime.UtcNow.Ticks - _startedAtUtcTicks;
            return elapsed > 0 ? elapsed / TimeSpan.TicksPerMillisecond : 0;
        }

        private void StopUnsafe()
        {
            _timer?.Dispose();
            _timer = null;
        }
    }
}
