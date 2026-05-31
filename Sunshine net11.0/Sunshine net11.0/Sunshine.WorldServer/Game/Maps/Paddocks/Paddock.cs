using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Maps.Paddocks;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Guilds;
using Sunshine.WorldServer.Game.Mounts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Maps.Paddocks
{
    public class Paddock
    {
        public WorldMapPaddockRecord Record { get; }

        public Paddock(WorldMapPaddockRecord record)
        {
            Record = record;
        }

        public bool IsDirty { get; set; }

        public int Id => Record.Id;
        public int MapId => Record.MapId;
        public Game.Maps.Map Map => Record.Map;
        public int? GuildId
        {
            get => Record.GuildId;
            set
            {
                if (Record.GuildId == value)
                    return;

                Record.GuildId = value;
                IsDirty = true;
            }
        }

        public int? OwnerId
        {
            get => Record.OwnerId;
            set
            {
                if (Record.OwnerId == value)
                    return;

                Record.OwnerId = value;
                IsDirty = true;
            }
        }

        public uint MaxOutdoorMount
        {
            get => Record.MaxOutdoorMount;
            set
            {
                if (Record.MaxOutdoorMount == value)
                    return;

                Record.MaxOutdoorMount = value;
                IsDirty = true;
            }
        }

        public uint MaxItems
        {
            get => Record.MaxItems;
            set
            {
                if (Record.MaxItems == value)
                    return;

                Record.MaxItems = value;
                IsDirty = true;
            }
        }

        public bool Abandonned
        {
            get => Record.Abandonned;
            set
            {
                if (Record.Abandonned == value)
                    return;

                Record.Abandonned = value;
                IsDirty = true;
            }
        }

        public bool OnSale
        {
            get => Record.OnSale;
            set
            {
                if (Record.OnSale == value)
                    return;

                Record.OnSale = value;
                IsDirty = true;
            }
        }

        public bool Locked
        {
            get => Record.Locked;
            set
            {
                if (Record.Locked == value)
                    return;

                Record.Locked = value;
                IsDirty = true;
            }
        }

        public int Price
        {
            get => Record.Price;
            set
            {
                if (Record.Price == value)
                    return;

                Record.Price = value;
                IsDirty = true;
            }
        }

        public bool IsPublicPaddock => Record.isPublic.HasValue && Record.isPublic.Value;

        public int? TpCell
        {
            get => Record.TpCell;
            set
            {
                if (Record.TpCell == value)
                    return;

                Record.TpCell = value;
                IsDirty = true;
            }
        }

        public int? CellIdSpawnMount
        {
            get => Record.CellIdSpawnMount;
            set
            {
                if (Record.CellIdSpawnMount == value)
                    return;

                Record.CellIdSpawnMount = value;
                IsDirty = true;
            }
        }

        public Guild Guild => GuildId.HasValue ? GuildManager.Instance.GetGuild(GuildId.Value) : null;

        public bool HasExplicitOwner => PaddockManager.Instance.HasOwnerIdColumn && OwnerId.HasValue && OwnerId.Value > 0;

        public bool IsOwned => (GuildId.HasValue && GuildId.Value > 0) || HasExplicitOwner;

        public int SalePrice => IsPublicPaddock ? 0 : Math.Max(0, Price);

        public bool IsCharacterOwner(Character character)
        {
            if (character == null)
                return false;

            if (HasExplicitOwner)
                return OwnerId.Value == character.Id;

            return character.Guild != null &&
                   GuildId.HasValue &&
                   character.Guild.Id == GuildId.Value &&
                   character.GuildMember != null &&
                   character.GuildMember.IsBoss;
        }

        public bool IsGuildMember(Character character)
        {
            return character != null &&
                   character.Guild != null &&
                   GuildId.HasValue &&
                   character.Guild.Id == GuildId.Value &&
                   character.GuildMember != null;
        }

        public bool CanUsePaddock(Character character)
        {
            if (character == null)
                return false;

            if (IsPublicPaddock)
                return true;

            if (Abandonned || OnSale)
                return false;

            if (IsCharacterOwner(character))
                return true;

            return IsGuildMember(character) &&
                   (character.GuildMember.IsBoss ||
                    character.GuildMember.HasRight(GuildRightsBitEnum.GUILD_RIGHT_USE_PADDOCKS) ||
                    character.GuildMember.HasRight(GuildRightsBitEnum.GUILD_RIGHT_ORGANIZE_PADDOCKS));
        }

        public bool CanTakeOtherMounts(Character character)
        {
            if (character == null || !IsGuildMember(character))
                return false;

            return IsCharacterOwner(character) ||
                   character.GuildMember.IsBoss ||
                   character.GuildMember.HasRight(GuildRightsBitEnum.GUILD_RIGHT_TAKE_OTHERS_MOUNTS_IN_PADDOCKS) ||
                   character.GuildMember.HasRight(GuildRightsBitEnum.GUILD_RIGHT_ORGANIZE_PADDOCKS);
        }

        public bool ShouldDisplayBuySkill(Character character)
        {
            if (character == null)
                return false;

            if (IsPublicPaddock || Abandonned || !OnSale || Locked)
                return false;

            if (character.Guild == null || character.GuildMember == null)
                return false;

            if (GuildId.HasValue && character.Guild.Id == GuildId.Value)
                return false;

            if (HasExplicitOwner && OwnerId.Value == character.Id)
                return false;

            return true;
        }

        public bool CanBuy(Character character)
        {
            if (character == null || !ShouldDisplayBuySkill(character))
                return false;

            if (character.Guild == null || character.GuildMember == null)
                return false;

            if (SalePrice <= 0)
                return false;

            var maxGuildPaddocks = Math.Max(0, character.Guild.MaxPaddocks);
            if (maxGuildPaddocks > 0 && PaddockManager.Instance.CountByGuild(character.Guild.Id) >= maxGuildPaddocks)
                return false;

            return true;
        }

        public bool CanSell(Character character)
        {
            return !IsPublicPaddock && !Abandonned && !OnSale && IsOwned && IsCharacterOwner(character);
        }

        public bool CanModifySalePrice(Character character)
        {
            return !IsPublicPaddock && !Abandonned && OnSale && IsOwned && IsCharacterOwner(character);
        }

        public IEnumerable<int> GetVisibleSkills(WorldClient client, IEnumerable<int> availableSkills = null)
        {
            var result = new HashSet<int>();
            var character = client?.Character;
            var candidates = new HashSet<int>(availableSkills ?? new[] { 175, 176, 177, 178 });

            if (IsPublicPaddock)
            {
                if (candidates.Contains(175))
                    result.Add(175);

                return result;
            }

            if (candidates.Contains(175) && CanUsePaddock(character))
                result.Add(175);

            if (candidates.Contains(176) && ShouldDisplayBuySkill(character))
                result.Add(176);

            if (candidates.Contains(177) && CanSell(character))
                result.Add(177);

            if (candidates.Contains(178) && CanModifySalePrice(character))
                result.Add(178);

            return result;
        }

        public void AssignTo(Character character)
        {
            if (character == null)
                return;

            GuildId = character.Guild?.Id;

            if (PaddockManager.Instance.HasOwnerIdColumn)
                OwnerId = character.Id;

            Record.isPublic = false;
            IsDirty = true;
            Abandonned = false;
            OnSale = false;
            Locked = false;
        }

        public void PutForSale(int price)
        {
            Record.isPublic = false;
            IsDirty = true;
            Price = Math.Max(0, price);
            OnSale = true;
            Locked = false;
            Abandonned = false;
        }

        public void CancelSale()
        {
            Record.isPublic = false;
            IsDirty = true;
            OnSale = false;
            Locked = false;
        }

        public PaddockInformations GetPropertiesForClient(Character character, IEnumerable<Mount> mounts)
        {
            short maxOutdoorMount = (short)Math.Max(0, Math.Min(short.MaxValue, (int)MaxOutdoorMount));
            short maxItems = (short)Math.Max(0, Math.Min(short.MaxValue, (int)MaxItems));
            int guildId = GuildId ?? 0;
            int visiblePrice = (Abandonned || OnSale) ? SalePrice : 0;

            // Un enclos public reste toujours affiché comme public : accessible, gratuit,
            // sans guilde propriétaire ni état de vente.
            if (IsPublicPaddock)
                return BuildContentInformations(mounts);

            // Un enclos privé/guilde ne doit jamais retomber dans l'affichage "public".
            // On privilégie donc les états privés explicitement.
            if (Abandonned)
                return new PaddockAbandonnedInformations(maxOutdoorMount, maxItems, visiblePrice, Locked, guildId);

            if (OnSale)
                return new PaddockBuyableInformations(maxOutdoorMount, maxItems, visiblePrice, Locked);

            if (IsOwned && Guild != null)
                return new PaddockPrivateInformations(maxOutdoorMount, maxItems, visiblePrice, Locked, guildId, Guild.GetGuildInformations());

            // Fallback de sécurité : enclos non public, sans guilde résolue.
            return BuildContentInformations(mounts);
        }

        public PaddockContentInformations BuildContentInformations(IEnumerable<Mount> mounts)
        {
            short worldX = 0;
            short worldY = 0;

            if (Map != null)
            {
                if (Map.Point != null)
                {
                    worldX = (short)Map.Point.X;
                    worldY = (short)Map.Point.Y;
                }
                else
                {
                    worldX = (short)Map.Position.X;
                    worldY = (short)Map.Position.Y;
                }
            }

            return new PaddockContentInformations(
                (short)Math.Max(0, Math.Min(short.MaxValue, (int)MaxOutdoorMount)),
                (short)Math.Max(0, Math.Min(short.MaxValue, (int)MaxItems)),
                Id,
                worldX,
                worldY,
                MapId,
                (mounts ?? Enumerable.Empty<Mount>()).Where(x => x != null).Select(x => x.GetInformationsForPaddock()).ToArray());
        }

        public PaddockContentInformations GetGuildContentInformations()
        {
            return BuildContentInformations(MountManager.Instance.GetPaddockedMountsByMap(MapId));
        }

        public PaddockInformationsForSell GetInformationsForSell()
        {
            short worldX = 0;
            short worldY = 0;
            short subAreaId = 0;

            if (Map != null)
            {
                if (Map.Point != null)
                {
                    worldX = (short)Map.Point.X;
                    worldY = (short)Map.Point.Y;
                }
                else
                {
                    worldX = (short)Map.Position.X;
                    worldY = (short)Map.Position.Y;
                }

                subAreaId = (short)Map.SubAreaId;
            }

            return new PaddockInformationsForSell(
                Guild?.Name ?? string.Empty,
                worldX,
                worldY,
                subAreaId,
                (sbyte)Math.Max(0, Math.Min(127, (int)MaxOutdoorMount)),
                (sbyte)Math.Max(0, Math.Min(127, (int)MaxItems)),
                SalePrice);
        }
    }
}
