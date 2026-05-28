using Dapper;
using Dapper.Contrib.Extensions;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.Auth.Accounts;
using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Characters;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Tools.Dlm;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Actors.Characters;
using Sunshine.WorldServer.Game.Actors.Characters.Inventory;
using Sunshine.WorldServer.Game.Actors.Characters.Jobs;
using Sunshine.WorldServer.Game.Actors.Characters.Quests;
using Sunshine.WorldServer.Game.Actors.Characters.Shortcuts;
using Sunshine.WorldServer.Game.Actors.Characters.Spells;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors.Look;
using Sunshine.WorldServer.Game.Actors.Merchants;
using Sunshine.WorldServer.Game.Actors.Monsters;
using Sunshine.WorldServer.Game.Actors.Npcs;
using Sunshine.WorldServer.Game.Actors.Stats;
using Sunshine.WorldServer.Game.Actors.TaxCollectors;
using Sunshine.WorldServer.Game.BidsHouse;
using Sunshine.WorldServer.Game.Dialogs;
using Sunshine.WorldServer.Game.Exchanges;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.WorldServer.Game.Fights.Types;
using Sunshine.WorldServer.Game.Guilds;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Maps.Houses;
using Sunshine.WorldServer.Game.Maps.PaddockInstances;
using Sunshine.WorldServer.Game.Maps.Pathfinding;
using Sunshine.WorldServer.Game.Maps.Triggers;
using Sunshine.WorldServer.Game.Mounts;
using Sunshine.WorldServer.Game.Parties;
using Sunshine.WorldServer.Handlers.Basic;
using Sunshine.WorldServer.Handlers.Characters;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using Sunshine.WorldServer.Handlers.Context;
using Sunshine.WorldServer.Handlers.Context.Roleplay;
using Sunshine.WorldServer.Handlers.Context.RolePlay.Party;
using Sunshine.WorldServer.Handlers.Houses;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace Sunshine.WorldServer.Game.Characters
{
    public class Character : RolePlayActor
    {
        public WorldClient Client { get; set; }
        public CharacterRecord Record { get; set; }
        public StatsFields Stats { get; set; }
        public CharacterAlignment Alignment { get; set; }
        public SpellInventory Spells { get; set; }
        public ShortcutBar Shortcuts { get; set; }
        public Inventory Inventory { get; set; }
        public BidHouseBag BidHouseBag { get; set; }
        public JobsCollection Jobs { get; set; }
        public QuestsCollection Quests { get; set; }
        public Guilds.GuildMember GuildMember { get; set; }
        public House LastTargetedHouse { get; set; }
        public PaddockInstance LastTargetedPaddockInstance { get; set; }
        public int? LastTargetedMountId { get; set; }
        public int? LastViewedMountId { get; set; }
        public sbyte MoodSmileyId { get; set; }
        public DateTime NextMoodSmileyChangeUtc { get; set; }

        public int? FightReturnMapId { get; set; }
        public short? FightReturnCellId { get; set; }
        public int? FightReturnDirection { get; set; }
        public bool IsMapMoving { get; set; }
        public bool IsStartingMonsterFight { get; set; }
        public int? PendingMonsterFightGroupId { get; set; }
        public short? PendingMonsterFightCellId { get; set; }
        public DateTime PendingMonsterFightUntilUtc { get; set; }
        public DateTime LastMonsterFightLaunchUtc { get; set; }

        private int _monsterFightStartGuard;

        public bool HasPendingMonsterFight()
        {
            return PendingMonsterFightGroupId.HasValue &&
                   PendingMonsterFightUntilUtc != DateTime.MinValue &&
                   DateTime.UtcNow <= PendingMonsterFightUntilUtc;
        }

        public bool CanChangeMoodSmiley()
        {
            return DateTime.UtcNow >= NextMoodSmileyChangeUtc;
        }

        public int GetMoodSmileyCooldownSeconds()
        {
            if (CanChangeMoodSmiley())
                return 0;

            return (int)Math.Ceiling((NextMoodSmileyChangeUtc - DateTime.UtcNow).TotalSeconds);
        }

        private MonsterGroup GetMonsterGroupOnCell(short cellId)
        {
            if (Map == null || Map.RolePlayActors == null)
                return null;

            return Map.RolePlayActors
                .OfType<MonsterGroup>()
                .FirstOrDefault(x => x != null &&
                                     x.Map != null &&
                                     x.Map.Record.Id == Map.Id &&
                                     x.Cell == cellId);
        }

        private void ArmPendingMonsterFight(MonsterGroup group)
        {
            if (group == null)
            {
                ClearPendingMonsterFight();
                return;
            }

            PendingMonsterFightGroupId = group.Id;
            PendingMonsterFightCellId = group.Cell;
            PendingMonsterFightUntilUtc = DateTime.UtcNow.AddSeconds(15);
        }

        public void ClearPendingMonsterFight()
        {
            PendingMonsterFightGroupId = null;
            PendingMonsterFightCellId = null;
            PendingMonsterFightUntilUtc = DateTime.MinValue;
        }

        private bool CanStartPendingMonsterFight(MonsterGroup group)
        {
            if (group == null || Map == null || Cell == null)
                return false;

            if (group.IsEngaged)
                return false;

            if (IsInFight() || Fighter != null || IsStartingMonsterFight)
                return false;

            if (!PendingMonsterFightGroupId.HasValue || PendingMonsterFightGroupId.Value != group.Id)
                return false;

            if (!PendingMonsterFightCellId.HasValue || PendingMonsterFightCellId.Value != Cell.Id)
                return false;

            if (group.Cell != Cell.Id)
                return false;

            if (PendingMonsterFightUntilUtc != DateTime.MinValue && DateTime.UtcNow > PendingMonsterFightUntilUtc)
                return false;

            if ((DateTime.UtcNow - LastMonsterFightLaunchUtc).TotalMilliseconds < 1200)
                return false;

            return true;
        }

        private bool TryBeginMonsterFightLaunch(MonsterGroup group)
        {
            if (group == null)
                return false;

            if (Interlocked.CompareExchange(ref _monsterFightStartGuard, 1, 0) != 0)
                return false;

            if (group.IsEngaged)
            {
                Interlocked.Exchange(ref _monsterFightStartGuard, 0);
                return false;
            }

            group.IsEngaged = true;
            IsStartingMonsterFight = true;
            return true;
        }

        private void EndMonsterFightLaunch(MonsterGroup group, bool success)
        {
            if (!success && group != null)
                group.IsEngaged = false;

            IsStartingMonsterFight = false;
            Interlocked.Exchange(ref _monsterFightStartGuard, 0);
        }

        public void SetMoodSmiley(sbyte smileyId)
        {
            MoodSmileyId = smileyId;
            NextMoodSmileyChangeUtc = DateTime.UtcNow.AddMinutes(1);
        }

        private const int BrokenHouseFallbackMapId = 2323;
        private const short BrokenHouseFallbackCellId = 328;
        private const int BrokenHouseFallbackDirection = 3;

        public Character(CharacterRecord record)
        {
            Record = record;
            Account = AccountManager.Instance.GetAccount(record.Id);
            this.LoadRecord();
        }

        public Account Account { get; set; }

        public bool GodMode { get; set; }

        public override int Id { get { return Record.Id; } }

        public string Name
        {
            get { return Record.Name; }
            set { Record.Name = value; }
        }

        public short Level
        {
            get { return Record.Level; }
            set { Record.Level = value; }
        }

        public int Breed
        {
            get { return Record.Breed; }
            set { Record.Breed = value; }
        }

        public bool Sex
        {
            get { return Record.Sex; }
            set { Record.Sex = value; }
        }

        public string CustomLook
        {
            get { return Record.CustomLook; }
            set { Record.CustomLook = value; }
        }

        public long Experience
        {
            get { return Record.Experience; }
            set
            {
                if (value < 0)
                    value = 0;

                Record.Experience = value;
                if (Level >= 200)
                    return;
                var nextLevel = ExperienceManager.Instance.GetNextLevelExperienceFloor(value);
                if (Level < nextLevel)
                {
                    int levelDiff = nextLevel - Level;
                    LevelUp((byte)levelDiff);
                }
            }
        }

        public long ExperienceLevelFloor
        {
            get { return ExperienceManager.Instance.GetExperienceLevelFloor(Level); }
        }

        public long ExperienceNextLevelFloor
        {
            get { return ExperienceManager.Instance.GetNextExperienceLevelFloor(Level); }
        }

        public int Direction
        {
            get { return Record.Direction; }
            set { Record.Direction = value; }
        }

        public int StatsPoints
        {
            get { return Record.StatsPoints; }
            set { Record.StatsPoints = value; }
        }

        public int SpellsPoints
        {
            get { return Record.SpellsPoints; }
            set { Record.SpellsPoints = value; }
        }

        public List<Map> Zaaps { get; set; }

        public Map Map { get; set; }

        public Cell Cell { get; set; }

        public ActorLook Look { get; set; }

        public EntityLook GetDisplayedEntityLook()
        {
            return GetDisplayedLook().GetEntityLook();
        }

        public EntityLook GetDisplayedEntityLook(ActorLook sourceLook)
        {
            return GetDisplayedLook(sourceLook).GetEntityLook();
        }

        public EntityLook GetSelectionEntityLook()
        {
            try
            {
                var merchant = MerchantManager.Instance.GetMerchant(Id);
                if (merchant != null && merchant.Look != null)
                    return merchant.Look.GetEntityLook();
            }
            catch
            {
            }

            return GetDisplayedEntityLook();
        }

        public ActorLook GetDisplayedLook()
        {
            return GetDisplayedLook(Look ?? EntityManager.Instance.GetActorLook(Record.EntityLook));
        }

        private const short DefaultPetScale = 100;

        private BasePlayerItem GetDisplayedPetItem()
        {
            if (Inventory == null)
                return null;

            var petItem = Inventory.GetItem(CharacterInventoryPositionEnum.ACCESSORY_POSITION_PETS);
            if (petItem == null || petItem.Template == null || petItem.AppearanceId <= 0)
                return null;

            switch (petItem.Type)
            {
                case ItemTypeEnum.FAMILIER:
                case ItemTypeEnum.FANTOME_DE_FAMILIER:
                case ItemTypeEnum.MONTILIER:
                case ItemTypeEnum.FANTOME_DE_MONTILIER:
                    return petItem;
                default:
                    return null;
            }
        }

        private void ApplyDisplayedPetLook(ActorLook look)
        {
            if (look == null)
                return;

            look.SubActorLooks.RemoveAll(x =>
                x.BindingCategory == SubEntityBindingPointCategoryEnum.HOOK_POINT_CATEGORY_PET);

            var petItem = GetDisplayedPetItem();
            if (petItem == null)
                return;

            var petLook = new ActorLook(
                petItem.AppearanceId,
                new short[0],
                new Dictionary<int, Color>(),
                new[] { DefaultPetScale },
                new SubActorLook[0]);

            look.AddSubLook(new SubActorLook(
                0,
                SubEntityBindingPointCategoryEnum.HOOK_POINT_CATEGORY_PET,
                petLook));
        }

        private static short ResolveMountedRiderBones(short baseBonesId, short defaultBonesId)
        {
            switch (baseBonesId)
            {
                case 44: // Pandawa - Picole
                    return 1084;
                case 113: // Xelor - Momification
                    return 1068;
                case 453: // Pandawa - Colère de Zatoïshwan
                    return 1202;
                default:
                    return baseBonesId == defaultBonesId ? (short)2 : baseBonesId;
            }
        }

        private static void ApplyMountedRiderScale(ActorLook riderLook, short baseBonesId)
        {
            if (riderLook == null)
                return;

            short? mountedScale = null;
            switch (baseBonesId)
            {
                case 453:
                    mountedScale = 60;
                    break;
            }

            if (!mountedScale.HasValue)
                return;

            riderLook.Scales.Clear();
            riderLook.Scales.Add(mountedScale.Value);
            riderLook.Scales.Add(mountedScale.Value);
        }

        public ActorLook GetDisplayedLook(ActorLook sourceLook)
        {
            var currentLook = sourceLook ?? Look ?? EntityManager.Instance.GetActorLook(Record.EntityLook);
            if (currentLook == null)
                return new ActorLook();

            var displayedLook = currentLook.Clone();
            ApplyDisplayedPetLook(displayedLook);

            if (EquippedMount == null || !IsRiding || EquippedMount.Template == null || string.IsNullOrWhiteSpace(EquippedMount.Template.LookAsString))
                return displayedLook;

            try
            {
                var mountLook = EntityManager.Instance.GetActorLook(EquippedMount.Template.LookAsString);
                if (mountLook == null)
                    return displayedLook;

                var displayedMountLook = mountLook.Clone();
                var riderLook = displayedLook.Clone();
                var riderBaseBones = riderLook.BonesID;
                var defaultBaseLook = EntityManager.Instance.GetActorLook(Record.EntityLook);
                var defaultBonesId = defaultBaseLook != null ? defaultBaseLook.BonesID : riderBaseBones;

                if (EquippedMount.GetBehaviors().Contains(9))
                {
                    displayedMountLook.Colors.Clear();
                    foreach (var riderColor in riderLook.Colors)
                        displayedMountLook.Colors[riderColor.Key] = riderColor.Value;
                }

                displayedMountLook.SubActorLooks.RemoveAll(x =>
                    x.BindingCategory == SubEntityBindingPointCategoryEnum.HOOK_POINT_CATEGORY_MOUNT_DRIVER);

                riderLook.SubActorLooks.RemoveAll(x =>
                    x.BindingCategory == SubEntityBindingPointCategoryEnum.HOOK_POINT_CATEGORY_MOUNT_DRIVER);

                riderLook.BonesID = ResolveMountedRiderBones(riderBaseBones, defaultBonesId);
                ApplyMountedRiderScale(riderLook, riderBaseBones);

                displayedMountLook.AddSubLook(new SubActorLook(
                    0,
                    SubEntityBindingPointCategoryEnum.HOOK_POINT_CATEGORY_MOUNT_DRIVER,
                    riderLook));

                return displayedMountLook;
            }
            catch
            {
                return displayedLook;
            }
        }

        public CharacterBaseInformations GetCharacterBaseInformations()
        {
            var selectionLook = GetSelectionEntityLook();
            return new CharacterBaseInformations(Id, (byte)Level, Name, selectionLook, (sbyte)Breed, Sex);
        }

        public ActorAlignmentInformations GetActorAlignmentInformations()
        {
            var side = Alignment != null && Alignment.Enabled ? (sbyte)Alignment.Side : (sbyte)0;
            var value = Alignment != null && Alignment.Enabled ? Alignment.Value : (sbyte)0;
            var grade = Alignment != null && Alignment.Enabled ? Alignment.Grade : (sbyte)0;

            if (grade < 0)
                grade = 0;
            if (grade > 10)
                grade = 10;

            return new ActorAlignmentInformations(side, value, grade, Alignment != null ? Alignment.Dishonor : (ushort)0, Level);
        }

        public ActorExtendedAlignmentInformations GetActorExtendedAlignmentInformations()
        {
            var side = Alignment != null ? (sbyte)Alignment.Side : (sbyte)0;
            var value = Alignment != null ? Alignment.Value : (sbyte)0;
            var grade = Alignment != null ? Alignment.Grade : (sbyte)0;

            if (grade < 0)
                grade = 0;
            if (grade > 10)
                grade = 10;

            return new ActorExtendedAlignmentInformations(side, value, grade, Alignment != null ? Alignment.Dishonor : (ushort)0, Level,
                Alignment != null ? Alignment.Honor : (ushort)0,
                Alignment != null ? Alignment.HonorGradeFloor : (ushort)0,
                Alignment != null ? Alignment.HonorNextGradeFloor : (ushort)0,
                Alignment != null && Alignment.Enabled);
        }

        public ActorRestrictionsInformations GetActorRestrictionsInformations()
        {
            return new ActorRestrictionsInformations(false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, true, false, false, false, false, false);
        }

        public override GameRolePlayActorInformations GetGameRolePlayActorInformations()
        {
            return new GameRolePlayCharacterInformations(Id, GetDisplayedEntityLook(), new EntityDispositionInformations(Cell.Id, (sbyte)Direction), Name,
               Guild != null ? new HumanWithGuildInformations(new List<EntityLook>(), 0, 0, GetActorRestrictionsInformations(), 0, "", Guild.GetGuildInformations()) : new HumanInformations(new List<EntityLook>(), 0, 0, GetActorRestrictionsInformations(), 0, ""), GetActorAlignmentInformations());
        }

        public CharacterMinimalPlusLookInformations GetCharacterMinimalPlusLookInformations()
        {
            return new CharacterMinimalPlusLookInformations(Id, (byte)Level, Name, GetDisplayedEntityLook());
        }

        public CharacterMinimalPlusLookAndGradeInformations GetCharacterMinimalPlusLookAndGradeInformations()
        {
            var grade = Alignment != null && Alignment.Enabled ? Alignment.Grade : (sbyte)0;

            if (grade < 0)
                grade = 0;
            if (grade > 10)
                grade = 10;

            return new CharacterMinimalPlusLookAndGradeInformations(Id, (byte)Level, Name, GetDisplayedEntityLook(), grade);
        }

        public CharacterCharacteristicsInformations GetCharacterCharacteristicsInformations()
        {
            return new CharacterCharacteristicsInformations(Experience, ExperienceLevelFloor, ExperienceNextLevelFloor, (int)Inventory.Kamas, StatsPoints, SpellsPoints,
                  GetActorExtendedAlignmentInformations(), Stats.Health.Total, Stats.Health.TotalMax, 1000, 1000, (short)Stats.AP.Total, (short)Stats.MP.Total, Stats[StatsEnum.Initiative], Stats[StatsEnum.Prospecting], Stats[StatsEnum.AP], Stats[StatsEnum.MP], Stats[StatsEnum.Strength],
                  Stats[StatsEnum.Vitality], Stats[StatsEnum.Wisdom], Stats[StatsEnum.Chance], Stats[StatsEnum.Agility], Stats[StatsEnum.Intelligence],
                  Stats[StatsEnum.Range], Stats[StatsEnum.SummonLimit], Stats[StatsEnum.DamageReflection], Stats[StatsEnum.CriticalHit], 0, Stats[StatsEnum.CriticalMiss],
                  Stats[StatsEnum.HealBonus], Stats[StatsEnum.DamageBonus], Stats[StatsEnum.WeaponDamageBonus], Stats[StatsEnum.DamageBonusPercent], Stats[StatsEnum.TrapBonus],
                  Stats[StatsEnum.TrapBonusPercent], Stats[StatsEnum.PermanentDamagePercent], Stats[StatsEnum.TackleBlock], Stats[StatsEnum.TackleEvade], Stats[StatsEnum.APAttack],
                  Stats[StatsEnum.MPAttack], Stats[StatsEnum.PushDamageBonus], Stats[StatsEnum.CriticalDamageBonus], Stats[StatsEnum.NeutralDamageBonus], Stats[StatsEnum.EarthDamageBonus],
                  Stats[StatsEnum.WaterDamageBonus], Stats[StatsEnum.AirDamageBonus], Stats[StatsEnum.FireDamageBonus], Stats[StatsEnum.DodgeAPProbability], Stats[StatsEnum.DodgeMPProbability],
                  Stats[StatsEnum.NeutralResistPercent], Stats[StatsEnum.EarthResistPercent], Stats[StatsEnum.WaterResistPercent], Stats[StatsEnum.AirResistPercent], Stats[StatsEnum.FireResistPercent],
                  Stats[StatsEnum.NeutralElementReduction], Stats[StatsEnum.EarthElementReduction], Stats[StatsEnum.WaterElementReduction], Stats[StatsEnum.AirElementReduction], Stats[StatsEnum.FireElementReduction],
                  Stats[StatsEnum.PushDamageReduction], Stats[StatsEnum.CriticalDamageReduction], Stats[StatsEnum.PvpNeutralResistPercent], Stats[StatsEnum.PvpEarthResistPercent], Stats[StatsEnum.PvpWaterResistPercent],
                  Stats[StatsEnum.PvpAirResistPercent], Stats[StatsEnum.PvpFireResistPercent], Stats[StatsEnum.PvpNeutralElementReduction], Stats[StatsEnum.PvpEarthElementReduction], Stats[StatsEnum.PvpWaterElementReduction],
                  Stats[StatsEnum.PvpAirElementReduction], Stats[StatsEnum.PvpFireElementReduction], new System.Collections.Generic.List<CharacterSpellModification>());
        }

        public PartyMemberInformations GetPartyMemberInformations
            => new PartyMemberInformations(Id, (byte)Level, Name, GetDisplayedEntityLook(), Stats.Health.Total, Stats.Health.TotalMax, (short)Stats.Prospecting.Total, 1, (short)Stats.Initiative.Total, Alignment.Enabled, (sbyte)Alignment.Side);

        public PartyInvitationMemberInformations GetPartyInvitationMemberInformations
            => new PartyInvitationMemberInformations(Id, (byte)Level, Name, GetDisplayedEntityLook(), (sbyte)Breed, Sex, 0, 0, Map.Id);

        public PartyGuestInformations GetPartyGuestInformations
            => new PartyGuestInformations(Id, Party.Leader.Id, Name, GetDisplayedEntityLook());

        public Party Party { get; set; }

        public Fight Fight { get; set; }

        public CharacterFighter Fighter { get; set; }

        public Trade Trade { get; set; }

        private Mounts.Mount _equippedMount;

        public Mounts.Mount EquippedMount
        {
            get { return _equippedMount; }
            set
            {
                _equippedMount = value;

                if (Record != null)
                    Record.EquippedMount = value != null ? (int?)value.Id : null;

                if (value == null && Record != null)
                    Record.IsRiding = false;
            }
        }

        public bool IsRiding
        {
            get { return EquippedMount != null && Record != null && Record.IsRiding; }
            set
            {
                if (Record != null)
                    Record.IsRiding = value && EquippedMount != null;
            }
        }

        public IDialog Dialog { get; set; }

        public bool IsInParty { get { return Party != null; } }

        public bool IsPartyLeader { get { return !IsInParty ? false : Party.Leader == this; } }

        public bool IsInFight() { return Fight != null; }

        public bool IsInTrade() { return Trade != null; }

        public bool IsInDialog() { return Dialog != null; }

        public bool IsInWorld() { return Client != null; }

        public Guild Guild { get { return GuildMember?.Guild; } }

        public bool IsBusy() { return IsInTrade() || IsInDialog(); }

        private bool _isInRegen;

        private DateTime _regenStartTime;

        private byte _regenRate;

        public bool HasZaap(int map)
        {
            return Zaaps.Any(x => x.Id == map);
        }


        private void RecoverOrphanedEquippedMount()
        {
            if (EquippedMount != null || Inventory == null)
                return;

            var certificateMountIds = Inventory.GetItems()
                .Select(x =>
                {
                    ObjectEffectMount effect;
                    return MountCertificateFactory.TryGetMountEffect(x, out effect) ? effect.mountId : 0;
                })
                .Where(x => x > 0)
                .ToHashSet();

            var orphanMount = MountManager.Instance.GetOwnerMounts(Id)
                .Where(x => x != null && !x.IsInStable && !x.Record.PaddockId.HasValue && x.Record.StoredSince.HasValue)
                .OrderByDescending(x => x.Record.StoredSince ?? DateTime.MinValue)
                .FirstOrDefault(x => !certificateMountIds.Contains(x.Id));

            if (orphanMount == null)
                return;

            orphanMount.Record.StoredSince = null;
            MountManager.Instance.Save(orphanMount);
            EquippedMount = orphanMount;
            IsRiding = false;
        }

        public void DiscoverZaap(Map map)
        {
            this.Zaaps.Add(map);
            this.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 24);
        }

        public void BindToHouse(House house, bool persist = true)
        {
            LastTargetedHouse = house;
            Record.HouseId = house != null ? (int?)house.Id : null;

            LastTargetedPaddockInstance = null;
            Record.PaddockId = null;

            if (persist)
                PersistLocationBindings();
        }

        public void ClearHouseBinding(bool persist = true)
        {
            LastTargetedHouse = null;
            Record.HouseId = null;

            if (persist)
                PersistLocationBindings();
        }

        public void BindToPaddockInstance(PaddockInstance instance, bool persist = true)
        {
            LastTargetedPaddockInstance = instance;
            Record.PaddockId = instance != null ? (int?)instance.Id : null;

            LastTargetedHouse = null;
            Record.HouseId = null;

            if (persist)
                PersistLocationBindings();
        }

        public void ClearPaddockInstanceBinding(bool persist = true)
        {
            LastTargetedPaddockInstance = null;
            Record.PaddockId = null;

            if (persist)
                PersistLocationBindings();
        }

        private int GetRuntimeInstanceCoordinateMapId(int targetMapId)
        {
            if (LastTargetedPaddockInstance != null &&
                LastTargetedPaddockInstance.ContainsInteriorMap(targetMapId))
            {
                return LastTargetedPaddockInstance.MapId;
            }

            return 0;
        }

        private Map CreateRuntimeMapIfNeeded(Map map)
        {
            if (map != null && map.IsInstanceTemplate() && !map.RuntimeInstance)
            {
                var coordinateMapId = GetRuntimeInstanceCoordinateMapId(map.Id);
                var runtimeMap = MapManager.Instance.CreateMapInstance(map.Id, coordinateMapId);
                if (runtimeMap != null)
                    return runtimeMap;
            }

            return map;
        }

        private Map GetRuntimeAwareCurrentMap()
        {
            return CreateRuntimeMapIfNeeded(MapManager.Instance.GetMap(this));
        }

        private void PersistLocationBindings()
        {
            try
            {
                CharacterPaddockBootstrap.EnsurePaddockIdColumn();
                DatabaseManager.Connection.Execute(
                    "UPDATE characters SET HouseId = @HouseId, PaddockId = @PaddockId WHERE Id = @Id",
                    new { HouseId = Record.HouseId, PaddockId = Record.PaddockId, Id = Record.Id });
            }
            catch
            {
            }
        }

        private void PersistHouseBinding()
        {
            PersistLocationBindings();
        }

        private void RecoverFromBrokenHouseState()
        {
            Record.MapId = BrokenHouseFallbackMapId;
            Record.CellId = BrokenHouseFallbackCellId;
            Record.Direction = BrokenHouseFallbackDirection;
            Record.HouseId = null;
            Record.PaddockId = null;
            LastTargetedHouse = null;
            LastTargetedPaddockInstance = null;

            try
            {
                CharacterPaddockBootstrap.EnsurePaddockIdColumn();
                DatabaseManager.Connection.Execute("UPDATE characters SET MapId = @MapId, CellId = @CellId, Direction = @Direction, HouseId = NULL, PaddockId = NULL WHERE Id = @Id",
                    new { MapId = Record.MapId, CellId = Record.CellId, Direction = Record.Direction, Id = Record.Id });
            }
            catch
            {
            }
        }

        private void SyncHouseStateOnLoad()
        {
            LastTargetedHouse = null;

            if (LastTargetedPaddockInstance != null &&
                LastTargetedPaddockInstance.ContainsInteriorMap(Record.MapId))
                return;

            var currentMapId = Record.MapId;
            var linkedHouseId = Record.HouseId.GetValueOrDefault();
            var linkedHouse = linkedHouseId > 0 ? HouseManager.Instance.GetHouse(linkedHouseId) : null;

            if (linkedHouse != null && linkedHouse.ContainsInteriorMap(currentMapId))
            {
                LastTargetedHouse = linkedHouse;
                return;
            }

            if (linkedHouseId > 0)
            {
                RecoverFromBrokenHouseState();
                return;
            }

            var implicitInteriorHouse = HouseManager.Instance.GetHousesByInteriorMapId(currentMapId).ToList();

            if (implicitInteriorHouse.Count == 0 && Record.HouseId.HasValue)
            {
                ClearHouseBinding(true);
                return;
            }
            if (implicitInteriorHouse.Count == 1)
            {
                BindToHouse(implicitInteriorHouse[0], true);
                return;
            }

            if (implicitInteriorHouse.Count > 1 || implicitInteriorHouse.Count == 0 && HouseManager.Instance.ResolveInteriorHouse(this) == null && currentMapId != BrokenHouseFallbackMapId)
            {
                var looksLikeHouseInterior = HouseManager.Instance.GetHousesByInteriorMapId(currentMapId).Any();
                if (looksLikeHouseInterior)
                    RecoverFromBrokenHouseState();
            }
        }

        private void SyncPaddockInstanceStateOnLoad()
        {
            LastTargetedPaddockInstance = null;

            var currentMapId = Record.MapId;
            var linkedPaddockId = Record.PaddockId.GetValueOrDefault();
            var linkedPaddock = linkedPaddockId > 0 ? PaddockInstanceManager.Instance.GetInstance(linkedPaddockId) : null;

            if (linkedPaddock != null && linkedPaddock.ContainsInteriorMap(currentMapId))
            {
                LastTargetedPaddockInstance = linkedPaddock;
                Record.PaddockId = linkedPaddock.Id;
                return;
            }

            if (linkedPaddockId > 0)
            {
                ClearPaddockInstanceBinding(true);
                return;
            }

            var implicitInteriorPaddocks = PaddockInstanceManager.Instance.GetInstancesByInteriorMap(currentMapId).ToList();

            if (implicitInteriorPaddocks.Count == 1)
            {
                BindToPaddockInstance(implicitInteriorPaddocks[0], true);
                return;
            }

            if (implicitInteriorPaddocks.Count == 0 && Record.PaddockId.HasValue)
                ClearPaddockInstanceBinding(true);
        }

        public void EnterMap(Map map)
        {
            map = CreateRuntimeMapIfNeeded(map);
            if (map == null)
                return;

            Map = map;

            if (map.Id == 35652608 && this.Quests.HasQuest(489))
                this.Quests.UpdateObjective(1045, 3503, true, true);

            map.EnterActor(this);

        }

        public void LeaveMap(Map map)
        {
            map.LeaveActor(this);
        }

        public void Teleport(int mapId, short cellId)
        {
            IsMapMoving = false;
            IsStartingMonsterFight = false;
            ClearPendingMonsterFight();

            Map?.LeaveActor(this);
            Record.MapId = mapId;
            Record.CellId = cellId;
            Map = GetRuntimeAwareCurrentMap();

            if (Map == null)
            {
                Record.MapId = BrokenHouseFallbackMapId;
                Record.CellId = BrokenHouseFallbackCellId;
                Record.Direction = BrokenHouseFallbackDirection;
                Map = GetRuntimeAwareCurrentMap();
            }

            Cell = MapManager.Instance.GetCell(this);
            if (Map != null && Map.Cells != null && Map.Cells.Any())
            {
                var fallbackCell = Map.Cells.Any(x => x.Id == Record.CellId)
                    ? Map.Cells.First(x => x.Id == Record.CellId)
                    : Map.Cells.Any(x => x.Walkable && !x.NonWalkableDuringFight)
                        ? Map.Cells.First(x => x.Walkable && !x.NonWalkableDuringFight)
                        : Map.Cells.First();

                Record.CellId = fallbackCell.Id;

                if (Cell == null)
                    Cell = MapManager.Instance.GetCell(this);
                else
                    Cell.Id = fallbackCell.Id;
            }

            if (Client != null)
            {
                ContextRoleplayHandler.SendCurrentMapMessage(Client, Record.MapId);
                ContextHandler.SendGameMapChangeOrientationMessage(Client);
            }
        }

        public void CaptureFightReturnPosition()
        {
            if (FightReturnMapId.HasValue && FightReturnCellId.HasValue)
                return;

            FightReturnMapId = Record.MapId;
            FightReturnCellId = Record.CellId;
            FightReturnDirection = Record.Direction;
        }

        public void ClearFightReturnPosition()
        {
            FightReturnMapId = null;
            FightReturnCellId = null;
            FightReturnDirection = null;
        }

        public void TeleportToFightReturnPosition()
        {
            var returnMapId = FightReturnMapId ?? Record.MapId;
            var returnCellId = FightReturnCellId ?? Record.CellId;
            var returnDirection = FightReturnDirection;

            ClearFightReturnPosition();

            if (returnDirection.HasValue)
                Direction = returnDirection.Value;

            Teleport(returnMapId, returnCellId);
        }

        public void EnterWorld()
        {
            SyncPaddockInstanceStateOnLoad();
            SyncHouseStateOnLoad();
            Map = GetRuntimeAwareCurrentMap();
            Cell = MapManager.Instance.GetCell(this);
            this.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 89);
            this.BidHouseBag.LoadBag();
            MerchantManager.Instance.DeactivateForConnectedCharacter(this);
            this.StartRegen();
        }

        public void SetParty(PartyTypeEnum party, Character leader)
        {
            switch (party)
            {
                case PartyTypeEnum.PARTY_TYPE_CLASSICAL:
                    Party = PartyManager.Instance.SetParty(leader);
                    Party.AddMember(leader);
                    break;
                case PartyTypeEnum.PARTY_TYPE_DUNGEON:
                    break;
            }
        }

        public void SetInvitation(PartyTypeEnum party, Character guest)
        {
            switch (party)
            {
                case PartyTypeEnum.PARTY_TYPE_CLASSICAL:
                    PartyHandler.SendPartyInvitationMessage(guest.Client, Party, this);
                    Party.Sender = this;
                    Party.AddGuest(guest);
                    break;
                case PartyTypeEnum.PARTY_TYPE_DUNGEON:
                    break;
            }
        }

        public void LeaveParty()
        {
            Party.RemoveMember(this);
        }

        public void SetFight(FightTypeEnum fight, object argv = null)
        {
            switch (fight)
            {
                case FightTypeEnum.FIGHT_TYPE_PvM:
                    Fight = argv != null ? (FightPvM)argv : FightManager.Instance.SetFight<FightPvM>(this);
                    break;

                case FightTypeEnum.FIGHT_TYPE_CHALLENGE:
                    Fight = argv != null ? (FightDuel)argv : FightManager.Instance.SetFight<FightDuel>(this);
                    break;

                case FightTypeEnum.FIGHT_TYPE_AGRESSION:
                    Fight = argv as Fight ?? FightManager.Instance.SetFight<FightAgression>(this);
                    break;

                case FightTypeEnum.FIGHT_TYPE_PvT:
                    Fight = argv as Fight ?? FightManager.Instance.SetFight<FightPvT>(this);
                    break;

                case FightTypeEnum.FIGHT_TYPE_MXvM:
                    Fight = argv as Fight;
                    break;
            }

        }

        public void SetFightRequest(FightTypeEnum fight, Character target)
        {
            switch (fight)
            {
                case FightTypeEnum.FIGHT_TYPE_CHALLENGE:
                    ContextRoleplayHandler.SendGameRolePlayPlayerFightFriendlyRequestedMessage(Client, target, this, target);
                    ContextRoleplayHandler.SendGameRolePlayPlayerFightFriendlyRequestedMessage(target.Client, this, this, target);
                    SetFight(fight);
                    target.SetFight(fight, Fight);
                    break;
            }
        }

        public void LeaveFight()
        {
            Fight.LeaveFighter(Client, Fighter);
            Fight = null;
        }

        public void SetTrade(ExchangeTypeEnum exchange, object argv = null, object target = null)
        {
            switch (exchange)
            {
                case ExchangeTypeEnum.PLAYER_TRADE:
                    Trade = argv != null ? (PlayerTrade)argv : new PlayerTrade(this, (Character)target);
                    break;

                case ExchangeTypeEnum.NPC_TRADE:
                    Trade = argv != null ? (NpcTrade)argv : new NpcTrade(this, (Npc)target);
                    break;

                case ExchangeTypeEnum.CRAFT:
                    Trade = argv != null ? (CraftTrade)argv : new CraftTrade(this, (Character)target);
                    break;

                case ExchangeTypeEnum.FORGEMAGIE:
                    Trade = argv != null ? (FMTrade)argv : new FMTrade(this, (Character)target);
                    break;
                case ExchangeTypeEnum.TAXCOLLECTOR:
                    Trade = argv != null ? (TaxCollectorTrade)argv : new TaxCollectorTrade(this, (TaxCollector)target);
                    break;

                case ExchangeTypeEnum.STORAGE:
                    if (argv != null)
                        Trade = (Trade)argv;
                    else if (target is House)
                        Trade = new HouseChestTrade(this, (House)target);
                    else
                        Trade = new BankTrade(this);
                    break;

                case ExchangeTypeEnum.SHOP_STOCK:
                    Trade = argv != null ? (Trade)argv : new MerchantStockTrade(this);
                    break;

                case ExchangeTypeEnum.DISCONNECTED_VENDOR:
                    Trade = argv != null ? (Trade)argv : new MerchantCustomerTrade(this, (MerchantActor)target);
                    break;

                case ExchangeTypeEnum.MOUNT:
                    Trade = argv != null ? (Trade)argv : Trade;
                    break;

            }
        }

        public void SetTradeRequest(ExchangeTypeEnum exchange, Character target)
        {
            switch (exchange)
            {
                case ExchangeTypeEnum.PLAYER_TRADE:
                    InventoryHandler.SendExchangeRequestedTradeMessage(Client, exchange, this, target);
                    InventoryHandler.SendExchangeRequestedTradeMessage(target.Client, exchange, this, target);
                    this.SetTrade(exchange, null, target);
                    target.SetTrade(exchange, Trade);
                    break;
            }
        }

        public void LeaveTrade()
        {
            this.Trade.Cancel();
        }

        public void AddExperience(long amount)
        {
            if (amount <= 0)
                return;

            if (long.MaxValue - Experience < amount)
                Experience = long.MaxValue;
            else
                Experience += amount;
        }

        public void AddHonor(ushort amount)
        {
            Alignment.Honor += amount;
        }

        public void SubHonor(ushort amount)
        {
            if (Alignment.Honor - amount < 0)
                Alignment.Honor = 0;
            else
                Alignment.Honor -= amount;
        }

        public void AddDishonor(ushort amount)
        {
            Alignment.Dishonor += amount;
        }

        public void SubDishonor(ushort amount)
        {
            if (Alignment.Dishonor - amount < 0)
                Alignment.Dishonor = 0;
            else
                Alignment.Dishonor -= amount;
        }

        public void LevelUp(byte levelAdded)
        {
            if (Level >= 200 || levelAdded <= 0)
                return;

            var targetLevel = Math.Min(200, Level + levelAdded);
            var appliedLevelAdded = targetLevel - Level;
            if (appliedLevelAdded <= 0)
                return;

            OnLevelChanged(Level, Level = (short)targetLevel);
        }

        private void OnLevelChanged(int lastLevel, int newLevel)
        {
            Experience = ExperienceLevelFloor;
            Stats[StatsEnum.Health].Base += 5 * (newLevel - lastLevel);
            Stats.Record.DamageTaken = 0;
            StatsPoints += 5 * (newLevel - lastLevel);
            SpellsPoints += (newLevel - lastLevel);

            if (newLevel >= 100 && lastLevel < 100)
                Stats[StatsEnum.AP].Base++;

            Shortcuts.AddNextShortcuts(ShortcutBarEnum.SPELL);
            CharacterHandler.SendCharacterLevelUpMessage(Client, (byte)Level);
            Map.Clients.ForEach(x => CharacterHandler.SendCharacterLevelUpInformationMessage(x, this, (byte)Level));
            Stats.Update();
            RefreshStats();
        }

        public int GetBaseStatsPoints()
        {
            return Math.Max(0, ((int)Level - 1) * 5);
        }

        public int GetBaseSpellsPoints()
        {
            return Math.Max(1, (int)Level);
        }
        public void ResetCharacteristicsToBase(bool resetSpellLevels = true)
        {
            Stats[StatsEnum.Strength].Base = 0;
            Stats[StatsEnum.Intelligence].Base = 0;
            Stats[StatsEnum.Chance].Base = 0;
            Stats[StatsEnum.Agility].Base = 0;
            Stats[StatsEnum.Wisdom].Base = 0;
            Stats[StatsEnum.Vitality].Base = 0;

            StatsPoints = GetBaseStatsPoints();

            if (resetSpellLevels)
            {
                Spells.ResetAllSpellLevelsToBase();
                SpellsPoints = GetBaseSpellsPoints();
            }

            Stats.RefreshCharacterDerivedStats(this);

            if (Stats.Health.Taken < 0)
                Stats.Health.Taken = 0;

            if (Stats.Health.Taken > Stats.Health.TotalMax)
                Stats.Health.Taken = Stats.Health.TotalMax;

            Stats.Update();
            CharacterHandler.SendUpdateLifePointsMessage(Client);
            InventoryHandler.SendSpellListMessage(Client, true);
            RefreshStats();
        }

        public void ApplyParchotage101()
        {
            Stats[StatsEnum.Strength].Base = Math.Max(Stats[StatsEnum.Strength].Base, 101);
            Stats[StatsEnum.Intelligence].Base = Math.Max(Stats[StatsEnum.Intelligence].Base, 101);
            Stats[StatsEnum.Chance].Base = Math.Max(Stats[StatsEnum.Chance].Base, 101);
            Stats[StatsEnum.Agility].Base = Math.Max(Stats[StatsEnum.Agility].Base, 101);
            Stats[StatsEnum.Wisdom].Base = Math.Max(Stats[StatsEnum.Wisdom].Base, 101);
            Stats[StatsEnum.Vitality].Base = Math.Max(Stats[StatsEnum.Vitality].Base, 101);

            Stats.RefreshCharacterDerivedStats(this);
            Stats.Update();
            CharacterHandler.SendUpdateLifePointsMessage(Client);
            RefreshStats();
        }

        public void TogglePvPMode(bool state)
        {
            Alignment.Enabled = state;
            Map.Refresh(this);
            RefreshStats();
        }

        public void ChangeAlignementSide(AlignmentSideEnum side)
        {
            Alignment.Side = side;
            Alignment.Honor = 0;
            TogglePvPMode(false);
        }

        public void StartRegen(byte regenRate = 20)
        {
            if (Stats != null && Stats.Health != null && Stats.Health.Total >= Stats.Health.TotalMax)
            {
                if (_isInRegen)
                    StopRegen();
                else if (Client != null)
                    CharacterHandler.SendUpdateLifePointsMessage(Client);

                return;
            }

            if (_isInRegen)
                StopRegen();
            _isInRegen = true;
            _regenRate = regenRate;
            _regenStartTime = DateTime.Now;
            CharacterHandler.SendLifePointsRegenBeginMessage(Client, regenRate);
        }

        public void StopRegen()
        {
            if (_isInRegen)
            {
                int lifePointsGained = (int)Math.Floor((DateTime.Now - _regenStartTime).TotalSeconds / (_regenRate / 10f));
                if (Stats.Health.Total + lifePointsGained > Stats.Health.TotalMax)
                    lifePointsGained = Stats.Health.TotalMax - Stats.Health.Total;

                if (lifePointsGained < 0)
                    lifePointsGained = 0;

                Stats.Health.Taken = Math.Max(0, Stats.Health.Taken - lifePointsGained);
                Stats.Update();
                CharacterHandler.SendLifePointsRegenEndMessage(Client, lifePointsGained);

                // On renvoie les PV serveur juste après l'arrêt de la regen pour éviter
                // que le client garde une prédiction locale trop haute ou full vie.
                if (Client != null)
                    CharacterHandler.SendUpdateLifePointsMessage(Client);

                _isInRegen = false;
            }
        }

        public void ResetRegenTimerAfterManualHeal()
        {
            if (!_isInRegen)
                return;

            if (Stats == null || Stats.Health == null)
                return;

            if (Stats.Health.Total >= Stats.Health.TotalMax)
            {
                StopRegen();
                return;
            }

            _regenStartTime = DateTime.Now;
        }

        public void ApplyLifeAfterFight(int remainingLife, bool wasDead, bool notifyClient = true)
        {
            if (Stats == null || Stats.Health == null)
                return;

            // Fin de combat : on enlève l'érosion et les bonus de combat, mais on ne soigne plus full vie.
            // Le joueur garde ses PV restants; s'il est mort, il revient à 1 PV pour pouvoir utiliser du pain.
            Stats.Health.PermanentTaken = 0;

            int maxLife = Math.Max(1, Stats.Health.TotalMax);
            int wantedLife = wasDead || remainingLife <= 0 ? 1 : Math.Min(remainingLife, maxLife);

            Stats.Health.Taken = Math.Max(0, maxLife - wantedLife);
            Stats.RefreshCharacterDerivedStats(this);
            Stats.Update();

            if (notifyClient && Client != null)
            {
                CharacterHandler.SendUpdateLifePointsMessage(Client);
                RefreshStats();
            }
        }

        public void RestoreFullLifeAfterFight(bool notifyClient = true)
        {
            if (Stats == null || Stats.Health == null)
                return;

            ApplyLifeAfterFight(Stats.Health.Total, Stats.Health.Total <= 0, notifyClient);
        }

        public void RefreshStats()
        {
            CharacterHandler.SendCharacterStatsListMessage(Client);
        }

        public void RefreshActor()
        {
            Map.Refresh(this);
        }

        public void PlayEmote(EmotesEnum emote)
        {
            if (emote == EmotesEnum.EMOTE_AURA_DE_PUISSANCE && this.Level < 200 || this.IsInFight())
                return;

            short auraSkin = this.GetAuraSkin(emote);
            if (auraSkin != -1)
            {
                if (this.Look.AuraLook != null && this.Look.AuraLook.BonesID == auraSkin)
                    this.Look.RemoveAuras();
                else
                    this.Look.SetAuraSkin(auraSkin);
                this.RefreshActor();
            }
            ContextRoleplayHandler.SendEmotePlayMessage(Map.Clients, this, emote);
        }

        public short GetAuraSkin(EmotesEnum auraEmote)
        {
            short result;
            switch (auraEmote)
            {
                case EmotesEnum.EMOTE_AURA_DE_PUISSANCE:
                    result = 170;
                    break;
                case EmotesEnum.EMOTE_AURA_VAMPYRIQUE:
                    result = 171;
                    break;
                default:
                    result = -1;
                    break;
            }
            return result;
        }

        public void UpdateLook(bool isCustomLook = false)
        {
            ActorLook rebuiltLook = null;

            if (isCustomLook && !string.IsNullOrWhiteSpace(Record.CustomLook))
            {
                try
                {
                    rebuiltLook = EntityManager.Instance.GetActorLook(Record.CustomLook);
                }
                catch
                {
                    rebuiltLook = null;
                }
            }

            if (rebuiltLook == null)
            {
                var baseLookString = BreedManager.Instance.GetLook(Breed, Sex);
                rebuiltLook = !string.IsNullOrWhiteSpace(baseLookString)
                    ? EntityManager.Instance.GetActorLook(baseLookString)
                    : new ActorLook();

                try
                {
                    var storedLook = !string.IsNullOrWhiteSpace(Record.EntityLook)
                        ? EntityManager.Instance.GetActorLook(Record.EntityLook)
                        : null;

                    if (storedLook != null && storedLook.Colors != null)
                    {
                        rebuiltLook.Colors.Clear();
                        foreach (var color in storedLook.Colors)
                            rebuiltLook.AddColor(color.Key, color.Value);
                    }
                }
                catch
                {
                }
            }

            Look = rebuiltLook ?? new ActorLook();

            if (Inventory != null)
            {
                foreach (var equippedItem in Inventory.GetEquipedItems().Where(x => x != null))
                {
                    bool isPetSlot = equippedItem.Position == CharacterInventoryPositionEnum.ACCESSORY_POSITION_PETS
                        || equippedItem.Type == ItemTypeEnum.FAMILIER
                        || equippedItem.Type == ItemTypeEnum.FANTOME_DE_FAMILIER
                        || equippedItem.Type == ItemTypeEnum.MONTILIER
                        || equippedItem.Type == ItemTypeEnum.FANTOME_DE_MONTILIER;

                    if (isPetSlot)
                        continue;

                    if (equippedItem.AppearanceId != 0)
                        Look.AddSkin(equippedItem.AppearanceId);
                }
            }

            if (Client != null && (Map != null || IsInFight()))
                OnLookRefreshed();
        }

        public void UpdateLook(BasePlayerItem item, bool isRemoved = false)
        {
            UpdateLook(!string.IsNullOrWhiteSpace(Record.CustomLook));
        }

        public void OnLookRefreshed()
        {
            if (IsInFight() && !Fighter.IsDead())
                ContextHandler.SendGameContextRefreshEntityLookMessage(Fight.Clients, Fighter.Id, GetDisplayedEntityLook());
            else
                ContextHandler.SendGameContextRefreshEntityLookMessage(Map.Clients, Id, GetDisplayedEntityLook());
        }

        public override void StartMove(Path path)
        {
            if (path == null)
                return;

            if (IsInFight())
            {
                var controlledSlave = Fighter?.GetSlave();
                if (controlledSlave != null && Fighter.IsSlaveTurn())
                    controlledSlave.StartMove(path);
                else
                    Fighter.StartMove(path);
                return;
            }

            bool wasMoving = IsMapMoving;
            IsMapMoving = true;

            var targetMonsterGroup = GetMonsterGroupOnCell(path.EndCell);

            if (!wasMoving && targetMonsterGroup != null)
                ArmPendingMonsterFight(targetMonsterGroup);
            else
                ClearPendingMonsterFight();

            Cell.Id = path.EndCell;
            Direction = (int)path.GetEndCellDirection();
            ContextHandler.SendGameMapMovementMessage(Map.Clients, Id, path.GetServerPathKeys());
        }

        public void CancelMapMove()
        {
            BasicHandler.SendBasicNoOperationMessage(Client);
            IsMapMoving = false;
            ClearPendingMonsterFight();
        }

        public void StopMove(bool allowPendingMonsterFight = true)
        {
            BasicHandler.SendBasicNoOperationMessage(Client);

            bool wasMoving = IsMapMoving;
            IsMapMoving = false;

            if (IsInFight())
            {
                ClearPendingMonsterFight();
                return;
            }

            if (!allowPendingMonsterFight || !wasMoving)
            {
                ClearPendingMonsterFight();
                return;
            }

            var monsterGroup = GetMonsterGroupOnCell(Cell.Id);
            if (monsterGroup != null && CanStartPendingMonsterFight(monsterGroup))
            {
                Map.EnsureFightCells();

                if (Map.RedCells == null || Map.BlueCells == null
                    || Map.BlueCells.Count <= 0 || Map.RedCells.Count <= 0)
                {
                    ClearPendingMonsterFight();
                    SendServerMessage("This map doesn't have fight cells.", Color.Red);
                    return;
                }

                if (!TryBeginMonsterFightLaunch(monsterGroup))
                {
                    ClearPendingMonsterFight();
                    return;
                }

                bool fightStarted = false;

                try
                {
                    LastMonsterFightLaunchUtc = DateTime.UtcNow;
                    ClearPendingMonsterFight();

                    SetFight(FightTypeEnum.FIGHT_TYPE_PvM);

                    if (Fight is global::Sunshine.WorldServer.Game.Fights.Types.FightPvM pvmFight)
                        pvmFight.SourceMonsterGroup = monsterGroup;

                    foreach (var monster in monsterGroup.Monsters)
                        Fight.AddFighter(new MonsterFighter(monster, Fight));

                    Fight.AddFighter(Fighter = new CharacterFighter(this), true);

                    Map.LeaveActor(monsterGroup);
                    Map.RolePlayActors.Remove(monsterGroup);
                    fightStarted = true;
                }
                finally
                {
                    EndMonsterFightLaunch(monsterGroup, fightStarted);
                }

                return;
            }

            ClearPendingMonsterFight();

            Maps.Houses.House currentHouse = null;

            if (LastTargetedHouse != null &&
                LastTargetedHouse.MatchesInteriorExitTrigger(Map.Id, Cell.Id))
            {
                currentHouse = LastTargetedHouse;
            }
            else
            {
                currentHouse = Maps.Houses.HouseManager.Instance.ResolveInteriorHouseByTriggerCell(Map.Id, Cell.Id, this);
            }

            if (currentHouse != null && currentHouse.TryGetExitDestination(out int houseExitMapId, out short houseExitCellId))
            {
                ClearHouseBinding(true);
                Teleport(houseExitMapId, houseExitCellId);
                return;
            }

            var trigger = Map.Triggers.FirstOrDefault(x => x.Map == Map.Id && x.Cell == Cell.Id);
            if (trigger != null)
                TriggerDispatcher.Dispatch(Client, trigger);
        }

        public FighterRefusedReasonEnum CanRequestFight(Character target)
        {
            if (!target.IsInWorld() || target.IsInFight())
                return FighterRefusedReasonEnum.OPPONENT_OCCUPIED;

            if (!IsInWorld() || IsInFight())
                return FighterRefusedReasonEnum.IM_OCCUPIED;

            if (target == this)
                return FighterRefusedReasonEnum.FIGHT_MYSELF;

            if (target.Map != Map || !Map.AllowChallenge)
                return FighterRefusedReasonEnum.WRONG_MAP;

            return FighterRefusedReasonEnum.FIGHTER_ACCEPTED;
        }

        public FighterRefusedReasonEnum CanAttack(TaxCollector target)
        {
            if (target.Fight != null)
                return FighterRefusedReasonEnum.OPPONENT_OCCUPIED;

            if (!IsInWorld() || IsInFight())
                return FighterRefusedReasonEnum.IM_OCCUPIED;

            if (target.Map != Map || !Map.AllowChallenge)
                return FighterRefusedReasonEnum.WRONG_MAP;

            return FighterRefusedReasonEnum.FIGHTER_ACCEPTED;
        }

        public FighterRefusedReasonEnum CanAgress(Character target)
        {
            if (target == this)
                return FighterRefusedReasonEnum.FIGHT_MYSELF;

            if (!target.Alignment.Enabled || !Alignment.Enabled)
                return FighterRefusedReasonEnum.INSUFFICIENT_RIGHTS;

            if (!target.IsInWorld() || target.IsInFight())
                return FighterRefusedReasonEnum.OPPONENT_OCCUPIED;

            if (!IsInWorld() || IsInFight())
                return FighterRefusedReasonEnum.IM_OCCUPIED;

            if (Alignment.Side <= AlignmentSideEnum.ALIGNMENT_NEUTRAL || target.Alignment.Side <= AlignmentSideEnum.ALIGNMENT_NEUTRAL)
                return FighterRefusedReasonEnum.WRONG_ALIGNMENT;

            if (target.Alignment.Side == Alignment.Side)
                return FighterRefusedReasonEnum.WRONG_ALIGNMENT;

            if (target.Map != Map /*|| !Map.AllowAggression)*/)
                return FighterRefusedReasonEnum.WRONG_MAP;

            if (target.Client.Ip == Client.Ip)
                return FighterRefusedReasonEnum.MULTIACCOUNT_NOT_ALLOWED;

            if (Level - target.Level > 20)
                return FighterRefusedReasonEnum.INSUFFICIENT_RIGHTS;

            return FighterRefusedReasonEnum.FIGHTER_ACCEPTED;
        }

        public void SendServerMessage(string message)
        {
            BasicHandler.SendTextInformationMessage(Client, TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 0, new object[] { message });
        }

        public void SendServerMessage(string message, Color color)
        {
            SendServerMessage(string.Format("<font color=\"#{0}\">{1}</font>", color.ToArgb().ToString("X"), message));
        }

        public void SendInformationMessage(TextInformationTypeEnum msgType, short msgId, params object[] parameters)
        {
            BasicHandler.SendTextInformationMessage(Client, msgType, msgId, parameters);
        }

        public void SendNotificationByServerMessage(string message, NotificationEnum notification, bool isForced = true)
        {
            BasicHandler.SendNotificationByServerMessage(Client, message, notification, isForced);
        }

        public void LogOut()
        {
            if (this.IsInParty)
                this.LeaveParty();
            if (this.IsInFight())
                this.LeaveFight();
            if (this.IsInTrade())
                this.LeaveTrade();
            DungeonPartyFinderManager.Instance.UnregisterPlayer(Id);
            this.LeaveMap(Map);
            Save();
        }

        public void LoadRecord()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(Record.CustomLook))
                    Look = EntityManager.Instance.GetActorLook(Record.CustomLook);
                else
                    Look = EntityManager.Instance.GetActorLook(Record.EntityLook);
            }
            catch
            {
                try
                {
                    Look = EntityManager.Instance.GetActorLook(Record.EntityLook);
                    Record.CustomLook = string.Empty;
                }
                catch
                {
                    Look = new ActorLook();
                    Record.CustomLook = string.Empty;
                }
            }

            try
            {
                bool savedRidingState = Record != null && Record.IsRiding;
                _equippedMount = null;

                if (Record.EquippedMount.HasValue)
                    EquippedMount = MountManager.Instance.GetMount(Record.EquippedMount.Value);

                var acceptedMountOwnerIds = new HashSet<int> { Id };
                if (Account != null)
                    acceptedMountOwnerIds.Add(Account.Id);

                if (EquippedMount != null &&
                    EquippedMount.Record != null &&
                    EquippedMount.Record.OwnerId.HasValue &&
                    acceptedMountOwnerIds.Contains(EquippedMount.Record.OwnerId.Value))
                {
                    bool repairedEquippedMountState = false;

                    if (EquippedMount.Record.IsInStable > 0)
                    {
                        EquippedMount.Record.IsInStable = 0;
                        repairedEquippedMountState = true;
                    }

                    if (EquippedMount.Record.PaddockId.HasValue)
                    {
                        EquippedMount.Record.PaddockId = null;
                        repairedEquippedMountState = true;
                    }

                    if (EquippedMount.Record.StoredSince.HasValue)
                    {
                        EquippedMount.Record.StoredSince = null;
                        repairedEquippedMountState = true;
                    }

                    if (repairedEquippedMountState)
                        MountManager.Instance.Save(EquippedMount);
                }

                if (EquippedMount == null ||
                    EquippedMount.Record == null ||
                    !EquippedMount.Record.OwnerId.HasValue ||
                    !acceptedMountOwnerIds.Contains(EquippedMount.Record.OwnerId.Value))
                {
                    EquippedMount = null;
                    if (Record != null)
                    {
                        Record.EquippedMount = null;
                        Record.IsRiding = false;
                    }
                }

                if (EquippedMount == null)
                {
                    EquippedMount = MountManager.Instance.GetOwnerMounts(Id)
                        .FirstOrDefault(x => x != null && x.Record != null && !x.IsInStable && !x.Record.PaddockId.HasValue && !x.Record.StoredSince.HasValue);

                    if (EquippedMount != null && Record != null && !Record.EquippedMount.HasValue)
                        Record.EquippedMount = EquippedMount.Id;
                }

                IsRiding = EquippedMount != null && savedRidingState;
            }
            catch
            {
                EquippedMount = null;
                IsRiding = false;
            }

            Stats = new StatsFields(this);
            NormalizeBaseHealthForLevel();
            Alignment = new CharacterAlignment(this);
            Spells = new SpellInventory(this);
            Inventory = new Inventory(this);
            Shortcuts = new ShortcutBar(this);
            RecoverOrphanedEquippedMount();

            var equippedPet = Inventory.GetItem(CharacterInventoryPositionEnum.ACCESSORY_POSITION_PETS);
            if (EquippedMount != null && equippedPet != null)
                IsRiding = false;

            if (EquippedMount != null && IsRiding)
                EquippedMount.ApplyMountEffects(this, false);

            Shortcuts.CleanupInvalidItemShortcuts(false);
            UpdateLook(!string.IsNullOrWhiteSpace(Record.CustomLook));
            BidHouseBag = new BidHouseBag(this);
            Jobs = new JobsCollection(this);
            Quests = new QuestsCollection(this);
            GuildMember = GuildManager.Instance.GetGuildMember(this);
            SyncPaddockInstanceStateOnLoad();
            SyncHouseStateOnLoad();

            Map = GetRuntimeAwareCurrentMap();
            if (Map == null)
                RecoverFromBrokenHouseState();

            SyncPaddockInstanceStateOnLoad();
            SyncHouseStateOnLoad();
            Map = GetRuntimeAwareCurrentMap();
            Cell = MapManager.Instance.GetCell(this);
            Zaaps = Record.Zaaps == null || Record.Zaaps == "" ? new List<Map>() :
                    Record.Zaaps.Split(',').Select(x => MapManager.Instance.GetMap(x)).Where(x => x != null).ToList();
        }


        private void NormalizeBaseHealthForLevel()
        {
            int expectedBaseHealth = 50 + (Level * 5);

            if (Stats == null || Stats[StatsEnum.Health] == null)
                return;

            if (Stats[StatsEnum.Health].Base != expectedBaseHealth)
            {
                Stats[StatsEnum.Health].Base = expectedBaseHealth;

                var healthStat = Stats.Health;
                if (healthStat != null)
                {
                    if (healthStat.Taken < 0)
                        healthStat.Taken = 0;

                    int totalMax = healthStat.TotalMax;
                    if (healthStat.Taken > totalMax)
                        healthStat.Taken = totalMax;
                }

                Stats.Update();
            }
        }

        private void Save()
        {
            Look?.RemoveAuras();
            CharacterManager.Instance.Save(this);
            Client = null;
            CharacterManager.Instance.Characters[Id] = this;
        }
    }
}
