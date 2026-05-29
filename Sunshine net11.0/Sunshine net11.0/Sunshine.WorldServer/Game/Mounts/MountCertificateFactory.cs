using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Items;
using Sunshine.MySql.Database.World.Mounts;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Game.Items.Custom;
using Sunshine.WorldServer.Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Mounts
{
    public static class MountCertificateFactory
    {
        public static readonly TimeSpan StorageValidity = TimeSpan.FromDays(20);


        public static BasePlayerItem CreateGeneratedCertificate(int certificateTemplateId, int ownerId = 0, string ownerName = null, int validityDays = 20)
        {
            var mountTemplate = MountManager.Instance.GetTemplateByScrollId(certificateTemplateId);
            if (mountTemplate == null)
                return null;

            if (string.IsNullOrWhiteSpace(ownerName) && ownerId > 0)
            {
                var owner = CharacterManager.Instance.GetCharacter(ownerId);
                if (owner != null)
                    ownerName = owner.Name;
            }

            List<MountBonusRecord> bonuses;
            if (!MountManager.Instance.Bonuses.TryGetValue(mountTemplate.Id, out bonuses))
                bonuses = new List<MountBonusRecord>();

            var mountRecord = new MountRecord
            {
                Id = MountManager.Instance.GenerateId(),
                Name = "Dragodinde",
                Sex = (sbyte)(new Random(Guid.NewGuid().GetHashCode()).Next(0, 2)),
                TemplateId = mountTemplate.Id,
                Experience = 0,
                GivenExperience = 0,
                Stamina = 0,
                Maturity = mountTemplate.MaturityBase,
                Energy = mountTemplate.EnergyBase,
                Serenity = 0,
                Love = 0,
                ReproductionCount = 0,
                BehaviorsCSV = "9",
                OwnerId = ownerId > 0 ? (int?)ownerId : null,
                OwnerName = ownerName ?? string.Empty,
                PaddockId = null,
                IsInStable = 0,
                StoredSince = DateTime.Now
            };

            var mount = new Mount(
                mountRecord,
                mountTemplate,
                bonuses,
                Enumerable.Empty<ObjectItem>());

            EnsureMountIsRideable(mount);
            MountManager.Instance.Save(mount);
            return CreateCertificate(mount, validityDays);
        }

        public static BasePlayerItem CreateCertificate(Mount mount, int validityDays = 20)
        {
            if (mount == null)
                throw new ArgumentNullException(nameof(mount));

            int scrollId = mount.Template.ScrollId;

            ItemRecord itemTemplate;
            if (!ItemManager.Instance.Items.TryGetValue(scrollId, out itemTemplate))
                throw new KeyNotFoundException("Item template not found for mount certificate ScrollId: " + scrollId);

            var certificate = new MountCertificate(itemTemplate);
            certificate.Id = ItemManager.Instance.GenerateId();
            certificate.Position = CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED;
            certificate.Stack = 1;
            certificate.Effects = new List<Effect>();
            certificate.EffectSets = itemTemplate.EffectSets;
            certificate.RawObjectEffects = BuildEffects(mount, validityDays);

            return certificate;
        }

        public static List<ObjectEffect> BuildEffects(Mount mount, int validityDays = 20)
        {
            if (mount == null)
                throw new ArgumentNullException(nameof(mount));

            var result = new List<ObjectEffect>();

            ItemRecord itemTemplate;
            if (!ItemManager.Instance.Items.TryGetValue(mount.Template.ScrollId, out itemTemplate))
                throw new KeyNotFoundException("Item template not found for mount certificate ScrollId: " + mount.Template.ScrollId);

            short actionId = GetCertificateActionId(itemTemplate);

            // Important: building certificate effects must stay read-only.
            // The inventory mount slot reuses these effects for client display, and mutating
            // StoredSince here can incorrectly turn an equipped mount into a stored certificate
            // after a later save/reconnect cycle.
            var storedSince = mount.Record.StoredSince ?? DateTime.Now;

            var remaining = storedSince.AddDays(validityDays) - DateTime.Now;
            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;

            result.Add(new ObjectEffectMount(
                actionId,
                mount.Id,
                GetUnixTimestampMilliseconds(storedSince),
                (short)mount.Template.Id));

            if (!string.IsNullOrWhiteSpace(mount.Record.OwnerName))
                result.Add(new ObjectEffectString(987, mount.Record.OwnerName));

            result.Add(new ObjectEffectString(997, mount.Name ?? string.Empty));
            result.Add(ToDurationEffect(998, remaining));

            return result;
        }

        public static bool TryGetMountEffect(BasePlayerItem item, out ObjectEffectMount effect)
        {
            effect = null;

            if (item == null || item.RawObjectEffects == null)
                return false;

            effect = item.RawObjectEffects.OfType<ObjectEffectMount>().FirstOrDefault();
            return effect != null;
        }

        public static bool TryNormalizeImportedCertificate(BasePlayerItem item, int ownerId = 0, int validityDays = 20)
        {
            if (item == null || item.Template == null)
                return false;

            if (!MountManager.Instance.IsMountCertificateTemplate(item.Template.Id))
                return false;

            var mount = ResolveMount(item, ownerId);
            if (mount == null)
                return false;

            if (string.IsNullOrWhiteSpace(mount.Record.OwnerName) && ownerId > 0)
            {
                var owner = CharacterManager.Instance.GetCharacter(ownerId);
                if (owner != null)
                    mount.Record.OwnerName = owner.Name;
            }

            EnsureMountIsRideable(mount);
            item.RawObjectEffects = BuildEffects(mount, validityDays);
            if (item.Effects == null)
                item.Effects = new List<Effect>();

            return true;
        }

        public static Mount ResolveMount(BasePlayerItem item, int ownerId = 0)
        {
            if (item == null || item.Template == null)
                return null;

            Mount mount = null;
            ObjectEffectMount mountEffect;

            if (TryGetMountEffect(item, out mountEffect))
            {
                var directMount = MountManager.Instance.GetMount(mountEffect.mountId);
                if (directMount != null)
                    mount = directMount;
            }

            if (mount == null)
            {
                var template = MountManager.Instance.GetTemplateByScrollId(item.Template.Id);
                if (template != null)
                {
                    mount = MountManager.Instance.Mounts.Values
                        .Where(x => x != null && x.Record != null && x.Template != null && x.Template.Id == template.Id)
                        .Where(x => IsStoredCertificateMount(x, ownerId))
                        .OrderByDescending(x => x.Record.StoredSince ?? DateTime.MinValue)
                        .FirstOrDefault();
                }
            }

            if (mount == null && ownerId > 0)
            {
                mount = MountManager.Instance.GetOwnerMounts(ownerId)
                    .Where(x => IsStoredCertificateMount(x, ownerId))
                    .OrderByDescending(x => x.Record.StoredSince ?? DateTime.MinValue)
                    .FirstOrDefault();
            }

            return mount;
        }


        public static bool IsResolvedCertificateState(Mount mount)
        {
            return mount != null &&
                   mount.Record != null &&
                   mount.Record.StoredSince.HasValue &&
                   mount.Record.IsInStable <= 0 &&
                   !mount.Record.PaddockId.HasValue;
        }

        public static bool IsActiveCertificateItem(BasePlayerItem item, int ownerId = 0, bool normalize = true)
        {
            if (item == null || item.Template == null)
                return false;

            if (!MountManager.Instance.IsMountCertificateTemplate(item.Template.Id))
                return false;

            if (normalize)
            {
                try
                {
                    TryNormalizeImportedCertificate(item, ownerId);
                }
                catch
                {
                }
            }

            var mount = ResolveMount(item, ownerId);
            return IsResolvedCertificateState(mount);
        }

        private static bool IsStoredCertificateMount(Mount mount, int ownerId = 0)
        {
            if (mount == null || mount.Record == null)
                return false;

            if (!MatchesOwnerIdentity(mount, ownerId))
                return false;

            return IsResolvedCertificateState(mount);
        }

        private static bool MatchesOwnerIdentity(Mount mount, int ownerId)
        {
            if (mount == null || mount.Record == null)
                return false;

            if (ownerId <= 0)
                return true;

            var acceptedOwnerIds = new HashSet<int> { ownerId };
            var ownerCharacter = CharacterManager.Instance.GetCharacter(ownerId);
            if (ownerCharacter?.Account != null)
                acceptedOwnerIds.Add(ownerCharacter.Account.Id);

            var accountCharacter = CharacterManager.Instance.GetCharacterByAccount(ownerId);
            if (accountCharacter != null)
            {
                acceptedOwnerIds.Add(accountCharacter.Id);
                if (accountCharacter.Account != null)
                    acceptedOwnerIds.Add(accountCharacter.Account.Id);
            }

            return mount.Record.OwnerId.HasValue && acceptedOwnerIds.Contains(mount.Record.OwnerId.Value);
        }

        public static bool EnsureMountIsRideable(Mount mount, bool persist = true)
        {
            if (mount == null || mount.Record == null || mount.Template == null)
                return false;

            bool changed = false;

            var minimumMaturity = Math.Max(0, mount.MaturityForAdult);
            if (mount.Record.Maturity < minimumMaturity)
            {
                mount.Record.Maturity = minimumMaturity;
                changed = true;
            }

            if (mount.Record.Energy <= 0)
            {
                mount.Record.Energy = Math.Max(1, mount.EnergyMax);
                changed = true;
            }

            if (changed && persist)
                MountManager.Instance.Save(mount);

            return changed;
        }

        public static bool IsCertificateStillValid(BasePlayerItem item, int validityDays = 20)
        {
            ObjectEffectMount effect;
            if (!TryGetMountEffect(item, out effect))
                return false;

            var storedSince = FromUnknownUnix(effect.date);
            return storedSince.AddDays(validityDays) > DateTime.Now;
        }

        public static DateTime? TryGetStoredSince(BasePlayerItem item)
        {
            ObjectEffectMount effect;
            if (!TryGetMountEffect(item, out effect))
                return null;

            return FromUnknownUnix(effect.date);
        }

        private static short GetCertificateActionId(ItemRecord itemTemplate)
        {
            var templateEffect = itemTemplate.EffectsBase != null
                ? itemTemplate.EffectsBase.FirstOrDefault()
                : null;

            if (templateEffect != null)
                return (short)templateEffect.Id;

            return 995;
        }

        private static ObjectEffectDuration ToDurationEffect(short actionId, TimeSpan span)
        {
            if (span < TimeSpan.Zero)
                span = TimeSpan.Zero;

            var totalMinutes = (int)Math.Floor(span.TotalMinutes);
            short days = (short)(totalMinutes / (24 * 60));
            totalMinutes -= days * 24 * 60;
            short hours = (short)(totalMinutes / 60);
            totalMinutes -= hours * 60;
            short minutes = (short)Math.Max(0, totalMinutes);

            return new ObjectEffectDuration(actionId, days, hours, minutes);
        }

        private static double GetUnixTimestampMilliseconds(DateTime date)
        {
            return (date.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        private static DateTime FromUnknownUnix(double value)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            if (value > 1000000000000d)
                return epoch.AddMilliseconds(value).ToLocalTime();

            return epoch.AddSeconds(value).ToLocalTime();
        }
    }
}
