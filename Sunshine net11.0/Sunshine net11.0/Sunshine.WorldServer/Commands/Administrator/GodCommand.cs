using System.Linq;
using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Commands;

namespace Sunshine.WorldServer.Commands.Administrator
{
    [CommandHandler("god", RoleEnum.Administrator)]
    public class GodCommand : WorldCommand
    {
        public override string Description => "Active ou désactive le mode god (.god on / .god off [joueur]).";

        public override void Execute()
        {
            if (Client?.Character == null)
                return;

            if (Parameters == null || Parameters.Length == 0)
            {
                Client.Character.SendServerMessage($".god est {(Client.Character.GodMode ? "activé" : "désactivé")}.");
                Client.Character.SendServerMessage("Usage: .god on|off [joueur]");
                return;
            }

            string mode = (Parameters[0] ?? string.Empty).ToString().Trim().ToLowerInvariant();
            bool enabled;
            if (mode == "on" || mode == "1" || mode == "true")
                enabled = true;
            else if (mode == "off" || mode == "0" || mode == "false")
                enabled = false;
            else
            {
                Client.Character.SendServerMessage("Usage: .god on|off [joueur]");
                return;
            }

            var target = ResolveTarget();
            if (target == null)
            {
                Client.Character.SendServerMessage("Cible introuvable.");
                return;
            }

            target.GodMode = enabled;
            ApplyImmediateGodState(target, enabled);

            var state = enabled ? "activé" : "désactivé";
            if (target == Client.Character)
                Client.Character.SendServerMessage($"Mode god {state}.");
            else
                Client.Character.SendServerMessage($"Mode god {state} sur {target.Name}.");

            if (target.Client != null && target != Client.Character)
                target.SendServerMessage($"Mode god {state}.");
        }

        private Game.Characters.Character ResolveTarget()
        {
            if (Parameters == null || Parameters.Length < 2)
                return Client.Character;

            string targetName = (Parameters[1] ?? string.Empty).ToString().Trim();
            if (string.IsNullOrWhiteSpace(targetName))
                return Client.Character;

            return CharacterManager.Instance.Characters.Values
                .FirstOrDefault(x => x != null && x.Name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase));
        }

        private static void ApplyImmediateGodState(Game.Characters.Character target, bool enabled)
        {
            if (target == null)
                return;

            if (enabled)
            {
                target.Stats.Health.Taken = 0;
                target.RefreshStats();

                if (target.IsInFight() && target.Fighter != null && !target.Fighter.IsDead())
                {
                    target.Fighter.ResetUsedPoints();
                    Handlers.Context.ContextHandler.SendGameFightSynchronizeMessage(target.Fight.Clients, target.Fighter);
                }
            }
            else if (target.IsInFight() && target.Fighter != null && !target.Fighter.IsDead())
            {
                Handlers.Context.ContextHandler.SendGameFightSynchronizeMessage(target.Fight.Clients, target.Fighter);
            }
        }
    }
}
