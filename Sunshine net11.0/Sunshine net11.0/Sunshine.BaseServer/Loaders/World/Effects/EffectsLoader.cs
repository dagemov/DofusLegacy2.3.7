using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Effects;
using Sunshine.WorldServer.Game.Effects.Spells;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Sunshine.BaseServer.Loaders.World.Effects
{
    public static class EffectsLoader
    {
        public static void Initialize()
        {
            Logs.Logger.Write("[ World ] Loading Effects...");

            EffectManager.Instance.SpellEffects.Clear();

            var effectTypes = Assembly.GetAssembly(typeof(SpellEffectHandler))
                .GetTypes()
                .Where(x => x.IsClass && !x.IsAbstract && x.BaseType != null && x.BaseType.Name == nameof(SpellEffectHandler));

            foreach (var effectType in effectTypes)
            {
                var ctor = effectType.GetConstructor(Type.EmptyTypes);
                if (ctor == null)
                    continue;

                var handlers = effectType.GetCustomAttributes(typeof(EffectHandler), false).OfType<EffectHandler>().ToArray();
                if (handlers.Length == 0)
                    continue;

                var factory = Expression.Lambda<Func<SpellEffectHandler>>(Expression.New(ctor)).Compile();

                foreach (var handler in handlers)
                {
                    if (EffectManager.Instance.SpellEffects.ContainsKey(handler.Effect))
                    {
                        continue;
                    }

                    EffectManager.Instance.SpellEffects.Add(handler.Effect, factory);
                }
            }

            Logs.Logger.Write($"[ World ] {EffectManager.Instance.SpellEffects.Count} Effects Loaded");
        }
    }
}
