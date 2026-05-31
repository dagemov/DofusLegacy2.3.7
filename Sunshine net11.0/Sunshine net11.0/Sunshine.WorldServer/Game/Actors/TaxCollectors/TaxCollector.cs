using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Guilds;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Actors.Look;
using Sunshine.WorldServer.Game.Guilds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sunshine.Protocol.Utils.Extensions;
using Sunshine.WorldServer.Game.Maps.Pathfinding;
using Sunshine.WorldServer.Game.Actors.Stats;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Maps;
using System.Drawing;
using Sunshine.WorldServer.Handlers.Context;
using Sunshine.WorldServer.Game.Actors.TaxCollectors.Inventory;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Fights.Types;
using Sunshine.WorldServer.Game.Spells;

namespace Sunshine.WorldServer.Game.Actors.TaxCollectors
{
    public class TaxCollector : RolePlayActor
    {
        public TaxCollectorSpawn Record { get; set; }

        public Guild Guild { get { return GuildManager.Instance.GetGuild(Record.Guild); } }

        public StatsFields Stats { get; set; }

        public List<Spell> Spells { get; set; }

        public TaxInventory Inventory { get; set; }

        public TaxCollector(TaxCollectorSpawn record)
        {
            Record = record;
            Stats = new StatsFields(this);
            Inventory = new TaxInventory(this);
            Map = MapManager.Instance.GetMap(record.Map);
            string colors = string.Format("8={0},7={1}", Guild.EmblemBackgroundColor, Guild.EmblemForegroundColor);
            Look = EntityManager.Instance.GetActorLook("{714||"+colors+"}");
            FightLoot = new Fights.Results.FightLoot(this);
            RefreshSpells();
        }

        public override int Id { get { return Record.Id; } }

        public int OwnerId { get { return Record.OwnerId; } }

        public short Cell { get { return Record.Cell; } set { Record.Cell = value; } }

        public DirectionsEnum Direction { get { return (DirectionsEnum)Record.Direction; } set { Record.Direction = (sbyte)value; } }

        public DateTime Date { get { return Record.Date; } set { Record.Date = value; } }

        public short FirstName { get { return Record.FirstName; } set { Record.FirstName = value; } }

        public short LastName { get { return Record.LastName; } set { Record.LastName = value; } }

        public string CallerName { get { return Record.CallerName; } set { Record.CallerName = value; } }

        public double GatheredExperience { get { return Record.GatheredExperience; } set { Record.GatheredExperience = value; } }

        public int AttacksCount { get { return Record.AttacksCount; } set { Record.AttacksCount = value; } }

        public short Level { get { return Guild.Level; } }

        public ActorLook Look { get; set; }

        public FightActor Fighter { get; set; }

        public Fight Fight { get { return Fighter?.Fight; } }

        public Map Map { get; set; }

        public EntityDispositionInformations GetEntityDisposition()
        {
            return new EntityDispositionInformations(Cell, (sbyte)Direction);
        }

        public override GameRolePlayActorInformations GetGameRolePlayActorInformations()
        {
            return new GameRolePlayTaxCollectorInformations(Id, Look.GetEntityLook(), GetEntityDisposition(), FirstName, LastName, Guild.GetGuildInformations(), Guild.Level, Record.AttacksCount);
        }

        public TaxCollectorInformations GetTaxCollectorInformations()
        {
            if (this.Fight != null)
            {
                if (Fight.State == FightStateEnum.Placement)
                    return new TaxCollectorInformationsInWaitForHelpState(Id, FirstName, LastName, this.GetAdditionalTaxCollectorInformations(), (short)Map.Point.X, (short)Map.Point.Y, (short)Fighter.Position.Map.SubAreaId, 1, this.Look.GetEntityLook(), this.Inventory.GatheredKamas, (double)this.GatheredExperience, Inventory.GetWeight(), Inventory.GetValue(), GetProtectedEntityWaitingForHelpInfo());
                return new TaxCollectorInformations(Id, FirstName, LastName, this.GetAdditionalTaxCollectorInformations(), (short)Map.Point.X, (short)Map.Point.Y, (short)Map.SubAreaId, 2, this.Look.GetEntityLook(), this.Inventory.GatheredKamas, (double)this.GatheredExperience, Inventory.GetWeight(), Inventory.GetValue());
            }
            return new TaxCollectorInformations(Id, FirstName, LastName, this.GetAdditionalTaxCollectorInformations(), (short)Map.Point.X, (short)Map.Point.Y, (short)Map.SubAreaId, 0, this.Look.GetEntityLook(), this.Inventory.GatheredKamas, (double)this.GatheredExperience, Inventory.GetWeight(), Inventory.GetValue());
        }

        public AdditionalTaxCollectorInformations GetAdditionalTaxCollectorInformations()
        {
            return new AdditionalTaxCollectorInformations(CallerName, Date.GetUnixTimeStamp());
        }

        public TaxCollectorBasicInformations GetTaxCollectorBasicInformations()
        {
            return new TaxCollectorBasicInformations(FirstName, LastName, (short)Map.Point.X, (short)Map.Point.Y, Map.Id);
        }

        public ProtectedEntityWaitingForHelpInfo GetProtectedEntityWaitingForHelpInfo()
        {
            var fight = (Fight as FightPvT);
            return new ProtectedEntityWaitingForHelpInfo(fight.GetTimeLeftBeforeFight(), fight.GetWaitTimeForPlacement(), (sbyte)fight.GetDefendersLeftSlot());
        }

        public void InteractWith(NpcActionTypeEnum action, Character source)
        {
            switch (action)
            {
                case NpcActionTypeEnum.ACTION_TALK:
                    source.Client.Send(new NpcDialogCreationMessage(Map.Id, Id));
                    source.Client.Send(new TaxCollectorDialogQuestionExtendedMessage(Guild.GetBasicGuildInformations(), Guild.Pods, Guild.Prospecting, Guild.Wisdom, (sbyte)Guild.TaxCollectors.Count, AttacksCount, Inventory.GatheredKamas, GatheredExperience, Inventory.GetWeight(), Inventory.GetValue()));
                    break;
            }
        }

        public override void StartMove(Path path)
        {
            Cell = path.EndCell;
            Direction = path.GetEndCellDirection();
            ContextHandler.SendGameMapMovementMessage(Map.Clients, Id, path.GetServerPathKeys());
        }

        public void RefreshSpells()
        {
            Spells = new List<Spell>();

            var spells = Guild.Spells.ToList();
            var levels = Guild.SpellsLevels.ToList();

            for (int i = 0; i < spells.Count; i++)
            {
                if (levels[i] >= 2)
                {
                    Spell spell = SpellManager.Instance.Spells[spells[i]][levels[i] - 1];
                    Spells.Add(spell);
                }
            }
        }
    }
}
