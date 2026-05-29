using Sunshine.Logs;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Effects.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Spells.Casts.Eca
{
    [SpellCastHandler(101)]
    public class RouletteHandler : SpellCastHandler
    {
        private static readonly Random s_random = new Random();

        private SpellEffectHandler _casterApBonusHandler;
        private SpellEffectHandler SelectedHandler;

        public RouletteHandler(FightActor caster, Spell spell, short targetedCell, bool critical)
            : base(caster, spell, targetedCell, critical)
        {
        }

        public override void Initialize()
        {
            _casterApBonusHandler = null;
            SelectedHandler = null;

            var availableHandlers = (Handlers ?? Array.Empty<SpellEffectHandler>())
                .Where(x => x != null && x.Effect != null)
                .ToArray();

            if (availableHandlers.Length == 0)
            {
                m_initialized = true;
                return;
            }

            _casterApBonusHandler = availableHandlers.FirstOrDefault(IsCasterApBonusEffect);

            var rouletteCandidates = availableHandlers
                .Where(x => !IsRouletteMetaEffect(x) && !ReferenceEquals(x, _casterApBonusHandler))
                .ToArray();

            if (rouletteCandidates.Length == 0)
                rouletteCandidates = availableHandlers.Where(x => !ReferenceEquals(x, _casterApBonusHandler)).ToArray();

            if (rouletteCandidates.Length > 0)
            {
                lock (s_random)
                    SelectedHandler = rouletteCandidates[s_random.Next(rouletteCandidates.Length)];
            }

            m_initialized = true;
        }

        public override void Execute()
        {
            if (!m_initialized)
                Initialize();

            ApplySafely(SelectedHandler);

            if (_casterApBonusHandler != null && !ReferenceEquals(_casterApBonusHandler, SelectedHandler))
                ApplySafely(_casterApBonusHandler);
        }

        private bool IsCasterApBonusEffect(SpellEffectHandler handler)
        {
            if (handler == null || IsRouletteMetaEffect(handler))
                return false;

            if (handler.Id != EffectsEnum.Effect_AddAP_111 && handler.Id != EffectsEnum.Effect_RegainAP)
                return false;

            var affectedActors = handler.GetAffectedActors()?
                .Where(x => x != null)
                .Distinct()
                .ToArray() ?? Array.Empty<FightActor>();

            if (affectedActors.Length != 1 || affectedActors[0] != Caster)
                return false;

            if (handler.TargetType == SpellTargetType.ONLY_SELF || handler.TargetType == SpellTargetType.SELF)
                return true;

            return handler.Value == 1 || handler.DiceNum == 1 || handler.DiceFace == 1;
        }

        private static bool IsRouletteMetaEffect(SpellEffectHandler handler)
        {
            return handler == null || handler.Id == (EffectsEnum)1026;
        }

        private void ApplySafely(SpellEffectHandler handler)
        {
            if (handler == null)
                return;

            try
            {
                handler.Apply();
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Roulette effect failed for spell {Spell?.Id}, effect {(int)handler.Id}: {ex.Message}");
            }
        }
    }
}
