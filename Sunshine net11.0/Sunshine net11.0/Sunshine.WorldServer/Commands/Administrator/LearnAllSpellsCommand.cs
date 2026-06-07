using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Commands;
using System.Drawing;

namespace Sunshine.WorldServer.Commands.Administrator
{
    /// <summary>
    /// QA-only bulk spell grant. Safe path: one SpellListMessage instead of N upgrade packets.
    /// See docs/admin-tools/qa/spell-learnall-qa-fix.md to remove or improve.
    /// </summary>
    [CommandHandler("spell learnall", RoleEnum.Administrator)]
    public class LearnAllSpellsCommand : WorldCommand
    {
        public override string Description =>
            "[QA] Aprende todos los hechizos del SpellManager (.spell learnall). Administrator.";

        public override void Execute()
        {
            if (Client?.Character == null)
                return;

            int added = Client.Character.Spells.LearnAllAvailableSpellsForQa();
            if (added < 0)
            {
                Client.Character.SendServerMessage("[QA] .spell learnall no disponible en combate.", Color.Red);
                return;
            }

            if (added == 0)
            {
                Client.Character.SendServerMessage(
                    "[QA] Sin hechizos nuevos (ya los tienes o SpellManager vacío).",
                    Color.Orange);
                return;
            }

            Client.Character.SendServerMessage(
                $"[QA] {added} hechizos añadidos. Un solo SpellListMessage enviado. Usa .save para persistir.",
                Color.Green);
        }
    }
}
