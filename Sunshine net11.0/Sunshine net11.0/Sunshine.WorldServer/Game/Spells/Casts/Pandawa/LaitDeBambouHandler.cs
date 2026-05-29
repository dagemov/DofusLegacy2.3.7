using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;

namespace Sunshine.WorldServer.Game.Spells.Casts.Pandawa
{
    // V9 : Lait de Bambou est volontairement découplé des effets SQL/D2O.
    // Les anciennes corrections laissaient parfois l'état Saoul/Picole côté serveur/client
    // parce que la DB appliquait l'apparence, les résistances et les modifications de sorts
    // via des effets différents. Ici, le sort exécute une seule logique centrale : retour sobre.
    [SpellCastHandler(699)]
    public class LaitDeBambouHandler : SpellCastHandler
    {
        public LaitDeBambouHandler(FightActor caster, Spell spell, short targetedCell, bool critical)
            : base(caster, spell, targetedCell, critical)
        {
        }

        public override void Execute()
        {
            Caster.ApplyBambooMilkPandawaReset();
        }
    }

    // Compatibilité avec les bases où l'enum Sunshine pointe encore sur 705.
    [SpellCastHandler((int)SpellIdEnum.BambooMilk)]
    public class LaitDeBambouAltHandler : LaitDeBambouHandler
    {
        public LaitDeBambouAltHandler(FightActor caster, Spell spell, short targetedCell, bool critical)
            : base(caster, spell, targetedCell, critical)
        {
        }
    }
}

// Compatibilité supplémentaire : certaines DB privées décalent Lait de Bambou sur 705.
namespace Sunshine.WorldServer.Game.Spells.Casts.Pandawa
{
    [SpellCastHandler(705)]
    public class LaitDeBambouDb705Handler : LaitDeBambouHandler
    {
        public LaitDeBambouDb705Handler(FightActor caster, Spell spell, short targetedCell, bool critical)
            : base(caster, spell, targetedCell, critical)
        {
        }
    }
}
