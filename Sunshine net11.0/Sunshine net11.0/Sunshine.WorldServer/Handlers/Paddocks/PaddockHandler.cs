using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Dialogs.Paddocks;
using Sunshine.WorldServer.Game.Guilds;
using Sunshine.WorldServer.Game.Maps.Paddocks;
using Sunshine.WorldServer.Handlers.Context.Roleplay;
using Sunshine.WorldServer.Handlers.Context.RolePlay;
using Sunshine.WorldServer.Handlers.Dialogs;
using Sunshine.WorldServer.Handlers.Guilds;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Handlers.Paddocks
{
    public class PaddockHandler : WorldPacketHandler
    {
        private const int MaxPaddockPrice = 2000000000;
        private const int PageSize = 10;

        private sealed class PaddockSearchFilter
        {
            public int AreaId { get; set; }
            public sbyte AtLeastNbMount { get; set; }
            public sbyte AtLeastNbMachine { get; set; }
            public int MaxPrice { get; set; } = int.MaxValue;
        }

        private static readonly Dictionary<int, PaddockSearchFilter> FiltersByCharacterId = new Dictionary<int, PaddockSearchFilter>();

        public static void SendPaddockPropertiesMessage(WorldClient client, Paddock paddock)
        {
            if (client?.Character == null || paddock == null)
                return;

            var mounts = MountManager.Instance.GetPaddockedMountsByMap(paddock.MapId);
            client.Send(new PaddockPropertiesMessage(paddock.GetPropertiesForClient(client.Character, mounts)));
        }

        public static void SendPurchasableDialogMessage(WorldClient client, bool isSellDialog, int paddockId, int price)
        {
            if (client?.Character == null)
                return;

            var paddock = client.Character.Map != null
                ? PaddockManager.Instance.GetPaddockByMap(client.Character.Map.Id)
                : null;

            if (paddock == null && paddockId > 0)
                paddock = PaddockManager.Instance.GetPaddockById(paddockId);

            if (paddock == null)
                return;

            new PaddockBuySellDialog(client.Character, paddock, isSellDialog, price).Open();
        }

        [WorldHandler(PaddockBuyRequestMessage.Id)]
        public static void HandlePaddockBuyRequestMessage(WorldClient client, PaddockBuyRequestMessage message)
        {
            var paddock = GetCurrentPaddock(client);
            if (paddock == null)
                return;

            if (!paddock.CanBuy(client.Character))
            {
                client.Character.SendServerMessage("Vous ne pouvez pas acheter cet enclos.");
                ClosePaddockDialog(client);
                return;
            }

            int price = Math.Max(0, Math.Min(MaxPaddockPrice, paddock.SalePrice));
            if (price <= 0)
            {
                client.Character.SendServerMessage("Le prix de vente de cet enclos est invalide.");
                ClosePaddockDialog(client);
                return;
            }

            if (client.Character.Inventory.Kamas < price)
            {
                client.Character.SendServerMessage($"Vous n'avez pas assez de kamas. Prix de l'enclos : {price:N0} kamas.");
                ClosePaddockDialog(client);
                return;
            }

            if (client.Character.Guild == null || client.Character.GuildMember == null)
            {
                client.Character.SendServerMessage("Vous devez posséder une guilde pour acheter un enclos.");
                ClosePaddockDialog(client);
                return;
            }

            int? previousGuildId = paddock.GuildId;

            client.Character.Inventory.SetKamas(-price);
            paddock.AssignTo(client.Character);
            paddock.Price = price;
            PaddockManager.Instance.Save(paddock);

            ClosePaddockDialog(client);
            client.Character.SendServerMessage("Vous avez acheté l'enclos.");

            NotifyGuildPaddockBought(paddock, client.Character.Guild.Id);
            if (previousGuildId.HasValue && previousGuildId.Value > 0 && previousGuildId.Value != client.Character.Guild.Id)
                NotifyGuildPaddockRemoved(paddock, previousGuildId.Value);

            RefreshPaddockState(paddock, previousGuildId);
        }

        [WorldHandler(PaddockSellRequestMessage.Id)]
        public static void HandlePaddockSellRequestMessage(WorldClient client, PaddockSellRequestMessage message)
        {
            var paddock = GetCurrentPaddock(client);
            if (paddock == null)
                return;

            bool canSell = paddock.CanSell(client.Character);
            bool canModify = paddock.CanModifySalePrice(client.Character);

            if (!canSell && !canModify)
            {
                client.Character.SendServerMessage("Vous ne pouvez pas modifier la vente de cet enclos.");
                ClosePaddockDialog(client);
                return;
            }

            if (message.price <= 0)
            {
                if (paddock.OnSale)
                {
                    paddock.CancelSale();
                    PaddockManager.Instance.Save(paddock);
                    client.Character.SendServerMessage("La vente de l'enclos a été annulée.");
                    ClosePaddockDialog(client);
                    RefreshPaddockState(paddock, paddock.GuildId);
                }
                else
                {
                    client.Character.SendServerMessage("Mise en vente annulée.");
                    ClosePaddockDialog(client);
                }
                return;
            }

            int price = Math.Min(MaxPaddockPrice, message.price);
            paddock.PutForSale(price);
            PaddockManager.Instance.Save(paddock);

            ClosePaddockDialog(client);
            client.Character.SendServerMessage(canModify
                ? $"Le prix de vente de l'enclos a été modifié à {paddock.SalePrice:N0} kamas."
                : $"L'enclos est maintenant en vente pour {paddock.SalePrice:N0} kamas.");

            RefreshPaddockState(paddock, paddock.GuildId);
        }

        [WorldHandler(PaddockToSellListRequestMessage.Id)]
        public static void HandlePaddockToSellListRequestMessage(WorldClient client, PaddockToSellListRequestMessage message)
        {
            SendPaddockToSellList(client, message.pageIndex <= 0 ? 1 : message.pageIndex);
        }

        [WorldHandler(PaddockToSellFilterMessage.Id)]
        public static void HandlePaddockToSellFilterMessage(WorldClient client, PaddockToSellFilterMessage message)
        {
            if (client?.Character == null)
                return;

            FiltersByCharacterId[client.Character.Id] = new PaddockSearchFilter
            {
                AreaId = message.areaId,
                AtLeastNbMount = message.atLeastNbMount,
                AtLeastNbMachine = message.atLeastNbMachine,
                MaxPrice = message.maxPrice > 0 ? message.maxPrice : int.MaxValue
            };

            SendPaddockToSellList(client, 1);
        }

        [WorldHandler(GuildPaddockTeleportRequestMessage.Id)]
        public static void HandleGuildPaddockTeleportRequestMessage(WorldClient client, GuildPaddockTeleportRequestMessage message)
        {
            if (client?.Character?.Guild == null || client.Character.IsBusy())
                return;

            var paddock = PaddockManager.Instance.GetPaddockById(message.paddockId);
            if (paddock == null || !paddock.GuildId.HasValue || paddock.GuildId.Value != client.Character.Guild.Id || paddock.Map == null)
                return;

            if (!paddock.TpCell.HasValue || paddock.TpCell.Value < 0 || paddock.TpCell.Value >= 560)
                return;

            client.Character.Teleport(paddock.Map.Id, (short)paddock.TpCell.Value);
        }

        private static void SendPaddockToSellList(WorldClient client, int requestedPage)
        {
            if (client == null)
                return;

            var filter = GetFilter(client);
            var paddocks = PaddockManager.Instance.GetPaddocksToSell()
                .Where(x => MatchesFilter(x, filter))
                .Select(x => x.GetInformationsForSell())
                .ToList();

            short totalPages = paddocks.Count <= 0
                ? (short)0
                : (short)Math.Ceiling(paddocks.Count / (double)PageSize);

            short pageIndex = totalPages <= 0
                ? (short)requestedPage
                : (short)Math.Max(1, Math.Min(requestedPage, totalPages));

            var pageEntries = totalPages <= 0
                ? new PaddockInformationsForSell[0]
                : paddocks.Skip((pageIndex - 1) * PageSize).Take(PageSize).ToArray();

            client.Send(new PaddockToSellListMessage(pageIndex, totalPages, pageEntries));
        }

        private static bool MatchesFilter(Paddock paddock, PaddockSearchFilter filter)
        {
            if (paddock == null)
                return false;

            if (filter == null)
                return true;

            if (paddock.SalePrice > filter.MaxPrice)
                return false;

            if (paddock.MaxOutdoorMount < (uint)Math.Max(0, (int)filter.AtLeastNbMount))
                return false;

            if (paddock.MaxItems < (uint)Math.Max(0, (int)filter.AtLeastNbMachine))
                return false;

            if (filter.AreaId > 0 && paddock.Map != null && paddock.Map.SubAreaId != filter.AreaId)
                return false;

            return true;
        }

        private static PaddockSearchFilter GetFilter(WorldClient client)
        {
            if (client?.Character == null)
                return new PaddockSearchFilter();

            PaddockSearchFilter filter;
            return FiltersByCharacterId.TryGetValue(client.Character.Id, out filter)
                ? filter
                : new PaddockSearchFilter();
        }

        private static Paddock GetCurrentPaddock(WorldClient client)
        {
            var dialog = client?.Character?.Dialog as PaddockBuySellDialog;
            if (dialog?.Paddock != null)
                return dialog.Paddock;

            return client?.Character?.Map == null
                ? null
                : PaddockManager.Instance.GetPaddockByMap(client.Character.Map.Id);
        }

        private static PaddockBuySellDialog GetActivePaddockBuySellDialog(WorldClient client)
        {
            return client?.Character?.Dialog as PaddockBuySellDialog;
        }

        private static void ClosePaddockDialog(WorldClient client)
        {
            var dialog = GetActivePaddockBuySellDialog(client);
            if (dialog != null)
                dialog.Close();
            else if (client?.Character != null)
                DialogHandler.SendLeaveDialogMessage(client);
        }

        private static void RefreshPaddockState(Paddock paddock, int? previousGuildId)
        {
            if (paddock?.Map != null)
            {
                foreach (var mapClient in paddock.Map.Clients.ToArray())
                {
                    ContextRoleplayHandler.SendMapComplementaryInformationsDataMessage(mapClient);
                    SendPaddockPropertiesMessage(mapClient, paddock);
                }
            }

            if (previousGuildId.HasValue && previousGuildId.Value > 0)
                RefreshGuildPaddocks(previousGuildId.Value);

            if (paddock.GuildId.HasValue && paddock.GuildId.Value > 0)
                RefreshGuildPaddocks(paddock.GuildId.Value);
        }

        private static void RefreshGuildPaddocks(int guildId)
        {
            var guild = GuildManager.Instance.GetGuild(guildId);
            if (guild == null)
                return;

            foreach (var member in guild.Members.Where(x => x != null && x.IsConnected()))
                GuildHandler.SendGuildInformationsPaddocksMessage(member.Client, guild);
        }

        private static void NotifyGuildPaddockBought(Paddock paddock, int guildId)
        {
            var guild = GuildManager.Instance.GetGuild(guildId);
            if (guild == null || paddock?.Map == null)
                return;

            short worldX = paddock.Map.Point != null ? (short)paddock.Map.Point.X : (short)paddock.Map.Position.X;
            short worldY = paddock.Map.Point != null ? (short)paddock.Map.Point.Y : (short)paddock.Map.Position.Y;

            foreach (var member in guild.Members.Where(x => x != null && x.IsConnected()))
                member.Client.Send(new GuildPaddockBoughtMessage(worldX, worldY, (sbyte)Math.Max(0, Math.Min(127, (int)paddock.MaxOutdoorMount)), (sbyte)Math.Max(0, Math.Min(127, (int)paddock.MaxItems))));
        }

        private static void NotifyGuildPaddockRemoved(Paddock paddock, int guildId)
        {
            var guild = GuildManager.Instance.GetGuild(guildId);
            if (guild == null || paddock?.Map == null)
                return;

            short worldX = paddock.Map.Point != null ? (short)paddock.Map.Point.X : (short)paddock.Map.Position.X;
            short worldY = paddock.Map.Point != null ? (short)paddock.Map.Point.Y : (short)paddock.Map.Position.Y;

            foreach (var member in guild.Members.Where(x => x != null && x.IsConnected()))
                member.Client.Send(new GuildPaddockRemovedMessage(worldX, worldY));
        }
    }
}
