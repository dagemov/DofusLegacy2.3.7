using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Exchanges;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Maps.Interactives.Skills;
using Sunshine.WorldServer.Game.Maps.Pathfinding;
using Sunshine.WorldServer.Game.Maps.Paddocks;
using Sunshine.WorldServer.Game.Mounts;
using Sunshine.WorldServer.Handlers.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Handlers.Mounts
{
    public class MountHandler : WorldPacketHandler
    {
        private static void RefreshMountInventoryDisplay(WorldClient client)
        {
            if (client?.Character == null)
                return;

            client.Character.Inventory?.SynchronizePresetObjects(true);
            client.Character.Shortcuts?.SynchronizeItemShortcuts(true);
            client.Character.Shortcuts?.SynchronizePresetShortcuts(true);
            Handlers.Characters.Inventory.InventoryHandler.SendInventoryContentMessage(client);
            Handlers.Characters.Shorcuts.ShortcutHandler.SendShortcutBarContentMessage(client, ShortcutBarEnum.OBJECT);
            Handlers.Characters.Inventory.InventoryHandler.SendInventoryWeightMessage(client);
        }


        private static void RefreshCharacterVisualState(WorldClient client)
        {
            if (client?.Character == null)
                return;

            client.Character.UpdateLook(!string.IsNullOrWhiteSpace(client.Character.Record?.CustomLook));
            client.Character.RefreshStats();
        }
        private static Paddock GetCurrentPaddock(WorldClient client)
        {
            return client?.Character?.Map == null
                ? null
                : PaddockManager.Instance.GetPaddockByMap(client.Character.Map.Id);
        }

        private static bool IsPaddockMap(WorldClient client)
        {
            if (client?.Character?.Map == null)
                return false;

            if (GetCurrentPaddock(client) != null)
                return true;

            return client.Character.Map.Interactives != null &&
                   client.Character.Map.Interactives.Any(x => x != null && (x.Type == 120 || (x.Skills != null && x.Skills.Any(skill => skill == 175 || skill == 176 || skill == 177 || skill == 178))));
        }

        private static bool CanUseCurrentPaddock(WorldClient client)
        {
            var paddock = GetCurrentPaddock(client);
            return paddock == null || paddock.CanUsePaddock(client?.Character);
        }

        private static bool CanStoreMountInCurrentPaddock(WorldClient client)
        {
            if (!IsPaddockMap(client) || !CanUseCurrentPaddock(client))
                return false;

            var paddock = GetCurrentPaddock(client);
            if (paddock == null)
                return true;

            if (paddock.MaxOutdoorMount <= 0)
                return true;

            return GetVisiblePaddockMounts(client).Count < paddock.MaxOutdoorMount;
        }

        private static List<Mount> GetAccessiblePaddockMounts(WorldClient client)
        {
            if (client?.Character?.Map == null || !IsPaddockMap(client))
                return new List<Mount>();

            var paddock = GetCurrentPaddock(client);
            if (paddock != null && !paddock.IsPublicPaddock)
            {
                if (!paddock.CanUsePaddock(client.Character))
                    return new List<Mount>();

                return MountManager.Instance.GetPaddockedMountsByMap(client.Character.Map.Id)
                    .Where(x => x != null && x.Record != null)
                    .OrderBy(x => x.Name)
                    .ThenBy(x => x.Id)
                    .ToList();
            }

            return MountManager.Instance.GetPaddockedMounts(client.Character.Id, client.Character.Account?.Id, client.Character.Map.Id)
                .Where(x => x != null && x.Record != null)
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToList();
        }

        private static List<Mount> GetVisiblePaddockMounts(WorldClient client)
        {
            if (client?.Character?.Map == null || !IsPaddockMap(client))
                return new List<Mount>();

            var paddock = GetCurrentPaddock(client);
            if (paddock != null && !paddock.IsPublicPaddock)
            {
                return MountManager.Instance.GetPaddockedMountsByMap(client.Character.Map.Id)
                    .Where(x => x != null && x.Record != null)
                    .OrderBy(x => x.Name)
                    .ThenBy(x => x.Id)
                    .ToList();
            }

            return MountManager.Instance.GetPaddockedMounts(client.Character.Id, client.Character.Account?.Id, client.Character.Map.Id)
                .Where(x => x != null && x.Record != null)
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToList();
        }

        private static bool CanManagePaddockMount(WorldClient client, Mount mount)
        {
            if (client?.Character == null || mount == null)
                return false;

            if (!GetAccessiblePaddockMounts(client).Any(x => x.Id == mount.Id))
                return false;

            return IsOwnedByCharacter(client, mount);
        }


        private static IEnumerable<Mount> GetOwnedMounts(WorldClient client)
        {
            if (client?.Character == null)
                return Enumerable.Empty<Mount>();

            return MountManager.Instance.GetOwnerMounts(client.Character.Id, client.Character.Account?.Id);
        }

        private static bool TryGetCertificateMountId(BasePlayerItem item, out int mountId)
        {
            mountId = 0;

            ObjectEffectMount mountEffect;
            if (!MountCertificateFactory.TryGetMountEffect(item, out mountEffect) || mountEffect == null)
                return false;

            mountId = mountEffect.mountId;
            return mountId > 0;
        }

        private static void RemoveGhostCertificates(WorldClient client, int mountId, int keepItemId = 0)
        {
            if (client?.Character?.Inventory == null || mountId <= 0)
                return;

            var ownedMounts = GetOwnedMounts(client).ToDictionary(x => x.Id, x => x);
            var targetMount = ownedMounts.ContainsKey(mountId) ? ownedMounts[mountId] : MountManager.Instance.GetMount(mountId);

            var ghostCertificates = client.Character.Inventory.GetItems()
                .Where(x => x != null && x.Template != null && MountManager.Instance.IsMountCertificateTemplate(x.Template.Id))
                .Where(x => keepItemId <= 0 || x.Id != keepItemId)
                .Where(x =>
                {
                    try
                    {
                        MountCertificateFactory.TryNormalizeImportedCertificate(x, client.Character.Id);
                    }
                    catch
                    {
                    }

                    int directMountId;
                    if (TryGetCertificateMountId(x, out directMountId))
                    {
                        if (directMountId == mountId)
                            return true;

                        Mount directMount;
                        if (ownedMounts.TryGetValue(directMountId, out directMount))
                        {
                            return directMount.Record == null || !directMount.Record.StoredSince.HasValue || directMount.Record.IsInStable > 0 || directMount.Record.PaddockId.HasValue;
                        }

                        return targetMount != null && targetMount.Template != null && x.Template.Id == targetMount.Template.ScrollId;
                    }

                    var resolvedMount = MountCertificateFactory.ResolveMount(x, client.Character.Id);
                    if (resolvedMount != null)
                    {
                        if (resolvedMount.Id == mountId)
                            return true;

                        return resolvedMount.Record == null || !resolvedMount.Record.StoredSince.HasValue || resolvedMount.Record.IsInStable > 0 || resolvedMount.Record.PaddockId.HasValue;
                    }

                    return targetMount != null && targetMount.Template != null && x.Template.Id == targetMount.Template.ScrollId;
                })
                .ToArray();

            foreach (var ghostCertificate in ghostCertificates)
                client.Character.Inventory.RemoveItem(ghostCertificate, Math.Max(1, ghostCertificate.Stack));
        }

        private static bool IsCertificateState(Mount mount)
        {
            return mount != null && mount.Record != null && mount.Record.StoredSince.HasValue && mount.Record.IsInStable <= 0 && !mount.Record.PaddockId.HasValue;
        }

        private static void PersistCharacterState(WorldClient client)
        {
            if (client?.Character == null)
                return;

            CharacterManager.Instance.Save(client.Character);
        }

        private enum StableExchangeAction
        {
            EQUIP_TO_STABLE = 1,
            STABLE_TO_EQUIP = 2,
            STABLE_TO_INVENTORY = 4,
            INVENTORY_TO_STABLE = 5,
            STABLE_TO_PADDOCK = 6,
            PADDOCK_TO_STABLE = 7,
            EQUIP_TO_PADDOCK = 9,
            PADDOCK_TO_EQUIP = 10,
            EQUIP_TO_INVENTORY = 13,
            PADDOCK_TO_INVENTORY = 14,
            INVENTORY_TO_EQUIP = 15,
            INVENTORY_TO_PADDOCK = 16,
        }

        [WorldHandler(MountToggleRidingRequestMessage.Id)]
        public static void HandleMountToggleRidingRequestMessage(WorldClient client, MountToggleRidingRequestMessage message)
        {
            if (client?.Character?.EquippedMount == null)
                return;

            var mount = client.Character.EquippedMount;
            if (!client.Character.IsRiding)
            {
                if (client.Character.Level < 60)
                {
                    client.Send(new MountEquipedErrorMessage((sbyte)MountEquipedErrorEnum.SET));
                    return;
                }

                if (!mount.IsRideable)
                {
                    client.Send(new MountEquipedErrorMessage((sbyte)MountEquipedErrorEnum.UNSET));
                    return;
                }

                UnequipPetIfNeeded(client);
                mount.ApplyMountEffects(client.Character, false);
                client.Character.IsRiding = true;
            }
            else
            {
                mount.UnApplyMountEffects(client.Character, false);
                client.Character.IsRiding = false;
            }

            RefreshCharacterVisualState(client);
            client.Send(new MountRidingMessage(client.Character.IsRiding));
            RefreshMountInventoryDisplay(client);
            PersistCharacterState(client);
        }

        [WorldHandler(MountSetXpRatioRequestMessage.Id)]
        public static void HandleMountSetXpRatioRequestMessage(WorldClient client, MountSetXpRatioRequestMessage message)
        {
            if (client?.Character?.EquippedMount == null)
                return;

            int xp = Math.Max(0, Math.Min(90, (int)message.xpRatio));
            client.Character.EquippedMount.Record.GivenExperience = (sbyte)xp;
            MountManager.Instance.Save(client.Character.EquippedMount);
            client.Send(new MountXpRatioMessage((sbyte)xp));
            PersistCharacterState(client);
        }

        [WorldHandler(MountReleaseRequestMessage.Id)]
        public static void HandleMountReleaseRequestMessage(WorldClient client, MountReleaseRequestMessage message)
        {
            if (client?.Character?.EquippedMount == null)
                return;

            var mount = client.Character.EquippedMount;
            if (client.Character.IsRiding)
                mount.UnApplyMountEffects(client.Character, false);

            client.Character.IsRiding = false;
            client.Character.EquippedMount = null;
            mount.Record.StoredSince = null;
            MountManager.Instance.Save(mount);

            RefreshCharacterVisualState(client);
            client.Send(new MountUnSetMessage());
            client.Send(new MountRidingMessage(false));
            RefreshMountInventoryDisplay(client);
            PersistCharacterState(client);
        }

        [WorldHandler(MountSterilizeRequestMessage.Id)]
        public static void HandleMountSterilizeRequestMessage(WorldClient client, MountSterilizeRequestMessage message)
        {
            if (client?.Character?.EquippedMount == null)
                return;

            client.Character.EquippedMount.Record.ReproductionCount = -1;
            MountManager.Instance.Save(client.Character.EquippedMount);
            client.Send(new MountSterilizedMessage(client.Character.EquippedMount.Id));
            PersistCharacterState(client);
        }

        [WorldHandler(MountInformationRequestMessage.Id)]
        public static void HandleMountInformationRequestMessage(WorldClient client, MountInformationRequestMessage message)
        {
            if (client == null || client.Character == null)
                return;

            var mount = MountManager.Instance.GetMount((int)message.id);
            if (mount == null)
            {
                client.Send(new MountDataErrorMessage(1));
                return;
            }

            if (client.Character.IsInTrade() && client.Character.Trade is MountStockTrade stockTrade && stockTrade.Mount != null && stockTrade.Mount.Id != mount.Id)
                return;

            bool isOwner = IsOwnedByCharacter(client, mount);
            bool isManagedMount = isOwner ||
                                 client.Character.EquippedMount?.Id == mount.Id ||
                                 IsStableMountForCharacter(client, mount) ||
                                 IsPaddockMountForCharacter(client, mount);

            BasePlayerItem certificate = client.Character.Inventory.GetItems()
                .FirstOrDefault(x =>
                {
                    ObjectEffectMount effect;
                    if (!MountCertificateFactory.TryGetMountEffect(x, out effect))
                        return false;

                    if (effect.mountId != (int)message.id)
                        return false;

                    return MountCertificateFactory.IsCertificateStillValid(x);
                });

            bool hasCertificate = certificate != null;
            if (!isManagedMount && !hasCertificate)
            {
                client.Send(new MountDataErrorMessage(1));
                return;
            }

            client.Character.LastViewedMountId = mount.Id;
            client.Send(new MountDataMessage(mount.GetClientData()));
        }

        [WorldHandler(MountInformationInPaddockRequestMessage.Id)]
        public static void HandleMountInformationInPaddockRequestMessage(WorldClient client, MountInformationInPaddockRequestMessage message)
        {
            if (client == null || client.Character == null)
                return;

            var mount = MountManager.Instance.GetMount(message.mapRideId);
            if (mount == null)
            {
                client.Send(new MountDataErrorMessage(1));
                return;
            }

            var allowed = GetVisiblePaddockMounts(client).Any(x => x.Id == mount.Id);

            if (!allowed)
            {
                client.Send(new MountDataErrorMessage(1));
                return;
            }

            client.Character.LastTargetedMountId = mount.Id;
            client.Character.LastViewedMountId = mount.Id;
            client.Send(new MountDataMessage(mount.GetClientData()));
        }

        [WorldHandler(MountRenameRequestMessage.Id)]
        public static void HandleMountRenameRequestMessage(WorldClient client, MountRenameRequestMessage message)
        {
            if (client?.Character == null)
                return;

            var mount = MountManager.Instance.GetMount((int)message.mountId);
            if (mount == null || !IsOwnedByCharacter(client, mount))
            {
                client.Send(new MountDataErrorMessage(1));
                return;
            }

            var normalizedName = (message.name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
                normalizedName = "Dragodinde";
            if (normalizedName.Length > 20)
                normalizedName = normalizedName.Substring(0, 20);

            if (string.Equals(mount.Record?.Name ?? string.Empty, normalizedName, StringComparison.Ordinal))
                return;

            mount.Record.Name = normalizedName;
            MountManager.Instance.Save(mount);

            var updatedCertificates = RefreshMountCertificateItems(client, mount);

            client.Character.LastViewedMountId = mount.Id;
            client.Character.LastTargetedMountId = mount.Id;

            client.Send(new MountRenamedMessage(mount.Id, mount.Name));

            if (updatedCertificates || client.Character.EquippedMount?.Id == mount.Id)
                RefreshMountInventoryDisplay(client);

            if (client.Character.IsInTrade() && client.Character.Trade is MountStockTrade stockTrade && stockTrade.Mount != null && stockTrade.Mount.Id == mount.Id)
                stockTrade.Refresh();

            if (ShouldDisplayPaddockVisuals(client) &&
                (client.Character.EquippedMount?.Id == mount.Id || IsStableMountForCharacter(client, mount) || IsPaddockMountForCharacter(client, mount)))
                SyncPublicPaddockMountVisuals(client);

            PersistCharacterState(client);
        }

        [WorldHandler(ExchangeRequestOnMountStockMessage.Id)]
        public static void HandleExchangeRequestOnMountStockMessage(WorldClient client, ExchangeRequestOnMountStockMessage message)
        {
            if (client?.Character == null)
                return;

            var targetMount = GetMountFromStockContext(client);
            if (targetMount == null || !IsOwnedByCharacter(client, targetMount))
                return;

            client.Character.LastTargetedMountId = targetMount.Id;
            client.Character.LastViewedMountId = targetMount.Id;

            if (client.Character.IsInTrade() && client.Character.Trade is MountStockTrade currentStock)
            {
                if (currentStock.Mount != null && currentStock.Mount.Id == targetMount.Id)
                    currentStock.Refresh();

                return;
            }

            client.Character.SetTrade(ExchangeTypeEnum.MOUNT, new MountStockTrade(client.Character, targetMount));
            client.Character.Trade.Open();
        }

        [WorldHandler(ExchangeHandleMountStableMessage.Id)]
        public static void HandleExchangeHandleMountStableMessage(WorldClient client, ExchangeHandleMountStableMessage message)
        {
            if (client?.Character?.Map == null)
                return;

            var action = (StableExchangeAction)message.actionType;
            if (action == StableExchangeAction.STABLE_TO_PADDOCK ||
                action == StableExchangeAction.PADDOCK_TO_STABLE ||
                action == StableExchangeAction.EQUIP_TO_PADDOCK ||
                action == StableExchangeAction.PADDOCK_TO_EQUIP ||
                action == StableExchangeAction.PADDOCK_TO_INVENTORY ||
                action == StableExchangeAction.INVENTORY_TO_PADDOCK)
            {
                if (!IsPaddockMap(client) || !CanUseCurrentPaddock(client))
                {
                    client.Send(new ExchangeMountStableErrorMessage());
                    return;
                }
            }

            switch (action)
            {
                case StableExchangeAction.EQUIP_TO_STABLE:
                    EquipToStable(client, message.rideId);
                    break;
                case StableExchangeAction.STABLE_TO_EQUIP:
                    StableToEquip(client, message.rideId);
                    break;
                case StableExchangeAction.STABLE_TO_INVENTORY:
                    StableToInventory(client, message.rideId);
                    break;
                case StableExchangeAction.INVENTORY_TO_STABLE:
                    InventoryToStable(client, message.rideId);
                    break;
                case StableExchangeAction.STABLE_TO_PADDOCK:
                    StableToPaddock(client, message.rideId);
                    break;
                case StableExchangeAction.PADDOCK_TO_STABLE:
                    PaddockToStable(client, message.rideId);
                    break;
                case StableExchangeAction.EQUIP_TO_PADDOCK:
                    EquipToPaddock(client, message.rideId);
                    break;
                case StableExchangeAction.PADDOCK_TO_EQUIP:
                    PaddockToEquip(client, message.rideId);
                    break;
                case StableExchangeAction.EQUIP_TO_INVENTORY:
                    EquipToInventory(client, message.rideId);
                    break;
                case StableExchangeAction.PADDOCK_TO_INVENTORY:
                    PaddockToInventory(client, message.rideId);
                    break;
                case StableExchangeAction.INVENTORY_TO_EQUIP:
                    InventoryToEquip(client, message.rideId);
                    break;
                case StableExchangeAction.INVENTORY_TO_PADDOCK:
                    InventoryToPaddock(client, message.rideId);
                    break;
                default:
                    client.Send(new ExchangeMountStableErrorMessage());
                    return;
            }

            RefreshPaddockPanel(client);
        }

        private static bool IsOwnedByCharacter(WorldClient client, Mount mount)
        {
            if (client?.Character == null || mount == null)
                return false;

            return mount.Record.OwnerId == client.Character.Id ||
                   (client.Character.Account != null && mount.Record.OwnerId == client.Character.Account.Id);
        }

        private static bool IsStableMountForCharacter(WorldClient client, Mount mount)
        {
            if (client?.Character == null || mount == null)
                return false;

            return MountManager.Instance.GetStableMounts(client.Character.Id, client.Character.Account?.Id).Any(x => x.Id == mount.Id);
        }

        private static bool IsPaddockMountForCharacter(WorldClient client, Mount mount)
        {
            if (client?.Character?.Map == null || mount == null)
                return false;

            return GetAccessiblePaddockMounts(client).Any(x => x.Id == mount.Id);
        }

        private static bool HasActiveCertificateForMount(WorldClient client, Mount mount)
        {
            if (client?.Character?.Inventory == null || mount == null)
                return false;

            return client.Character.Inventory.GetItems().Any(x =>
            {
                if (x == null || x.Template == null || !MountManager.Instance.IsMountCertificateTemplate(x.Template.Id))
                    return false;

                ObjectEffectMount effect;
                return MountCertificateFactory.TryGetMountEffect(x, out effect) &&
                       effect != null &&
                       effect.mountId == mount.Id &&
                       MountCertificateFactory.IsCertificateStillValid(x);
            });
        }

        private static bool RefreshMountCertificateItems(WorldClient client, Mount mount)
        {
            if (client?.Character?.Inventory == null || mount == null)
                return false;

            bool updated = false;
            foreach (var item in client.Character.Inventory.GetItems()
                .Where(x => x != null && x.Template != null && MountManager.Instance.IsMountCertificateTemplate(x.Template.Id))
                .ToArray())
            {
                ObjectEffectMount effect;
                if (!MountCertificateFactory.TryGetMountEffect(item, out effect) || effect == null || effect.mountId != mount.Id)
                    continue;

                item.RawObjectEffects = MountCertificateFactory.BuildEffects(mount);
                updated = true;
            }

            return updated;
        }

        private static bool CanEquipMount(WorldClient client, Mount mount)
        {
            if (client?.Character == null || mount == null)
                return false;

            MountCertificateFactory.EnsureMountIsRideable(mount);

            if (client.Character.EquippedMount != null)
                return false;

            if (client.Character.Level < 60)
            {
                client.Send(new MountEquipedErrorMessage((sbyte)MountEquipedErrorEnum.SET));
                return false;
            }

            if (!mount.IsRideable)
            {
                client.Send(new MountEquipedErrorMessage((sbyte)MountEquipedErrorEnum.UNSET));
                return false;
            }

            return true;
        }

        public static BasePlayerItem CreateMountCertificate(WorldClient client, Mount mount, int validityDays = 20)
        {
            if (client == null || client.Character == null || mount == null)
                return null;

            if (!IsOwnedByCharacter(client, mount))
                return null;

            mount.Record.OwnerId = client.Character.Id;
            mount.Record.OwnerName = client.Character.Name;
            mount.Record.StoredSince = DateTime.Now;
            mount.Record.IsInStable = 0;
            mount.Record.PaddockId = null;

            var certificate = MountCertificateFactory.CreateCertificate(mount, validityDays);
            if (certificate == null)
                return null;

            RemoveGhostCertificates(client, mount.Id);
            client.Character.Inventory.AddItem(certificate);
            MountManager.Instance.Save(mount);
            PersistCharacterState(client);
            return certificate;
        }

        public static void UnequipPetIfNeeded(WorldClient client)
        {
            var pet = client?.Character?.Inventory?.GetItem(CharacterInventoryPositionEnum.ACCESSORY_POSITION_PETS);
            if (pet == null)
                return;

            client.Character.Inventory.MoveItem(pet, CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED);
        }

        public static bool TryDismountCurrentMount(WorldClient client)
        {
            if (client?.Character?.EquippedMount == null)
                return true;

            if (!client.Character.IsRiding)
                return true;

            client.Character.EquippedMount.UnApplyMountEffects(client.Character, false);
            client.Character.IsRiding = false;
            RefreshCharacterVisualState(client);
            client.Send(new MountRidingMessage(false));
            RefreshMountInventoryDisplay(client);
            PersistCharacterState(client);
            return true;
        }

        public static bool TryUnequipCurrentMountToInventory(WorldClient client)
        {
            if (client?.Character?.EquippedMount == null)
                return true;

            return TryUnequipMountToInventory(client, client.Character.EquippedMount.Id);
        }

        public static bool EquipMountFromInventoryCertificate(WorldClient client, BasePlayerItem item, Mount mount)
        {
            if (client?.Character?.Inventory == null || item == null || mount == null)
                return false;

            UnequipPetIfNeeded(client);
            MountCertificateFactory.EnsureMountIsRideable(mount);

            if (!CanEquipMount(client, mount))
                return false;

            mount.Record.OwnerId = client.Character.Id;
            mount.Record.OwnerName = client.Character.Name;
            mount.Record.IsInStable = 0;
            mount.Record.PaddockId = null;
            mount.Record.StoredSince = null;

            client.Character.EquippedMount = mount;
            client.Character.LastTargetedMountId = mount.Id;
            client.Character.LastViewedMountId = mount.Id;
            client.Character.IsRiding = false;
            MountManager.Instance.Save(mount);
            client.Character.Inventory.RemoveItem(item, 1);
            RemoveGhostCertificates(client, mount.Id);

            RefreshCharacterVisualState(client);
            client.Send(new MountSetMessage(mount.GetClientData()));
            client.Send(new MountXpRatioMessage((sbyte)Math.Max(0, Math.Min(90, (int)mount.Record.GivenExperience))));
            client.Send(new MountRidingMessage(false));
            RefreshMountInventoryDisplay(client);
            PersistCharacterState(client);
            return true;
        }

        private static void EquipToStable(WorldClient client, int mountId)
        {
            var mount = client.Character.EquippedMount;
            if (mount == null || mount.Id != mountId)
                return;

            if (client.Character.IsRiding)
                mount.UnApplyMountEffects(client.Character, false);

            client.Character.IsRiding = false;
            client.Character.EquippedMount = null;
            mount.Record.OwnerId = client.Character.Id;
            mount.Record.OwnerName = client.Character.Name;
            mount.Record.IsInStable = 1;
            mount.Record.PaddockId = null;
            mount.Record.StoredSince = null;
            RemoveGhostCertificates(client, mount.Id);
            client.Character.LastTargetedMountId = mount.Id;
            client.Character.LastViewedMountId = mount.Id;
            MountManager.Instance.Save(mount);
            RefreshCharacterVisualState(client);
            client.Send(new MountUnSetMessage());
            client.Send(new MountRidingMessage(false));
            client.Send(new ExchangeMountStableAddMessage(mount.GetClientData()));
            RefreshMountInventoryDisplay(client);
            PersistCharacterState(client);
        }

        private static void StableToEquip(WorldClient client, int mountId)
        {
            var mount = GetStableMount(client, mountId);
            if (mount == null || !CanEquipMount(client, mount))
                return;

            UnequipPetIfNeeded(client);
            RemoveGhostCertificates(client, mount.Id);
            mount.Record.OwnerId = client.Character.Id;
            mount.Record.OwnerName = client.Character.Name;
            mount.Record.IsInStable = 0;
            mount.Record.PaddockId = null;
            mount.Record.StoredSince = null;
            RemoveGhostCertificates(client, mount.Id);
            client.Character.EquippedMount = mount;
            client.Character.LastTargetedMountId = mount.Id;
            client.Character.LastViewedMountId = mount.Id;
            client.Character.IsRiding = false;
            MountManager.Instance.Save(mount);
            client.Send(new ExchangeMountStableRemoveMessage(mount.Id));
            RefreshMountInventoryDisplay(client);
            RefreshCharacterVisualState(client);
            client.Send(new MountSetMessage(mount.GetClientData()));
            client.Send(new MountXpRatioMessage((sbyte)Math.Max(0, Math.Min(90, (int)mount.Record.GivenExperience))));
            client.Send(new MountRidingMessage(false));
            RefreshMountInventoryDisplay(client);
            PersistCharacterState(client);
        }

        private static void StableToInventory(WorldClient client, int mountId)
        {
            var mount = GetStableMount(client, mountId);
            if (mount == null)
                return;

            mount.Record.IsInStable = 0;
            mount.Record.PaddockId = null;
            if (CreateMountCertificate(client, mount) != null)
                client.Send(new ExchangeMountStableRemoveMessage(mount.Id));
            RefreshMountInventoryDisplay(client);
            PersistCharacterState(client);
        }

        private static void InventoryToStable(WorldClient client, int itemUid)
        {
            var item = GetCertificateItem(client, itemUid);
            var mount = ResolveInventoryMount(client, item);
            if (mount == null)
                return;

            MountCertificateFactory.EnsureMountIsRideable(mount);
            mount.Record.OwnerId = client.Character.Id;
            mount.Record.OwnerName = client.Character.Name;
            mount.Record.IsInStable = 1;
            mount.Record.PaddockId = null;
            mount.Record.StoredSince = null;
            MountManager.Instance.Save(mount);
            client.Character.Inventory.RemoveItem(item, 1);
            RemoveGhostCertificates(client, mount.Id);
            client.Send(new ExchangeMountStableAddMessage(mount.GetClientData()));
            RefreshMountInventoryDisplay(client);
            PersistCharacterState(client);
        }

        private static void StableToPaddock(WorldClient client, int mountId)
        {
            var mount = GetStableMount(client, mountId);
            if (mount == null || !CanStoreMountInCurrentPaddock(client))
                return;

            mount.Record.OwnerId = client.Character.Id;
            mount.Record.OwnerName = client.Character.Name;
            mount.Record.IsInStable = 0;
            mount.Record.PaddockId = client.Character.Map.Id;
            mount.Record.StoredSince = null;
            RemoveGhostCertificates(client, mount.Id);
            client.Character.LastTargetedMountId = mount.Id;
            client.Character.LastViewedMountId = mount.Id;
            MountManager.Instance.Save(mount);
            client.Send(new ExchangeMountStableRemoveMessage(mount.Id));
            RefreshMountInventoryDisplay(client);
            client.Send(new ExchangeMountPaddockAddMessage(mount.GetClientData()));
            RefreshMountInventoryDisplay(client);
            PersistCharacterState(client);
        }

        private static void PaddockToStable(WorldClient client, int mountId)
        {
            var mount = GetPaddockMount(client, mountId);
            if (mount == null)
                return;

            mount.Record.OwnerId = client.Character.Id;
            mount.Record.OwnerName = client.Character.Name;
            mount.Record.IsInStable = 1;
            mount.Record.PaddockId = null;
            mount.Record.StoredSince = null;
            RemoveGhostCertificates(client, mount.Id);
            client.Character.LastViewedMountId = mount.Id;
            MountManager.Instance.Save(mount);
            client.Send(new ExchangeMountPaddockRemoveMessage(mount.Id));
            client.Send(new ExchangeMountStableAddMessage(mount.GetClientData()));
            RefreshMountInventoryDisplay(client);
            PersistCharacterState(client);
        }

        private static void EquipToPaddock(WorldClient client, int mountId)
        {
            var mount = client.Character.EquippedMount;
            if (mount == null || mount.Id != mountId || !CanStoreMountInCurrentPaddock(client))
                return;

            if (client.Character.IsRiding)
                mount.UnApplyMountEffects(client.Character, false);

            client.Character.IsRiding = false;
            client.Character.EquippedMount = null;
            mount.Record.OwnerId = client.Character.Id;
            mount.Record.OwnerName = client.Character.Name;
            mount.Record.IsInStable = 0;
            mount.Record.PaddockId = client.Character.Map.Id;
            mount.Record.StoredSince = null;
            RemoveGhostCertificates(client, mount.Id);
            client.Character.LastTargetedMountId = mount.Id;
            client.Character.LastViewedMountId = mount.Id;
            MountManager.Instance.Save(mount);
            RefreshCharacterVisualState(client);
            client.Send(new MountUnSetMessage());
            client.Send(new MountRidingMessage(false));
            client.Send(new ExchangeMountPaddockAddMessage(mount.GetClientData()));
            RefreshMountInventoryDisplay(client);
            PersistCharacterState(client);
        }

        private static void PaddockToEquip(WorldClient client, int mountId)
        {
            var mount = GetPaddockMount(client, mountId);
            if (mount == null || !CanEquipMount(client, mount))
                return;

            UnequipPetIfNeeded(client);
            RemoveGhostCertificates(client, mount.Id);
            mount.Record.OwnerId = client.Character.Id;
            mount.Record.OwnerName = client.Character.Name;
            mount.Record.IsInStable = 0;
            mount.Record.PaddockId = null;
            mount.Record.StoredSince = null;
            RemoveGhostCertificates(client, mount.Id);
            client.Character.EquippedMount = mount;
            client.Character.LastTargetedMountId = mount.Id;
            client.Character.LastViewedMountId = mount.Id;
            client.Character.IsRiding = false;
            MountManager.Instance.Save(mount);
            client.Send(new ExchangeMountPaddockRemoveMessage(mount.Id));
            RefreshCharacterVisualState(client);
            client.Send(new MountSetMessage(mount.GetClientData()));
            client.Send(new MountXpRatioMessage((sbyte)Math.Max(0, Math.Min(90, (int)mount.Record.GivenExperience))));
            client.Send(new MountRidingMessage(false));
            RefreshMountInventoryDisplay(client);
            PersistCharacterState(client);
        }

        private static void EquipToInventory(WorldClient client, int mountId)
        {
            TryUnequipMountToInventory(client, mountId);
        }

        private static bool TryUnequipMountToInventory(WorldClient client, int mountId)
        {
            var mount = client?.Character?.EquippedMount;
            if (mount == null || mount.Id != mountId)
                return false;

            bool wasRiding = client.Character.IsRiding;
            if (wasRiding)
                mount.UnApplyMountEffects(client.Character, false);

            client.Character.IsRiding = false;
            client.Character.EquippedMount = null;
            RefreshCharacterVisualState(client);
            client.Send(new MountUnSetMessage());
            client.Send(new MountRidingMessage(false));

            var certificate = CreateMountCertificate(client, mount);
            if (certificate == null)
            {
                client.Character.EquippedMount = mount;
                client.Character.IsRiding = wasRiding;
                if (wasRiding)
                    mount.ApplyMountEffects(client.Character, false);

                RefreshCharacterVisualState(client);
                client.Send(new MountSetMessage(mount.GetClientData()));
                client.Send(new MountXpRatioMessage((sbyte)Math.Max(0, Math.Min(90, (int)mount.Record.GivenExperience))));
                client.Send(new MountRidingMessage(client.Character.IsRiding));
                RefreshMountInventoryDisplay(client);
                return false;
            }

            RefreshCharacterVisualState(client);
            RefreshMountInventoryDisplay(client);
            PersistCharacterState(client);
            return true;
        }

        private static void PaddockToInventory(WorldClient client, int mountId)
        {
            var mount = GetPaddockMount(client, mountId);
            if (mount == null)
                return;

            mount.Record.IsInStable = 0;
            mount.Record.PaddockId = client.Character.Map.Id;
            if (CreateMountCertificate(client, mount) != null)
                client.Send(new ExchangeMountPaddockRemoveMessage(mount.Id));
            RefreshMountInventoryDisplay(client);
            PersistCharacterState(client);
        }

        private static void InventoryToEquip(WorldClient client, int itemUid)
        {
            var item = GetCertificateItem(client, itemUid);
            var mount = ResolveInventoryMount(client, item);
            if (mount == null || !CanEquipMount(client, mount))
                return;

            EquipMountFromInventoryCertificate(client, item, mount);
        }

        private static void InventoryToPaddock(WorldClient client, int itemUid)
        {
            var item = GetCertificateItem(client, itemUid);
            var mount = ResolveInventoryMount(client, item);
            if (mount == null || !CanStoreMountInCurrentPaddock(client))
                return;

            MountCertificateFactory.EnsureMountIsRideable(mount);
            mount.Record.OwnerId = client.Character.Id;
            mount.Record.OwnerName = client.Character.Name;
            mount.Record.IsInStable = 0;
            mount.Record.PaddockId = client.Character.Map.Id;
            mount.Record.StoredSince = null;
            client.Character.LastTargetedMountId = mount.Id;
            client.Character.LastViewedMountId = mount.Id;
            MountManager.Instance.Save(mount);
            client.Character.Inventory.RemoveItem(item, 1);
            RemoveGhostCertificates(client, mount.Id);
            client.Send(new ExchangeMountPaddockAddMessage(mount.GetClientData()));
            RefreshMountInventoryDisplay(client);
            PersistCharacterState(client);
        }

        private static Mount GetStableMount(WorldClient client, int mountId)
        {
            return MountManager.Instance.GetStableMounts(client.Character.Id, client.Character.Account?.Id)
                .FirstOrDefault(x => x.Id == mountId);
        }

        private static Mount GetPaddockMount(WorldClient client, int mountId)
        {
            return GetAccessiblePaddockMounts(client)
                .FirstOrDefault(x => x.Id == mountId && CanManagePaddockMount(client, x));
        }

        private static BasePlayerItem GetCertificateItem(WorldClient client, int itemUid)
        {
            var item = client.Character.Inventory.GetItemUid(itemUid);
            if (item == null || item.Template == null)
                return null;

            return MountManager.Instance.IsMountCertificateTemplate(item.Template.Id) ? item : null;
        }

        private static Mount ResolveInventoryMount(WorldClient client, BasePlayerItem item)
        {
            if (item == null)
                return null;

            MountCertificateFactory.TryNormalizeImportedCertificate(item, client.Character.Id);
            return MountCertificateFactory.ResolveMount(item, client.Character.Id);
        }

        private static Mount GetMountFromStockContext(WorldClient client)
        {
            if (client?.Character == null)
                return null;

            if (client.Character.IsInTrade() && client.Character.Trade is MountStockTrade currentStock && currentStock.Mount != null && IsOwnedByCharacter(client, currentStock.Mount))
                return currentStock.Mount;

            var candidateIds = new[] { client.Character.LastViewedMountId, client.Character.LastTargetedMountId }
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .Distinct()
                .ToList();

            foreach (var candidateId in candidateIds)
            {
                var targeted = MountManager.Instance.GetMount(candidateId);
                if (targeted == null || !IsOwnedByCharacter(client, targeted))
                    continue;

                if (client.Character.EquippedMount?.Id == targeted.Id ||
                    IsStableMountForCharacter(client, targeted) ||
                    IsPaddockMountForCharacter(client, targeted))
                    return targeted;
            }

            if (client.Character.EquippedMount != null && IsOwnedByCharacter(client, client.Character.EquippedMount))
                return client.Character.EquippedMount;

            return null;
        }

        private static Mount GetViewedMount(WorldClient client)
        {
            if (client?.Character == null)
                return null;

            var candidateIds = new[] { client.Character.LastViewedMountId, client.Character.LastTargetedMountId }
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .Distinct()
                .ToList();

            foreach (var candidateId in candidateIds)
            {
                var mount = MountManager.Instance.GetMount(candidateId);
                if (mount == null)
                    continue;

                if (!IsOwnedByCharacter(client, mount) && client.Character.EquippedMount?.Id != mount.Id && !HasActiveCertificateForMount(client, mount))
                    continue;

                if (client.Character.EquippedMount?.Id == mount.Id ||
                    IsStableMountForCharacter(client, mount) ||
                    IsPaddockMountForCharacter(client, mount) ||
                    HasActiveCertificateForMount(client, mount))
                    return mount;
            }

            if (client.Character.EquippedMount != null && IsOwnedByCharacter(client, client.Character.EquippedMount))
                return client.Character.EquippedMount;

            return null;
        }

        private static void SendSelectedMountData(WorldClient client)
        {
            var selectedMount = GetViewedMount(client);
            if (selectedMount != null)
                client.Send(new MountDataMessage(selectedMount.GetClientData()));
        }

        private sealed class PublicPaddockVisualEntry
        {
            public int ActorId { get; set; }
            public int MountId { get; set; }

            // Cellule d'affichage / position actuelle de la monture
            public short CellId { get; set; }

            // Cellule de base IMMUTABLE = CellIdSpawnMount
            public short BaseCellId { get; set; }

            // Première cellule walkable utilisée pour démarrer les déplacements
            public short MovementBaseCellId { get; set; }

            public DirectionsEnum Direction { get; set; }
        }

        private sealed class PublicPaddockVisualState
        {
            public int MapId { get; set; }
            public CancellationTokenSource Cancellation { get; set; }
            public Dictionary<int, PublicPaddockVisualEntry> Entries { get; } = new Dictionary<int, PublicPaddockVisualEntry>();
        }

        private static readonly object PublicPaddockVisualSync = new object();
        private static readonly Dictionary<int, PublicPaddockVisualState> PublicPaddockVisualStates = new Dictionary<int, PublicPaddockVisualState>();
        private static readonly Random PublicPaddockVisualRandom = new Random();

        private static bool ShouldDisplayPaddockVisuals(WorldClient client)
        {
            return GetCurrentPaddock(client) != null;
        }

        private static int GetPublicPaddockActorId(Mount mount)
        {
            return mount == null ? 0 : -System.Math.Abs(mount.Id);
        }

        private static List<Mount> GetPublicPaddockMounts(WorldClient client)
        {
            return GetVisiblePaddockMounts(client);
        }

        private static HashSet<short> BuildPublicPaddockReservedCells(WorldClient client, IEnumerable<short> reservedCells = null)
        {
            var reserved = new HashSet<short>(reservedCells ?? Enumerable.Empty<short>());
            var map = client?.Character?.Map;
            if (map == null)
                return reserved;

            if (client.Character?.Cell != null)
                reserved.Add(client.Character.Cell.Id);

            foreach (var actor in map.RolePlayActors ?? Enumerable.Empty<Game.Actors.RolePlayActor>())
            {
                try
                {
                    var info = actor?.GetGameRolePlayActorInformations();
                    if (info?.disposition != null)
                        reserved.Add(info.disposition.cellId);
                }
                catch
                {
                }
            }

            return reserved;
        }

        private static bool IsValidPublicPaddockCell(WorldClient client, short cellId, HashSet<short> reservedCells = null)
        {
            var map = client?.Character?.Map;
            if (map?.Cells == null || cellId < 0 || cellId >= map.Cells.Length)
                return false;

            var cell = map.Cells[cellId];
            if (!cell.Walkable || cell.NonWalkableDuringFight || cell.NonWalkableDuringRP || cell.FarmCell)
                return false;

            return reservedCells == null || !reservedCells.Contains(cellId);
        }

        private static short? GetConfiguredPaddockSpawnCell(WorldClient client, HashSet<short> reservedCells = null)
        {
            short configuredCell;
            if (!PaddockManager.Instance.TryGetConfiguredSpawnCell(client?.Character?.Map?.Id ?? 0, out configuredCell))
                return null;

            var map = client?.Character?.Map;
            if (map?.Cells == null || configuredCell < 0 || configuredCell >= map.Cells.Length)
                return null;

            // IMPORTANT :
            // Pour les montures d'enclos, on accepte la cellule configurée telle quelle.
            // On ne bloque pas sur Walkable / NonWalkableDuringRP / FarmCell,
            // sinon certaines CellIdSpawnMount valides côté contenu ne s'affichent jamais.
            return configuredCell;
        }

        private static bool TryGetExactConfiguredPaddockSpawnCell(WorldClient client, IEnumerable<short> reservedCells, out short cellId)
        {
            cellId = 0;

            var configuredCell = GetConfiguredPaddockSpawnCell(client, null);
            if (!configuredCell.HasValue)
                return false;

            cellId = configuredCell.Value;
            return true;
        }

        private static IEnumerable<short> GetCellsAroundConfiguredPaddockSpawn(WorldClient client, short anchorCell, HashSet<short> reservedCells)
        {
            if (IsValidPublicPaddockCell(client, anchorCell, reservedCells))
                yield return anchorCell;

            var anchorPoint = new MapPoint(anchorCell);
            var directions = new[]
            {
                DirectionsEnum.DIRECTION_EAST,
                DirectionsEnum.DIRECTION_WEST,
                DirectionsEnum.DIRECTION_NORTH_EAST,
                DirectionsEnum.DIRECTION_NORTH_WEST,
                DirectionsEnum.DIRECTION_SOUTH_EAST,
                DirectionsEnum.DIRECTION_SOUTH_WEST,
                DirectionsEnum.DIRECTION_NORTH,
                DirectionsEnum.DIRECTION_SOUTH
            };

            for (short step = 1; step <= 5; step++)
            {
                foreach (var direction in directions)
                {
                    var point = anchorPoint.GetCellInDirection(direction, step);
                    if (point == null)
                        continue;

                    if (IsValidPublicPaddockCell(client, point.CellId, reservedCells))
                        yield return point.CellId;
                }
            }
        }

        private static List<short> GetPublicPaddockAnchorCells(WorldClient client)
        {
            var anchors = new List<short>();
            var map = client?.Character?.Map;
            if (map == null)
                return anchors;

            short configuredCell;
            if (PaddockManager.Instance.TryGetConfiguredSpawnCell(map.Id, out configuredCell))
                anchors.Add(configuredCell);

            var interactiveIds = (map.Interactives ?? new List<Game.Maps.Interactives.Interactive>())
                .Where(x => x != null && (x.Type == 120 || (x.Skills != null && x.Skills.Any(skill => skill == 175 || skill == 176 || skill == 177 || skill == 178))))
                .Select(x => (uint)x.Element)
                .ToHashSet();

            if (interactiveIds.Count == 0)
                return anchors.Distinct().ToList();

            anchors.AddRange((map.Elements ?? new List<Element>())
                .Where(x => x != null && interactiveIds.Contains(x.Id) && x.Cell >= 0 && x.Cell < 560)
                .Select(x => x.Cell)
                .Distinct());

            return anchors.Distinct().ToList();
        }

        private static IEnumerable<short> GetCellsBehindPaddockInteractive(WorldClient client, short anchorCell, HashSet<short> reservedCells)
        {
            var anchorPoint = new MapPoint(anchorCell);

            // "Derrière" l'interactive d'enclos = à l'intérieur de l'enclos.
            // On évite volontairement NORTH pour ne jamais placer la monture "au-dessus" de la porte.
            var preferredDirections = new[]
            {
                DirectionsEnum.DIRECTION_NORTH_WEST,
                DirectionsEnum.DIRECTION_NORTH_EAST,
                DirectionsEnum.DIRECTION_WEST,
                DirectionsEnum.DIRECTION_EAST
            };

            for (short step = 1; step <= 5; step++)
            {
                foreach (var direction in preferredDirections)
                {
                    var point = anchorPoint.GetCellInDirection(direction, step);
                    if (point == null)
                        continue;

                    if (IsValidPublicPaddockCell(client, point.CellId, reservedCells))
                        yield return point.CellId;
                }
            }
        }

        private static HashSet<short> GetPreferredPublicPaddockAreaCells(WorldClient client)
        {
            var result = new HashSet<short>();
            var map = client?.Character?.Map;
            if (map == null)
                return result;

            short configuredSpawnCell;
            bool hasConfiguredSpawn = PaddockManager.Instance.TryGetConfiguredSpawnCell(map.Id, out configuredSpawnCell);

            // Zone "intérieure" basée sur les interactives d'enclos
            var interactiveArea = new HashSet<short>();

            var interactiveIds = (map.Interactives ?? new List<Game.Maps.Interactives.Interactive>())
                .Where(x => x != null && (x.Type == 120 || (x.Skills != null && x.Skills.Any(skill => skill == 175 || skill == 176 || skill == 177 || skill == 178))))
                .Select(x => (uint)x.Element)
                .ToHashSet();

            if (interactiveIds.Count > 0)
            {
                var anchorCells = (map.Elements ?? new List<Element>())
                    .Where(x => x != null && interactiveIds.Contains(x.Id) && x.Cell >= 0 && x.Cell < 560)
                    .Select(x => x.Cell)
                    .Distinct()
                    .ToList();

                foreach (var anchorCell in anchorCells)
                {
                    foreach (var candidate in GetCellsBehindPaddockInteractive(client, anchorCell, null))
                    {
                        if (IsValidPublicPaddockCell(client, candidate, null))
                            interactiveArea.Add(candidate);
                    }
                }
            }

            // Si on a un CellIdSpawnMount, on construit une petite zone autour,
            // mais on l'intersecte avec la vraie zone intérieure si elle existe.
            if (hasConfiguredSpawn)
            {
                result.Add(configuredSpawnCell);

                var anchorPoint = new MapPoint(configuredSpawnCell);
                var directions = new[]
                {
            DirectionsEnum.DIRECTION_EAST,
            DirectionsEnum.DIRECTION_WEST,
            DirectionsEnum.DIRECTION_NORTH_EAST,
            DirectionsEnum.DIRECTION_NORTH_WEST,
            DirectionsEnum.DIRECTION_SOUTH_EAST,
            DirectionsEnum.DIRECTION_SOUTH_WEST,
            DirectionsEnum.DIRECTION_NORTH,
            DirectionsEnum.DIRECTION_SOUTH
        };

                for (short step = 1; step <= 2; step++)
                {
                    foreach (var direction in directions)
                    {
                        var point = anchorPoint.GetCellInDirection(direction, step);
                        if (point == null)
                            continue;

                        if (!IsValidPublicPaddockCell(client, point.CellId, null))
                            continue;

                        if (interactiveArea.Count > 0 && !interactiveArea.Contains(point.CellId))
                            continue;

                        result.Add(point.CellId);
                    }
                }

                // Si on a une vraie zone intérieure issue des interactives,
                // on garde seulement l'intersection + la cellule de spawn.
                if (interactiveArea.Count > 0)
                {
                    result.Add(configuredSpawnCell);
                    return result;
                }

                return result;
            }

            foreach (var cellId in interactiveArea)
                result.Add(cellId);

            return result;
        }

        private static DirectionsEnum GetPreferredPublicPaddockDirection(WorldClient client, short cellId)
        {
            var configuredSpawn = GetConfiguredPaddockSpawnCell(client, null);
            if (configuredSpawn.HasValue && configuredSpawn.Value != cellId)
            {
                try
                {
                    return new MapPoint(configuredSpawn.Value).OrientationTo(new MapPoint(cellId));
                }
                catch
                {
                }
            }

            var anchorCells = GetPublicPaddockAnchorCells(client);
            if (anchorCells.Count > 0)
            {
                var anchorCell = anchorCells[0];
                if (anchorCell >= 0 && anchorCell < 560 && anchorCell != cellId)
                {
                    try
                    {
                        return new MapPoint(anchorCell).OrientationTo(new MapPoint(cellId));
                    }
                    catch
                    {
                    }
                }
            }

            return DirectionsEnum.DIRECTION_SOUTH;
        }

        private static GameRolePlayActorInformations BuildPublicPaddockActorInfo(Mount mount, PublicPaddockVisualEntry entry)
        {
            if (mount?.Template == null || entry == null || string.IsNullOrWhiteSpace(mount.Template.LookAsString))
                return null;

            var look = EntityManager.Instance.GetActorLook(mount.Template.LookAsString);
            if (look == null)
                return null;

            return new GameRolePlayMountInformations(
                entry.ActorId,
                look.Clone().GetEntityLook(),
                new EntityDispositionInformations(entry.CellId, (sbyte)entry.Direction),
                mount.Name,
                mount.Record.OwnerName ?? string.Empty,
                (byte)System.Math.Max(1, System.Math.Min(100, mount.Level)));
        }

        private static void CancelPublicPaddockLoopLocked(PublicPaddockVisualState state)
        {
            if (state?.Cancellation == null)
                return;

            try
            {
                state.Cancellation.Cancel();
                state.Cancellation.Dispose();
            }
            catch
            {
            }
            finally
            {
                state.Cancellation = null;
            }
        }

        private static void EnsurePublicPaddockLoopLocked(int characterId, PublicPaddockVisualState state)
        {
            if (state == null)
                return;

            if (state.Cancellation != null && !state.Cancellation.IsCancellationRequested)
                return;

            state.Cancellation = new CancellationTokenSource();
            var token = state.Cancellation.Token;
            Task.Run(async () => await RunPublicPaddockMovementLoop(characterId, token));
        }

        private static async Task RunPublicPaddockMovementLoop(int characterId, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(30000, token);
                }
                catch
                {
                    break;
                }

                var client = CharacterManager.Instance.GetCharacter(characterId)?.Client;
                if (client?.Character?.Map == null || !ShouldDisplayPaddockVisuals(client))
                    break;

                PublicPaddockVisualEntry selectedEntry = null;
                List<PublicPaddockVisualEntry> snapshot;

                lock (PublicPaddockVisualSync)
                {
                    PublicPaddockVisualState state;
                    if (!PublicPaddockVisualStates.TryGetValue(characterId, out state) ||
                        state == null ||
                        state.Cancellation == null ||
                        state.Cancellation.Token != token ||
                        state.Entries.Count == 0 ||
                        state.MapId != client.Character.Map.Id)
                        break;

                    snapshot = state.Entries.Values
                        .Select(x => new PublicPaddockVisualEntry
                        {
                            ActorId = x.ActorId,
                            MountId = x.MountId,
                            CellId = x.CellId,
                            BaseCellId = x.BaseCellId,
                            MovementBaseCellId = x.MovementBaseCellId,
                            Direction = x.Direction
                        })
                        .ToList();

                    selectedEntry = snapshot[PublicPaddockVisualRandom.Next(snapshot.Count)];
                }

                if (selectedEntry == null)
                    continue;

                var allowedArea = GetPreferredPublicPaddockAreaCells(client);
                if (allowedArea.Count == 0)
                    continue;

                short startCell = selectedEntry.CellId;

                // La monture doit partir de sa cellule actuelle,
                // pas de la base, sinon effet de téléportation.
                if (!allowedArea.Contains(startCell) || !IsValidPublicPaddockCell(client, startCell, null))
                    startCell = selectedEntry.MovementBaseCellId;

                if (!allowedArea.Contains(startCell) || !IsValidPublicPaddockCell(client, startCell, null))
                    continue;

                var candidates = GetAdjacentPaddockMovementCells(client, startCell, allowedArea)
                    .Where(x => x != startCell)
                    .ToList();

                if (candidates.Count == 0)
                    continue;

                Path path = null;
                try
                {
                    var pathFinder = new Pathfinder(client.Character.Map.CellsInfoProvider);

                    for (int attempt = 0; attempt < 8; attempt++)
                    {
                        short targetCell;
                        lock (PublicPaddockVisualSync)
                            targetCell = candidates[PublicPaddockVisualRandom.Next(candidates.Count)];

                        path = pathFinder.FindPath(startCell, targetCell, false);
                        if (path != null && !path.IsEmpty() && path.MPCost > 0 && path.EndCell == targetCell)
                            break;

                        path = null;
                    }
                }
                catch
                {
                    path = null;
                }

                if (path == null)
                    continue;

                lock (PublicPaddockVisualSync)
                {
                    PublicPaddockVisualState state;
                    PublicPaddockVisualEntry liveEntry;
                    if (!PublicPaddockVisualStates.TryGetValue(characterId, out state) ||
                        state == null ||
                        !state.Entries.TryGetValue(selectedEntry.MountId, out liveEntry))
                        continue;

                    liveEntry.BaseCellId = selectedEntry.BaseCellId;
                    liveEntry.MovementBaseCellId = selectedEntry.MovementBaseCellId;
                    liveEntry.CellId = path.EndCell;
                    liveEntry.Direction = path.GetEndCellDirection();
                }

                ContextHandler.SendGameMapMovementMessage(client, selectedEntry.ActorId, path.GetServerPathKeys());
            }

            lock (PublicPaddockVisualSync)
            {
                PublicPaddockVisualState state;
                if (PublicPaddockVisualStates.TryGetValue(characterId, out state) &&
                    state != null &&
                    state.Cancellation != null &&
                    state.Cancellation.Token == token)
                    state.Cancellation = null;
            }
        }

        private static List<GameRolePlayActorInformations> PreparePublicPaddockVisuals(WorldClient client, bool sendUpdates, bool initialMapLoad)
        {
            var result = new List<GameRolePlayActorInformations>();
            var removedEntries = new List<PublicPaddockVisualEntry>();

            if (client?.Character == null)
                return result;

            int characterId = client.Character.Id;
            int mapId = client.Character.Map != null ? client.Character.Map.Id : 0;
            bool isPaddockMap = ShouldDisplayPaddockVisuals(client);
            var mounts = isPaddockMap ? GetPublicPaddockMounts(client) : new List<Mount>();

            lock (PublicPaddockVisualSync)
            {
                PublicPaddockVisualState state;
                if (!PublicPaddockVisualStates.TryGetValue(characterId, out state) || state == null)
                {
                    state = new PublicPaddockVisualState();
                    PublicPaddockVisualStates[characterId] = state;
                }

                if (state.MapId != mapId)
                {
                    removedEntries.AddRange(state.Entries.Values);
                    CancelPublicPaddockLoopLocked(state);
                    state.Entries.Clear();
                    state.MapId = mapId;
                }

                var desiredMountIds = new HashSet<int>(mounts.Select(x => x.Id));
                foreach (var obsoleteEntry in state.Entries.Where(x => !desiredMountIds.Contains(x.Key)).Select(x => x.Value).ToList())
                {
                    removedEntries.Add(obsoleteEntry);
                    state.Entries.Remove(obsoleteEntry.MountId);
                }

                if (!isPaddockMap || mounts.Count == 0)
                {
                    CancelPublicPaddockLoopLocked(state);
                    if (state.Entries.Count == 0)
                        PublicPaddockVisualStates.Remove(characterId);
                }
                else
                {
                    var allowedArea = GetPreferredPublicPaddockAreaCells(client);

                    short baseCell;
                    if (!TryGetExactConfiguredPaddockSpawnCell(client, null, out baseCell))
                        baseCell = 0;

                    short movementBaseCell = GetWalkableMovementBaseCell(client, baseCell);

                    foreach (var mount in mounts)
                    {
                        PublicPaddockVisualEntry entry;
                        if (!state.Entries.TryGetValue(mount.Id, out entry))
                        {
                            entry = new PublicPaddockVisualEntry
                            {
                                ActorId = GetPublicPaddockActorId(mount),
                                MountId = mount.Id,
                                BaseCellId = baseCell,
                                MovementBaseCellId = movementBaseCell,
                                CellId = baseCell,
                                Direction = GetPreferredPublicPaddockDirection(client, baseCell)
                            };
                            state.Entries[mount.Id] = entry;
                        }
                        else
                        {
                            entry.BaseCellId = baseCell;
                            entry.MovementBaseCellId = movementBaseCell;

                            // IMPORTANT :
                            // on ne téléporte plus systématiquement sur la base.
                            // On garde la position actuelle si elle reste valide et dans la zone.
                            var keepCurrentCell =
                                entry.CellId >= 0 &&
                                (allowedArea.Count == 0 || allowedArea.Contains(entry.CellId)) &&
                                (entry.CellId == baseCell || IsValidPublicPaddockCell(client, entry.CellId, null));

                            if (!keepCurrentCell)
                            {
                                entry.CellId = baseCell;
                                entry.Direction = GetPreferredPublicPaddockDirection(client, baseCell);
                            }
                        }

                        var info = BuildPublicPaddockActorInfo(mount, entry);
                        if (info != null)
                            result.Add(info);
                    }

                    EnsurePublicPaddockLoopLocked(characterId, state);
                }
            }

            if (sendUpdates)
            {
                foreach (var removedEntry in removedEntries.DistinctBy(x => x.ActorId))
                    client.Send(new GameContextRemoveElementMessage(removedEntry.ActorId));

                if (!initialMapLoad)
                {
                    foreach (var info in result)
                        client.Send(new GameRolePlayShowActorMessage(info));
                }
            }

            return result;
        }

        public static IEnumerable<GameRolePlayActorInformations> GetPublicPaddockRolePlayActors(WorldClient client)
        {
            return PreparePublicPaddockVisuals(client, false, true);
        }

        public static void SyncPublicPaddockMountVisuals(WorldClient client, bool initialMapLoad = false)
        {
            PreparePublicPaddockVisuals(client, true, initialMapLoad);
        }

        public static void ClearPublicPaddockMountVisuals(WorldClient client, bool silent = false)
        {
            if (client?.Character == null)
                return;

            PublicPaddockVisualState state = null;
            lock (PublicPaddockVisualSync)
            {
                if (!PublicPaddockVisualStates.TryGetValue(client.Character.Id, out state) || state == null)
                    return;

                PublicPaddockVisualStates.Remove(client.Character.Id);
                CancelPublicPaddockLoopLocked(state);
            }

            if (silent)
                return;

            foreach (var entry in state.Entries.Values.DistinctBy(x => x.ActorId))
                client.Send(new GameContextRemoveElementMessage(entry.ActorId));
        }

        public static void OpenPaddockPanel(WorldClient client)
        {
            RefreshPaddockPanel(client);
        }

        private static void RefreshPaddockPanel(WorldClient client, bool includeSelectedMountData = true, bool refreshInventory = true)
        {
            if (client?.Character?.Map == null)
                return;

            var malformedStableMounts = MountManager.Instance.GetOwnerMounts(client.Character.Id, client.Character.Account?.Id)
                .Where(x => x != null && x.Record != null && x.Record.IsInStable == 1 && (x.Record.PaddockId.HasValue || x.Record.StoredSince.HasValue))
                .ToArray();

            foreach (var malformedStableMount in malformedStableMounts)
            {
                malformedStableMount.Record.PaddockId = null;
                malformedStableMount.Record.StoredSince = null;
                MountManager.Instance.Save(malformedStableMount);
            }

            var ghostCertificates = client.Character.Inventory.GetItems()
                .Where(x => x != null && x.Template != null && MountManager.Instance.IsMountCertificateTemplate(x.Template.Id))
                .Where(x => !MountCertificateFactory.IsActiveCertificateItem(x, client.Character.Id))
                .ToArray();

            var removedGhostCertificates = false;
            foreach (var ghostCertificate in ghostCertificates)
            {
                client.Character.Inventory.RemoveItem(ghostCertificate, System.Math.Max(1, ghostCertificate.Stack));
                removedGhostCertificates = true;
            }

            if (removedGhostCertificates)
                CharacterManager.Instance.Save(client.Character);

            if (refreshInventory)
            {
                Handlers.Characters.Inventory.InventoryHandler.SendInventoryContentMessage(client);
                Handlers.Characters.Inventory.InventoryHandler.SendInventoryWeightMessage(client);
            }

            var stableMounts = MountManager.Instance.GetStableMounts(client.Character.Id, client.Character.Account?.Id)
                .Select(x => x.GetClientData())
                .ToArray();

            var paddock = GetCurrentPaddock(client);
            var visiblePaddockMounts = GetVisiblePaddockMounts(client);
            var accessiblePaddockMounts = GetAccessiblePaddockMounts(client);
            var paddockedMounts = accessiblePaddockMounts
                .Select(x => x.GetClientData())
                .ToArray();

            var paddockContent = paddock != null
                ? paddock.GetPropertiesForClient(client.Character, visiblePaddockMounts)
                : new PaddockContentInformations(
                    maxOutdoorMount: (short)System.Math.Max(5, paddockedMounts.Length),
                    maxItems: 0,
                    paddockId: client.Character.Map.Id,
                    worldX: (short)client.Character.Map.Position.X,
                    worldY: (short)client.Character.Map.Position.Y,
                    mapId: client.Character.Map.Id,
                    mountsInformations: visiblePaddockMounts.Select(x => x.GetInformationsForPaddock()).ToArray());

            client.Send(new PaddockPropertiesMessage(paddockContent));
            client.Send(new GameDataPaddockObjectListAddMessage(new PaddockItem[0]));
            client.Send(new ExchangeStartOkMountMessage(stableMounts, paddockedMounts));

            if (includeSelectedMountData)
                SendSelectedMountData(client);

            SyncPublicPaddockMountVisuals(client);
            client.Character.OnLookRefreshed();
        }
        
        private static short GetWalkableMovementBaseCell(WorldClient client, short baseCell)
        {
            var map = client?.Character?.Map;
            if (map?.Cells == null || baseCell < 0 || baseCell >= map.Cells.Length)
                return baseCell;

            var allowedArea = GetPreferredPublicPaddockAreaCells(client);

            if (allowedArea.Count == 0)
                allowedArea.Add(baseCell);

            if (allowedArea.Contains(baseCell) && IsValidPublicPaddockCell(client, baseCell, null))
                return baseCell;

            var anchorPoint = new MapPoint(baseCell);
            var directions = new[]
            {
        DirectionsEnum.DIRECTION_EAST,
        DirectionsEnum.DIRECTION_WEST,
        DirectionsEnum.DIRECTION_NORTH_EAST,
        DirectionsEnum.DIRECTION_NORTH_WEST,
        DirectionsEnum.DIRECTION_SOUTH_EAST,
        DirectionsEnum.DIRECTION_SOUTH_WEST,
        DirectionsEnum.DIRECTION_NORTH,
        DirectionsEnum.DIRECTION_SOUTH
    };

            for (short step = 1; step <= 3; step++)
            {
                foreach (var direction in directions)
                {
                    var point = anchorPoint.GetCellInDirection(direction, step);
                    if (point == null)
                        continue;

                    if (!allowedArea.Contains(point.CellId))
                        continue;

                    if (IsValidPublicPaddockCell(client, point.CellId, null))
                        return point.CellId;
                }
            }

            // fallback : première cellule valide DANS la zone de l'enclos
            foreach (var cellId in allowedArea)
            {
                if (IsValidPublicPaddockCell(client, cellId, null))
                    return cellId;
            }

            return baseCell;
        }
        private static List<short> GetPaddockMovementCellsAroundBase(WorldClient client, short movementBaseCell)
        {
            var result = new List<short>();
            var map = client?.Character?.Map;
            if (map?.Cells == null || movementBaseCell < 0 || movementBaseCell >= map.Cells.Length)
                return result;

            var allowedArea = GetPreferredPublicPaddockAreaCells(client);
            if (allowedArea.Count == 0)
                return result;

            var anchorPoint = new MapPoint(movementBaseCell);
            var directions = new[]
            {
        DirectionsEnum.DIRECTION_EAST,
        DirectionsEnum.DIRECTION_WEST,
        DirectionsEnum.DIRECTION_NORTH_EAST,
        DirectionsEnum.DIRECTION_NORTH_WEST,
        DirectionsEnum.DIRECTION_SOUTH_EAST,
        DirectionsEnum.DIRECTION_SOUTH_WEST,
        DirectionsEnum.DIRECTION_NORTH,
        DirectionsEnum.DIRECTION_SOUTH
    };

            // Zone courte + limitée strictement à la zone d'enclos
            for (short step = 1; step <= 2; step++)
            {
                foreach (var direction in directions)
                {
                    var point = anchorPoint.GetCellInDirection(direction, step);
                    if (point == null)
                        continue;

                    if (!allowedArea.Contains(point.CellId))
                        continue;

                    if (IsValidPublicPaddockCell(client, point.CellId, null))
                        result.Add(point.CellId);
                }
            }

            // fallback : si aucune cellule locale valide, rester uniquement dans la zone autorisée
            if (result.Count == 0)
            {
                foreach (var cellId in allowedArea)
                {
                    if (IsValidPublicPaddockCell(client, cellId, null))
                        result.Add(cellId);
                }
            }

            return result.Distinct().ToList();
        }
        private static List<short> GetAdjacentPaddockMovementCells(WorldClient client, short currentCell, HashSet<short> allowedArea)
        {
            var result = new List<short>();
            var map = client?.Character?.Map;
            if (map?.Cells == null || currentCell < 0 || currentCell >= map.Cells.Length)
                return result;

            var anchorPoint = new MapPoint(currentCell);
            var directions = new[]
            {
        DirectionsEnum.DIRECTION_EAST,
        DirectionsEnum.DIRECTION_WEST,
        DirectionsEnum.DIRECTION_NORTH_EAST,
        DirectionsEnum.DIRECTION_NORTH_WEST,
        DirectionsEnum.DIRECTION_SOUTH_EAST,
        DirectionsEnum.DIRECTION_SOUTH_WEST,
        DirectionsEnum.DIRECTION_NORTH,
        DirectionsEnum.DIRECTION_SOUTH
    };

            foreach (var direction in directions)
            {
                var point = anchorPoint.GetCellInDirection(direction, 1);
                if (point == null)
                    continue;

                if (allowedArea != null && allowedArea.Count > 0 && !allowedArea.Contains(point.CellId))
                    continue;

                if (IsValidPublicPaddockCell(client, point.CellId, null))
                    result.Add(point.CellId);
            }

            return result.Distinct().ToList();
        }
    }
}
