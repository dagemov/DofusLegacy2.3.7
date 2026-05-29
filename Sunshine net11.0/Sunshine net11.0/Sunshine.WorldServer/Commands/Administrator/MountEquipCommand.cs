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
        public override string Description => "Consume a dragoturkey certificate and equips the linked mount.";

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
                Client.Character.SendServerMessage("Aucun certificat de dragodinde trouvé dans l'inventaire.", Color.Red);
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
                Client.Character.SendServerMessage("Plusieurs certificats trouvés. Utilise .mount equip <uidCertificat>.", Color.Red);
                return;
            }

            MountCertificateFactory.TryNormalizeImportedCertificate(certificate, Client.Character.Id);
            var mount = MountCertificateFactory.ResolveMount(certificate, Client.Character.Id);
            if (mount == null)
            {
                Client.Send(new MountEquipedErrorMessage((sbyte)MountEquipedErrorEnum.UNSET));
                Client.Character.SendServerMessage("Impossible de résoudre la monture du certificat.", Color.Red);
                return;
            }

            if (!Handlers.Mounts.MountHandler.EquipMountFromInventoryCertificate(Client, certificate, mount))
            {
                Client.Send(new MountEquipedErrorMessage((sbyte)MountEquipedErrorEnum.UNSET));
                Client.Character.SendServerMessage("Impossible d'équiper la monture.", Color.Red);
                return;
            }

            Client.Character.SendServerMessage("Monture équipée depuis le certificat : " + mount.Name + ".");
        }
    }
}
