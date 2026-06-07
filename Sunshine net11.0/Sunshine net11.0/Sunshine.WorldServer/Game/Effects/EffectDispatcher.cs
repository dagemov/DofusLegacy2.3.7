using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Game.Effects.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Effects.Items;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Fights.Telemetry;
using System.Diagnostics;

namespace Sunshine.WorldServer.Game.Effects
{
    public static class EffectDispatcher
    {
        public static void Dispatch(FightActor caster, Spell spell, Effect effect, short cell, short trapCell = -1, 
            ObjectPosition firstPosition = null, int countPushed = 0)
        {
            if (caster == null || caster.Fight == null || effect == null)
                return;

            var effectStopwatch = Stopwatch.StartNew();
            try
            {
                if (EffectManager.Instance.SpellEffects.ContainsKey(effect.Id))
                {
                    SpellEffectHandler spellEffect = EffectManager.Instance.SpellEffects[effect.Id]();
                    var affectedActors = EffectManager.Instance.GetAffectedActors(caster, effect, cell);
                    FightCombatLogger.LogEffectDispatch(caster.Fight, caster, spell, effect);
                    spellEffect.Initialize(new List<object> { effect.Id, effect.DiceNum, effect.DiceFace, effect.Value,
                                                              effect.Delay, effect.Duration, effect.Target, cell,
                                                              affectedActors, caster, spell, effect, trapCell, firstPosition, countPushed});
                    CombatTelemetry.LogSpellEvent(
                        "EffectResolved",
                        caster.Fight,
                        caster,
                        spell?.Id,
                        spell?.Level,
                        effectIds: new[] { effect.Id },
                        result: "OK",
                        durationMs: effectStopwatch.ElapsedMilliseconds);
                }
                else
                {
                    Logs.Logger.WriteError($"Cannot dispatch the effect {effect.Id} on spell {(spell != null ? spell.Id : 0)} from fighter {(caster != null ? caster.Id : 0)} !");
                    CombatTelemetry.LogSpellEvent(
                        "EffectFailed",
                        caster.Fight,
                        caster,
                        spell?.Id,
                        spell?.Level,
                        effectIds: new[] { effect.Id },
                        result: "HandlerMissing",
                        durationMs: effectStopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                Logs.Logger.WriteError($"Failed to dispatch effect {effect.Id} on spell {(spell != null ? spell.Id : 0)} from fighter {(caster != null ? caster.Id : 0)}: {ex}");
                CombatTelemetry.LogSpellEvent(
                    "EffectFailed",
                    caster.Fight,
                    caster,
                    spell?.Id,
                    spell?.Level,
                    effectIds: new[] { effect.Id },
                    result: "Exception",
                    error: ex.Message,
                    durationMs: effectStopwatch.ElapsedMilliseconds);
            }
        }
    }
}
