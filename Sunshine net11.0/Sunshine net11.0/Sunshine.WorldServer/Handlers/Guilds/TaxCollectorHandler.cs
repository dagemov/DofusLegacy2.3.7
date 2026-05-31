using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors.TaxCollectors;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Exchanges;
using Sunshine.WorldServer.Game.Fights.Types;
using Sunshine.WorldServer.Game.Guilds;
using Sunshine.WorldServer.Handlers.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Handlers.Guilds
{
    public class TaxCollectorHandler : WorldPacketHandler
    {
        [WorldHandler(5682)]
        public static void HandleTaxCollectorFireRequestMessage(WorldClient client, TaxCollectorFireRequestMessage message)
        {
            if (client.Character.Guild != null)
            {
                TaxCollector taxCollector = TaxCollectorManager.Instance.GetTaxCollectors(client.Character.Guild.Id).FirstOrDefault(x => x.Id == message.collectorId);

                if (taxCollector != null)
                    TaxCollectorManager.Instance.DeleteTaxCollector(taxCollector, client.Character);
            }
        }

        [WorldHandler(5779)]
        public static void HandleExchangeRequestOnTaxCollectorMessage(WorldClient client, ExchangeRequestOnTaxCollectorMessage message)
        {
            if (client.Character.Guild != null)
            {
                TaxCollector taxCollector = TaxCollectorManager.Instance.GetTaxCollectors(client.Character.Guild.Id).FirstOrDefault(x => x.Id == message.taxCollectorId);

                if (taxCollector != null && taxCollector.Fight == null)
                {
                    if (!client.Character.GuildMember.HasRight(GuildRightsBitEnum.GUILD_RIGHT_COLLECT) || !client.Character.GuildMember.HasRight(GuildRightsBitEnum.GUILD_RIGHT_COLLECT_MY_TAX_COLLECTOR))
                        TaxCollectorHandler.SendTaxCollectorErrorMessage(client, TaxCollectorErrorReasonEnum.TAX_COLLECTOR_NOT_OWNED);
                    else
                    {
                        client.Character.SetTrade(ExchangeTypeEnum.TAXCOLLECTOR, null, taxCollector);
                        client.Character.Trade.Open();
                    }
                }
            }
        }


        [WorldHandler(5681)]
        public static void HandleTaxCollectorHireRequestMessage(WorldClient client, TaxCollectorHireRequestMessage message)
        {
            if (client.Character.Guild != null)
                TaxCollectorManager.Instance.AddTaxCollector(client.Character);
        }


        [WorldHandler(5954)]
        public static void HandleGameRolePlayTaxCollectorFightRequestMessage(WorldClient client, GameRolePlayTaxCollectorFightRequestMessage message)
        {
            var taxCollector = client.Character.Map.GetActor(message.taxCollectorId);

            if (taxCollector != null)
            {
                FighterRefusedReasonEnum fighterRefusedReasonEnum = client.Character.CanAttack(taxCollector as TaxCollector);

                if (fighterRefusedReasonEnum != FighterRefusedReasonEnum.FIGHTER_ACCEPTED)
                    ContextHandler.SendChallengeFightJoinRefusedMessage(client, client.Character, fighterRefusedReasonEnum);
                else
                {
                    client.Character.SetFight(FightTypeEnum.FIGHT_TYPE_PvT);
                    var fight = client.Character.Fight;
                    (fight as FightPvT).TaxCollector = taxCollector as TaxCollector;
                    fight.AddFighter((taxCollector as TaxCollector).Fighter = new TaxCollectorFighter(taxCollector as TaxCollector, fight));
                    fight.AddFighter(client.Character.Fighter = new CharacterFighter(client.Character), true);
                    for (int i = 0; i < (taxCollector as TaxCollector).Guild.Members.Count; i++)
                    {
                        if ((taxCollector as TaxCollector).Guild.Members[i].IsConnected())
                            TaxCollectorHandler.SendTaxCollectorAttackedMessage((taxCollector as TaxCollector).Guild.Members[i].Client, taxCollector as TaxCollector);
                    }
                    fight.Map.LeaveActor(taxCollector);
                }
            }
        }

        [WorldHandler(5717)]
        public static void HandleGuildFightJoinRequestMessage(WorldClient client, GuildFightJoinRequestMessage message)
        {
            if (client.Character.Guild != null)
            {
                TaxCollector taxCollector = client.Character.Guild.TaxCollectors.FirstOrDefault(x => x.Id == message.taxCollectorId);
                if (taxCollector != null && taxCollector.Fight != null && taxCollector.Guild == client.Character.Guild)
                {
                    FightPvT fightPvT = taxCollector.Fighter.Fight as FightPvT;
                    if (fightPvT != null)
                        fightPvT.AddDefender(client.Character);
                }
            }
        }
        [WorldHandler(5715)]
        public static void HandleGuildFightLeaveRequestMessage(WorldClient client, GuildFightLeaveRequestMessage message)
        {
            if (client.Character.Guild != null)
            {
                TaxCollector taxCollector = client.Character.Guild.TaxCollectors.FirstOrDefault(x => x.Id == message.taxCollectorId);
                if (taxCollector != null && taxCollector.Fight != null && taxCollector.Guild == client.Character.Guild)
                {
                    FightPvT fightPvT = taxCollector.Fighter.Fight as FightPvT;
                    if (fightPvT != null)
                        fightPvT.RemoveDefender(client.Character);
                }
            }
        }

        public static void SendTaxCollectorListMessage(WorldClient client, Guild guild)
        {
            client.Send(new TaxCollectorListMessage(guild.MaxTaxCollectors, guild.HireCost, guild.TaxCollectors.Select(x => x.GetTaxCollectorInformations()), 
                guild.TaxCollectors.Where(x => x.Fight != null).Select(x => (x.Fighter as TaxCollectorFighter).GetTaxCollectorFightersInformation())));
        }

        public static void SendTaxCollectorErrorMessage(WorldClient client, TaxCollectorErrorReasonEnum reason)
        {
            client.Send(new TaxCollectorErrorMessage((sbyte)reason));
        }

        public static void SendTaxCollectorMovementAddMessage(WorldClient client, TaxCollector taxCollector)
        {
            client.Send(new TaxCollectorMovementAddMessage(taxCollector.GetTaxCollectorInformations()));
        }

        public static void SendTaxCollectorMovementRemoveMessage(WorldClient client, TaxCollector taxCollector)
        {
            client.Send(new TaxCollectorMovementRemoveMessage(taxCollector.Id));
        }

        public static void SendExchangeStartOkTaxCollectorMessage(WorldClient client, TaxCollector taxCollector)
        {
            client.Send(new ExchangeStartOkTaxCollectorMessage(taxCollector.Id, taxCollector.Inventory.GetItems().Select(x => x.GetObjectItem()), taxCollector.Inventory.GatheredKamas));
        }

        public static void SendTaxCollectorMovementMessage(WorldClient client, TaxCollector taxCollector, bool hireOrFire, string playerName)
        {
            client.Send(new TaxCollectorMovementMessage(hireOrFire, taxCollector.GetTaxCollectorBasicInformations(), playerName));
        }

        public static void SendTaxCollectorAttackedMessage(WorldClient client, TaxCollector taxCollector)
        {
            client.Send(new TaxCollectorAttackedMessage(taxCollector.FirstName, taxCollector.LastName, (short)taxCollector.Map.Point.X, (short)taxCollector.Map.Point.Y, taxCollector.Map.Id));
        }

        public static void SendGuildFightPlayersHelpersJoinMessage(WorldClient client, TaxCollector taxCollector, Character character)
        {
            client.Send(new GuildFightPlayersHelpersJoinMessage(taxCollector.Id, character.GetCharacterMinimalPlusLookInformations()));
        }

        public static void SendGuildFightPlayersHelpersLeaveMessage(WorldClient client, TaxCollector taxCollector, Character character)
        {
            client.Send(new GuildFightPlayersHelpersLeaveMessage(taxCollector.Id, character.Id));
        }

        public static void SendGuildFightPlayersEnemyRemoveMessage(WorldClient client, TaxCollector taxCollector, Character character)
        {
            client.Send(new GuildFightPlayersEnemyRemoveMessage(taxCollector.Id, character.Id));
        }

        public static void SendGuildFightPlayersEnemiesListMessage(WorldClient client, TaxCollector taxCollector, System.Collections.Generic.IEnumerable<Character> characters)
        {
            client.Send(new GuildFightPlayersEnemiesListMessage(taxCollector.Id,
                from x in characters
                select x.GetCharacterMinimalPlusLookInformations()));
        }

        public static void SendTaxCollectorAttackedResultMessage(WorldClient client, bool deadOrAlive, TaxCollector taxCollector)
        {
            client.Send(new TaxCollectorAttackedResultMessage(deadOrAlive, taxCollector.GetTaxCollectorBasicInformations()));
        }
    }
}
