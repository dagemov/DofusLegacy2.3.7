using Dapper;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.World.Guilds;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Actors.TaxCollectors;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Effects;
using Sunshine.WorldServer.Game.Guilds;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Handlers.Guilds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.Managers
{
    public class TaxCollectorManager : Singleton<TaxCollectorManager>
    {
        private UniqueIdProvider m_idProvider = new UniqueIdProvider();

        public Dictionary<int, List<TaxCollector>> TaxCollectors = new Dictionary<int, List<TaxCollector>>();

        public Dictionary<int, List<TaxCollector>> GetAllTaxCollectorsById()
        {
            Dictionary<int, List<TaxCollector>> m_taxCollector = new Dictionary<int, List<TaxCollector>>();

            var taxCollectorSpawns = DatabaseManager.Connection.Query<TaxCollectorSpawn>($"SELECT * FROM worlds_taxcollectors");

            foreach (var tCollector in taxCollectorSpawns)
            {
                TaxCollector taxCollector = new TaxCollector(tCollector);

                taxCollector.Map.EnterActor(taxCollector);

                if (m_taxCollector.ContainsKey(tCollector.Guild))
                    m_taxCollector[tCollector.Guild].Add(taxCollector);
                else
                    m_taxCollector.Add(tCollector.Guild, new List<TaxCollector> { taxCollector });
            }

            return m_taxCollector;
        }

        public List<TaxCollector> GetTaxCollectors(int guildId)
        {
            if (TaxCollectors.ContainsKey(guildId))
                return TaxCollectors[guildId];
            return new List<TaxCollector>();
        }

        public Dictionary<int, List<BasePlayerItem>> GetTaxCollectorItems(int taxCollectorId)
        {
            var taxItems = DatabaseManager.Connection.Query<TaxCollectorItemsRecord>($"SELECT * FROM taxcollectors_items WHERE Id = '{taxCollectorId}'");

            Dictionary<int, List<BasePlayerItem>> items = new Dictionary<int, List<BasePlayerItem>>();

            foreach (var tax in taxItems)
            {
                BasePlayerItem playerItem = new BasePlayerItem(tax.Item)
                {
                    Id = ItemManager.Instance.GenerateId(),
                    Position = CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED,
                    Stack = tax.Stack,
                    Effects = EffectManager.Instance.GetEffects(tax.Effects),
                    EffectSets = ItemManager.Instance.Items[tax.Item].EffectSets
                };

                if (items.ContainsKey(tax.Item))
                    items[tax.Item].Add(playerItem);
                else
                    items.Add(tax.Item, new List<BasePlayerItem>() { playerItem });
            }
            return items;
        }

        public int GenerateId()
        {
            return TaxCollectors.Count <= 0 ? 1 : TaxCollectors.Last().Key + 1;
        }

        public void AddTaxCollector(Character character)
        {
            if (!character.GuildMember.HasRight(GuildRightsBitEnum.GUILD_RIGHT_HIRE_TAX_COLLECTOR))
                TaxCollectorHandler.SendTaxCollectorErrorMessage(character.Client, TaxCollectorErrorReasonEnum.TAX_COLLECTOR_NO_RIGHTS);
            else
            {
                if (character.Guild.TaxCollectors.Count >= character.Guild.MaxTaxCollectors)
                    TaxCollectorHandler.SendTaxCollectorErrorMessage(character.Client, TaxCollectorErrorReasonEnum.TAX_COLLECTOR_MAX_REACHED);
                else
                {
                    if (character.Map.HasTaxCollector())
                        TaxCollectorHandler.SendTaxCollectorErrorMessage(character.Client, TaxCollectorErrorReasonEnum.TAX_COLLECTOR_ALREADY_ONE);
                    else
                    {
                        if (character.Inventory.Kamas < character.Guild.HireCost)
                            TaxCollectorHandler.SendTaxCollectorErrorMessage(character.Client, TaxCollectorErrorReasonEnum.TAX_COLLECTOR_NOT_ENOUGH_KAMAS);
                        else
                        {
                            //if (!character.Position.Map.AllowCollector)
                            //{
                            //    character.Client.Send(new TaxCollectorErrorMessage(7));
                            //}
                            if (character.IsInFight())
                                TaxCollectorHandler.SendTaxCollectorErrorMessage(character.Client, TaxCollectorErrorReasonEnum.TAX_COLLECTOR_ERROR_UNKNOWN);
                            else
                            {
                                character.Inventory.SetKamas(-character.Guild.HireCost);
                                AsyncRandom rdn = new AsyncRandom();

                                TaxCollectorSpawn taxCollectorSpawn = new TaxCollectorSpawn
                                {
                                    Id = m_idProvider.Pop(),
                                    OwnerId = character.Id,
                                    Guild = character.Guild.Id,
                                    Map = character.Map.Id,
                                    Cell = character.Cell.Id,
                                    Direction = (sbyte)character.Direction,
                                    FirstName = (short)rdn.Next(1, 154),
                                    LastName = (short)rdn.Next(1, 253),
                                    CallerName = character.Name,
                                    Date = DateTime.Now,                                  
                                };

                                TaxCollector taxCollector = new TaxCollector(taxCollectorSpawn);
                                character.Map.EnterActor(taxCollector);

                                for (int i = 0; i < character.Guild.Members.Count; i++)
                                {
                                    if (character.Guild.Members[i].IsConnected())
                                        TaxCollectorHandler.SendTaxCollectorMovementAddMessage(character.Guild.Members[i].Client, taxCollector);
                                }

                                if (TaxCollectors.ContainsKey(character.Guild.Id))
                                    TaxCollectors[character.Guild.Id].Add(taxCollector);
                                else
                                    TaxCollectors.Add(character.Guild.Id, new List<TaxCollector> { taxCollector });
                            }

                        }
                    }
                }
            }
        }

        public void DeleteTaxCollector(TaxCollector taxCollector, Character character)
        {
            if (character != null)
            {
                if (!character.GuildMember.HasRight(GuildRightsBitEnum.GUILD_RIGHT_COLLECT) || !character.GuildMember.HasRight(GuildRightsBitEnum.GUILD_RIGHT_COLLECT_MY_TAX_COLLECTOR))
                    return;

                character.Map.LeaveActor(taxCollector);

                for (int i = 0; i < character.Guild.Members.Count; i++)
                {
                    if (character.Guild.Members[i].IsConnected())
                        TaxCollectorHandler.SendTaxCollectorMovementRemoveMessage(character.Guild.Members[i].Client, taxCollector);
                }

                TaxCollectors[character.Guild.Id].Remove(taxCollector);
            }         
        }

        public void DeleteTaxCollector(TaxCollector taxCollector, Guild guild)
        {
            TaxCollectors[guild.Id].Remove(taxCollector);

            for (int i = 0; i < guild.Members.Count; i++)
            {
                if (guild.Members[i].IsConnected())
                    TaxCollectorHandler.SendTaxCollectorMovementRemoveMessage(guild.Members[i].Client, taxCollector);
            }
        }
    }
}
