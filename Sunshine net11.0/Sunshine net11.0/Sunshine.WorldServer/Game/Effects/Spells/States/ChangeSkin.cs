using System.Linq;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors.Look;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;

namespace Sunshine.WorldServer.Game.Effects.Spells.States
{
    [EffectHandler(EffectsEnum.Effect_ChangeAppearance)]
    [EffectHandler(EffectsEnum.Effect_ChangeAppearance_335)]
    public class ChangeSkin : SpellEffectHandler
    {
        private const short PLEUTRE_MALE_SKINID = 1443;
        private const short PLEUTRE_FEMALE_SKINID = 1444;
        private const short PSYCHO_MALE_SKINID = 1449;
        private const short PSYCHO_FEMALE_SKINID = 1448;

        private const int PLEUTRE_EFFECT_ID_1 = 102;
        private const int PLEUTRE_EFFECT_ID_2 = 105;
        private const int PSYCHO_EFFECT_ID_1 = 103;
        private const int PSYCHO_EFFECT_ID_2 = 106;

        private static readonly short[] MaskSkins =
        {
            PLEUTRE_MALE_SKINID,
            PSYCHO_MALE_SKINID,
            PLEUTRE_FEMALE_SKINID,
            PSYCHO_FEMALE_SKINID,
        };

        public override void Apply()
        {
            foreach (var actor in GetAffectedActors())
            {
                if (actor == null)
                    continue;

                var look = actor.Look?.Clone() ?? new ActorLook();
                var driverLook = look.SubActorLooks.FirstOrDefault(x =>
                    x.BindingCategory == SubEntityBindingPointCategoryEnum.HOOK_POINT_CATEGORY_MOUNT_DRIVER);
                var destinationLook = driverLook != null ? driverLook.Look : look;

                short skinId;
                short bonesId;
                short scale;
                if (!ResolveAppearance(actor, driverLook != null, out skinId, out bonesId, out scale))
                    continue;

                foreach (var skinBuff in actor.GetBuffs(x => x is SkinBuff).ToArray())
                    actor.RemoveBuff(skinBuff);

                if (skinId != -1)
                {
                    RemoveMaskSkins(destinationLook);
                    destinationLook.RemoveSkin(skinId);
                    destinationLook.AddSkin(skinId);
                }

                if (bonesId != -1)
                    destinationLook.BonesID = bonesId;

                if (scale != -1)
                {
                    destinationLook.Scales.Clear();
                    destinationLook.Scales.Add(scale);
                    destinationLook.Scales.Add(scale);
                }

                short duration = Duration == -1 ? (short)1000 : (short)Duration;
                actor.AddBuff(new SkinBuff(Caster, actor, Spell, Effect, duration, true, look));
            }
        }

        private static void RemoveMaskSkins(ActorLook look)
        {
            if (look == null)
                return;

            foreach (var skin in MaskSkins)
                look.RemoveSkin(skin);
        }

        private static bool GetSex(FightActor actor)
        {
            if (actor is CharacterFighter fighter && fighter.Character != null)
                return fighter.Character.Sex;

            return false;
        }

        private bool ResolveAppearance(FightActor actor, bool hasDriverLook, out short skinId, out short bonesId, out short scale)
        {
            skinId = -1;
            bonesId = -1;
            scale = -1;

            bool isFemale = GetSex(actor);
            var candidates = new[] { Value, (int)DiceNum, (int)DiceFace }
                .Where(x => x > 0)
                .Distinct()
                .ToArray();

            foreach (var candidate in candidates)
            {
                switch (candidate)
                {
                    case 667: // Pandawa - Picole
                        bonesId = (short)(hasDriverLook ? 1084 : 44);
                        return true;

                    case 729: // Xelor - Momification
                        bonesId = (short)(hasDriverLook ? 1068 : 113);
                        return true;

                    case 874: // Pandawa - Colère de Zatoïshwan
                        bonesId = (short)(hasDriverLook ? 1202 : 453);
                        scale = (short)(hasDriverLook ? 60 : 80);
                        return true;

                    case 969:
                        bonesId = 1246;
                        scale = 120;
                        return true;

                    case 970:
                        bonesId = 1243;
                        scale = 110;
                        return true;

                    case 971:
                        bonesId = 1249;
                        scale = 110;
                        return true;

                    // 1035 (scaphandre) intentionally ignored per project rules.

                    case 1575:
                    case PLEUTRE_EFFECT_ID_1:
                    case PLEUTRE_EFFECT_ID_2:
                        skinId = isFemale ? PLEUTRE_FEMALE_SKINID : PLEUTRE_MALE_SKINID;
                        return true;

                    case 1576:
                    case PSYCHO_EFFECT_ID_1:
                    case PSYCHO_EFFECT_ID_2:
                        skinId = isFemale ? PSYCHO_FEMALE_SKINID : PSYCHO_MALE_SKINID;
                        return true;

                    case PLEUTRE_MALE_SKINID:
                    case PLEUTRE_FEMALE_SKINID:
                    case PSYCHO_MALE_SKINID:
                    case PSYCHO_FEMALE_SKINID:
                        skinId = (short)candidate;
                        return true;
                }
            }

            return false;
        }
    }
}
