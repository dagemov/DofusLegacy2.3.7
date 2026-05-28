using Sunshine.Logs;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Effects;
using Sunshine.WorldServer.Game.Effects.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sunshine.WorldServer.Game.Spells.Casts
{
    public sealed class SpellCastManager : Singleton<SpellCastManager>
    {
        private readonly Dictionary<int, Type> m_handlers = new Dictionary<int, Type>();

        public SpellCastManager()
        {
            LoadHandlers();
        }

        private void LoadHandlers()
        {
            var assembly = Assembly.GetExecutingAssembly();

            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || !typeof(SpellCastHandler).IsAssignableFrom(type))
                    continue;

                var attribute = type.GetCustomAttributes(typeof(SpellCastHandlerAttribute), false)
                    .OfType<SpellCastHandlerAttribute>()
                    .FirstOrDefault();

                if (attribute == null)
                    continue;

                m_handlers[attribute.SpellId] = type;
            }
        }

        public SpellCastHandler CreateHandler(FightActor caster, Spell spell, short targetedCell, bool critical, IEnumerable<Effect> effects)
        {
            if (spell == null || !m_handlers.TryGetValue(spell.Id, out var handlerType))
                return null;

            try
            {
                var handler = (SpellCastHandler)Activator.CreateInstance(handlerType, caster, spell, targetedCell, critical);
                var effectHandlers = new List<SpellEffectHandler>();

                foreach (var effect in effects ?? Enumerable.Empty<Effect>())
                {
                    if (!EffectManager.Instance.SpellEffects.TryGetValue(effect.Id, out var factory))
                        continue;

                    var spellEffectHandler = factory();
                    spellEffectHandler.Prepare(new List<object>
                    {
                        effect.Id,
                        effect.DiceNum,
                        effect.DiceFace,
                        effect.Value,
                        effect.Delay,
                        effect.Duration,
                        effect.Target,
                        targetedCell,
                        EffectManager.Instance.GetAffectedActors(caster, effect, targetedCell),
                        caster,
                        spell,
                        effect,
                        (short)-1,
                        null,
                        0
                    });
                    effectHandlers.Add(spellEffectHandler);
                }

                handler.Handlers = effectHandlers.ToArray();
                handler.Initialize();
                return handler;
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Unable to create custom cast handler for spell {spell.Id}: {ex.Message}");
                return null;
            }
        }
    }
}
