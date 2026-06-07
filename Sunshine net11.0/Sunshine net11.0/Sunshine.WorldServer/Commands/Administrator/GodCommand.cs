using System.Linq;
using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Commands;

namespace Sunshine.WorldServer.Commands.Administrator
{
    [CommandHandler("god", RoleEnum.Administrator)]
    public class GodCommand : WorldCommand
    {
        public override string Description => "Activa o desactiva el modo dios (.god on / .god off [jugador]).";

        public override void Execute()
        {
            if (Client?.Character == null)
                return;

            if (Parameters == null || Parameters.Length == 0)
            {
                Client.Character.SendServerMessage($".god está {(Client.Character.GodMode ? "activado" : "desactivado")}.");
                Client.Character.SendServerMessage("Uso: .god on|off [jugador]");
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
                Client.Character.SendServerMessage("Uso: .god on|off [jugador]");
                return;
            }

            var target = ResolveTarget();
            if (target == null)
            {
                Client.Character.SendServerMessage("Objetivo no encontrado.");
                return;
            }

            target.GodMode = enabled;
            ApplyImmediateGodState(target, enabled);

            var state = enabled ? "activado" : "desactivado";
            if (target == Client.Character)
                Client.Character.SendServerMessage($"Modo dios {state}.");
            else
                Client.Character.SendServerMessage($"Modo dios {state} en {target.Name}.");

            if (target.Client != null && target != Client.Character)
                target.SendServerMessage($"Modo dios {state}.");
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
