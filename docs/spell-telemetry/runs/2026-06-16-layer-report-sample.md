# Combat Telemetry Analysis Report

Generated at: `2026-06-16 09:55:21 -04:00`
Input directory: `C:\Users\Hombr\source\repos\DofusLegacy2.3.7\docs\spell-telemetry\examples`
Total log files: `1`
Total FIGHT-PERF events: `0`
Total FIGHT-TURN events: `0`
Total distinct session fights: `0`

## Visible Turn-Latency Follow-up

The earlier telemetry pass measured internal combat methods such as AI, spell casting, handlers, and cleanup. This updated report keeps that data and adds `FIGHT-TURN` reconstruction so we can separate inner-method cost from player-visible turn waits between `AIEnd`, `EndTurn`, `ReadyChecker`, and the next visible turn.
Phase 2 extends that again with transition-specific checkpoints such as `EndTurnRequested`, `EndTurnBegin`, `EndTurnCompleted`, `EndTurnTimerDispose`, `NextTurnRequested`, and `SequencesClearedBeforeNewTurn`, which lets us distinguish a slow AI from a turn that actually stalls after `EndTurn`.

## Files Analyzed

- `sample-spell-effects.jsonl`: `perf=0 turn=0`

## Summary By Phase

| Phase | Count | AvgMs | MaxMs | P50 | P95 | P99 | Slow | Errors | AvgFanOut | MaxFanOut |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |

## Fine-Grained Profiling Summary

No fine-grained `Brain.*`, `CastSpell.*`, or `ApplyHandlers.*` events were found in this capture set.

## Top 10 Slowest Events

| Rank | ElapsedMs | Phase | FightId | MonsterId | SpellId | FanOut | Status | Source | Detail |
| --- | ---: | --- | ---: | ---: | ---: | ---: | --- | --- | --- |

## Worst Monsters By AI

No monster AI events with `monsterId > 0` were found.

## Worst Spells

No events exposing `spellId` were found.

## Worst Handler Types

No handler-level `ApplyHandlers.Handler` events were found.

## Worst Session Fights

| Session Fight | FightId | Events | Total Observed Ms | Max Event Ms | Worst Phase | Slow | Errors | Max FanOut |
| --- | ---: | ---: | ---: | ---: | --- | ---: | ---: | ---: |

## Fan-out Correlation

| Cohort | Events With FanOut | Avg FanOut | P95 FanOut | Max FanOut | Avg ElapsedMs |
| --- | ---: | ---: | ---: | ---: | ---: |
| Slow events | 0 | - | - | - | - |
| Non-slow events | 0 | - | - | - | - |

No events with `observedMessageFanOut` were captured in this sample.

## Turn Latency Analysis

No `FIGHT-TURN` completion events were found in this sample. This log set predates turn-latency telemetry, so it cannot explain the visible 30-second waits yet.

## Errors Detected

No `status=error` or `exceptionType=` entries were found.

## Conclusions

- No `status=error` combat telemetry entries were found in this sample.
- This sample contains no `FIGHT-TURN` lifecycle data yet, so it still cannot explain the user-visible 30-second waits between AI completion and the next visible turn transition.

## Recommended Next Phase

- Keep `feature/combat-telemetry-phase1` non-invasive and use this parser as the baseline before any combat optimization.
- Lower `FightTelemetrySlowThresholdMs` for the next capture window or add per-turn aggregate timings, because user-visible latency can still exist even when each single event stays under `50 ms`.
- Do not start with pathfinding optimization yet; `PathFinder.Resolve` stays inexpensive in this sample.
- Do not start with broadcast refactors; the current data points more strongly to AI, pathfinding, spell handling, or cleanup.
- Capture a fresh combat sample with `FIGHT-TURN` enabled against Dark Vlad, Edad, Nomekop, Pandora, Minotoror, and a control mob, because the visible 30-second waits likely live in turn transition rather than the already-measured inner methods.
