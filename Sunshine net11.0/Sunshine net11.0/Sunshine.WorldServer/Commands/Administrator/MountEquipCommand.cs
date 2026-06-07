using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Commands;
using Sunshine.WorldServer.Game.Items.Custom;
using Sunshine.WorldServer.Game.Mounts;
using System.Drawing;
using System.Linq;

namespace Sunshine.WorldServer.Commands.Moderator
{
    [CommandHandler("mount equip", RoleEnum.Moderator)]
    public class MountEquipCommand : WorldCommand
    {
        public override string Description => "Consume un certificado de dragopavo y equipa la montura vinculada.";

        public override void Execute()
        {
            if (Client?.Character == null)
                return;

            var inventory = Client.Character.Inventory;
            var certificates = inventory.GetItems(CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED)
                .OfType<MountCertificate>()
                .ToList();

            if (!certificates.Any())
            {
                Client.Character.SendServerMessage("No se encontró ningún certificado de dragopavo en el inventario.", Color.Red);
                return;
            }

            MountCertificate certificate = null;
            if (Parameters.Length >= 2)
            {
                int uid;
                if (int.TryParse(Parameters[1]?.ToString(), out uid))
                    certificate = certificates.FirstOrDefault(x => x.Id == uid);
            }

            if (certificate == null && certificates.Count == 1)
                certificate = certificates[0];

            if (certificate == null)
            {
                Client.Character.SendServerMessage("Se encontraron varios certificados. Usa .mount equip <uidCertificado>.", Color.Red);
                return;
            }

            MountCertificateFactory.TryNormalizeImportedCertificate(certificate, Client.Character.Id);
            var mount = MountCertificateFactory.ResolveMount(certificate, Client.Character.Id);
            if (mount == null)
            {
                Client.Send(new MountEquipedErrorMessage((sbyte)MountEquipedErrorEnum.UNSET));
                Client.Character.SendServerMessage("No se pudo resolver la montura del certificado.", Color.Red);
                return;
            }

            if (!Handlers.Mounts.MountHandler.EquipMountFromInventoryCertificate(Client, certificate, mount))
            {
                Client.Send(new MountEquipedErrorMessage((sbyte)MountEquipedErrorEnum.UNSET));
                Client.Character.SendServerMessage("No se pudo equipar la montura.", Color.Red);
                return;
            }

            Client.Character.SendServerMessage("Montura equipada desde el certificado: " + mount.Name + ".");
        }
    }
}
