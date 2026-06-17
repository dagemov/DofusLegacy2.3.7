using Sunshine.MySql.Database.World.Npcs;
using System;
using System.Collections.Generic;
using System.Linq;
using Sunshine.Protocol.Types;
using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Actors.Npcs.Actions;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors.Look;
using Sunshine.WorldServer.Game.Maps.Pathfinding;

namespace Sunshine.WorldServer.Game.Actors.Npcs
{
    public class NpcReplyResolution
    {
        public int Type { get; set; }
        public string Args { get; set; } = string.Empty;
        public string Source { get; set; } = "None";
        public short ResolvedMessageId { get; set; }
        public bool Ambiguous { get; set; }
    }

    public class Npc : RolePlayActor
    {
        private class DialogCsvEntry
        {
            public short MessageId { get; set; }
            public short ParameterId { get; set; }
            public string Parameter { get; set; }
        }

        public NpcTemplate Record { get; set; }
        public NpcSpawn Spawn { get; set; }
        public NpcMessage Message { get; set; }
        public IEnumerable<NpcReplie> Replies { get; set; }
        public Dictionary<int, NpcShop> Shops { get; set; }

        public Npc(NpcTemplate record, NpcSpawn spawn)
        {
            Id = ActorManager.Instance.GenerateId();
            Record = record;
            Spawn = spawn;
            Message = NpcManager.Instance.GetNpcMessage(Record.Id);
            Replies = NpcManager.Instance.GetNpcReplies(Record.Id, Spawn.Map);
            Shops = NpcManager.Instance.GetNpcShops(Record.Id);
            Look = EntityManager.Instance.GetActorLook(Record.EntityLook);
            GetNpcTypes = new List<int>();
            GetAllDialogs = new Dictionary<short, List<short>>();
            GetDialogMessagesId = new List<short>();
            GetDialogRepliesId = new List<short>();
            GetDialogParams = new List<string>();
            GetParameters = new List<string>();
            InitializeNpc();
            InjectFallbackBankerDialogs();
        }

        public override int Id { get; }
        public ActorLook Look { get; set; }
        public NpcAction Action { get; set; }
        public IEnumerable<ObjectItemToSellInNpcShop> GetObjectItemToSellInNpcShops { get; set; }
        public Dictionary<short, List<short>> GetAllDialogs { get; set; }
        public List<int> GetNpcTypes { get; set; }
        public List<short> GetDialogMessagesId { get; set; }
        public List<short> GetDialogRepliesId { get; set; }
        public List<string> GetParameters { get; set; }
        public List<string> GetDialogParams { get; set; }

        public EntityDispositionInformations GetEntityDisposition()
        {
            return new EntityDispositionInformations(Spawn.Cell, (sbyte)Spawn.Direction);
        }

        public override GameRolePlayActorInformations GetGameRolePlayActorInformations()
        {
            return new GameRolePlayNpcInformations(Id, Look.GetEntityLook(), GetEntityDisposition(), (short)Record.Id, Record.Gender, 0, Record.HasQuest);
        }

        public GameRolePlayActorInformations GetGameRolePlayNpcInformations(WorldClient client)
        {
            if (!Record.HasQuest)
                return GetGameRolePlayActorInformations();

            var replies = Replies.Where(x => x.Type == 5);
            foreach (var replie in replies)
            {
                short questId = short.Parse(replie.ParametersCSV.Substring(0, 3));
                var quest = client.Character.Quests.GetQuest(questId);
                if (quest == null)
                    return GetGameRolePlayActorInformations();

                return new GameRolePlayNpcInformations(Id, Look.GetEntityLook(), GetEntityDisposition(), (short)Record.Id, Record.Gender, 0, false);
            }

            return GetGameRolePlayActorInformations();
        }

        public void InteractWith(NpcActionTypeEnum action, Character source)
        {
            switch (action)
            {
                case NpcActionTypeEnum.ACTION_TALK:
                    Action = new NpcTalkAction(this, source);
                    Action.Execute();
                    break;
                case NpcActionTypeEnum.ACTION_BUY_SELL:
                    Action = new NpcBuySellAction(this, source);
                    Action.Execute();
                    break;
                case NpcActionTypeEnum.ACTION_BUY:
                    Action = new NpcBuyAction(this, source);
                    Action.Execute();
                    break;
                case NpcActionTypeEnum.ACTION_SELL:
                    Action = new NpcSellAction(this, source);
                    Action.Execute();
                    break;
                case NpcActionTypeEnum.ACTION_EXCHANGE:
                    Action = new NpcTradeAction(this, source);
                    Action.Execute();
                    break;
            }
        }

        public override void StartMove(Path path)
        {
        }

        public int ResolveShopToken()
        {
            var token = Shops != null ? Shops.Values.Select(x => x != null ? x.Token : 0).FirstOrDefault(x => x > 0) : 0;
            return token > 0 ? token : Record.Token;
        }

        private void InjectFallbackBankerDialogs()
        {
            if (Record.Id != 100 || GetAllDialogs.Count > 0)
                return;

            Record.DialogMessagesIdCSV = "318,15194;410,16065;2911,16004;6205,17425;8767,8669";
            Record.DialogRepliesIdCSV = ";259,23501;329,18273;2556,25155;5422,18766;8795,19131";
            BuildDialogsFromTemplateCsv();
        }

        private void InitializeNpc()
        {
            BuildNpcShopObjects();
            BuildDialogsFromReplies();
            BuildDialogsFromTemplateCsv();
        }

        private void BuildNpcShopObjects()
        {
            var objectItems = new List<ObjectItemToSellInNpcShop>();

            foreach (var shop in Shops.Values.Where(x => ItemManager.Instance.Items.ContainsKey(x.Item))
                                             .OrderBy(x => x.Price > 0 ? x.Price : ItemManager.Instance.Items[x.Item].Price)
                                             .ThenBy(x => ItemManager.Instance.Items[x.Item].Name)
                                             .ThenBy(x => x.Item))
            {

                var item = ItemManager.Instance.Items[shop.Item];
                var npcPrice = shop.GetPrice((int)item.Price);
                objectItems.Add(new ObjectItemToSellInNpcShop((short)shop.Item, 0, false, item.EffectsBase.Select(x => x.GetObjectEffectMinMax()), npcPrice, item.Criteria ?? string.Empty));
            }

            GetObjectItemToSellInNpcShops = objectItems;
        }

        private void BuildDialogsFromReplies()
        {
            foreach (var replie in Replies)
            {
                RegisterDialogEntry(replie.Type, replie.MessageId, replie.ReplieId, replie.ParametersCSV, replie.DialogParamsCSV);
            }
        }

        private void BuildDialogsFromTemplateCsv()
        {
            var messageEntries = ParseDialogCsv(Record.DialogMessagesIdCSV);
            var replyEntries = ParseDialogCsv(Record.DialogRepliesIdCSV, allowEmptyMessageKey: true);

            if (messageEntries.Count == 0)
                return;

            short currentMessage = messageEntries[0].MessageId;
            EnsureDialogMessage(currentMessage);

            foreach (var entry in messageEntries)
            {
                currentMessage = entry.MessageId;
                EnsureDialogMessage(currentMessage);
                RegisterDialogParams(currentMessage, entry.Parameter);
            }

            foreach (var entry in replyEntries)
            {
                if (entry.MessageId > 0)
                    currentMessage = entry.MessageId;

                if (currentMessage <= 0)
                    continue;

                if (ShouldSkipCsvReply(currentMessage, entry.ParameterId))
                    continue;

                EnsureDialogMessage(currentMessage);
                RegisterDialogEntry(1, currentMessage, entry.ParameterId, string.Empty, string.Empty);
            }
        }

        private bool ShouldSkipCsvReply(short messageId, short replyId)
        {
            if (Replies == null)
                return false;

            if (Replies.Any(x => x.ReplieId == replyId && x.Type != 1))
                return true;

            return Replies.Any(x => x.MessageId == messageId && x.ReplieId == replyId);
        }

        private List<DialogCsvEntry> ParseDialogCsv(string csv, bool allowEmptyMessageKey = false)
        {
            var result = new List<DialogCsvEntry>();
            if (string.IsNullOrWhiteSpace(csv))
                return result;

            foreach (var rawEntry in csv.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = rawEntry.Split(',');
                if (parts.Length < 2)
                    continue;

                short messageId = 0;
                if (!string.IsNullOrWhiteSpace(parts[0]))
                {
                    short.TryParse(parts[0], out messageId);
                }
                else if (!allowEmptyMessageKey)
                {
                    continue;
                }

                short parameterId;
                if (!short.TryParse(parts[1], out parameterId))
                    continue;

                string parameter = parts.Length > 2 ? string.Join(",", parts.Skip(2)) : string.Empty;
                result.Add(new DialogCsvEntry { MessageId = messageId, ParameterId = parameterId, Parameter = parameter });
            }

            return result;
        }

        private void RegisterDialogEntry(int type, short messageId, short replyId, string parameters, string dialogParams)
        {
            if (messageId <= 0)
                return;

            EnsureDialogMessage(messageId);

            GetNpcTypes.Add(type);
            GetDialogMessagesId.Add(messageId);
            GetDialogRepliesId.Add(replyId);
            GetParameters.Add(parameters ?? string.Empty);
            GetDialogParams.Add(dialogParams ?? string.Empty);

            if (replyId > 0 && !GetAllDialogs[messageId].Contains(replyId))
                GetAllDialogs[messageId].Add(replyId);
        }

        private void RegisterDialogParams(short messageId, string dialogParam)
        {
            int index = GetDialogMessagesId.FindIndex(x => x == messageId);
            if (index >= 0 && string.IsNullOrWhiteSpace(GetDialogParams[index]))
                GetDialogParams[index] = dialogParam ?? string.Empty;
        }

        public short GetFirstDialogMessageId()
        {
            return GetAllDialogs != null && GetAllDialogs.Count > 0
                ? GetAllDialogs.First().Key
                : (short)0;
        }

        public List<short> GetDialogMessageOrder()
        {
            return GetAllDialogs != null
                ? GetAllDialogs.Keys.ToList()
                : new List<short>();
        }

        public List<short> GetDialogReplies(short messageId)
        {
            if (GetAllDialogs == null || !GetAllDialogs.ContainsKey(messageId))
                return new List<short>();

            return GetAllDialogs[messageId] ?? new List<short>();
        }

        public short GetNextDialogMessageId(short currentMessageId)
        {
            var orderedMessages = GetDialogMessageOrder();
            int index = orderedMessages.IndexOf(currentMessageId);
            if (index < 0 || index + 1 >= orderedMessages.Count)
                return 0;

            return orderedMessages[index + 1];
        }

        public IEnumerable<string> GetDialogParameters(Character character, short messageId)
        {
            var parameters = new List<string>();
            if (character == null)
                return parameters;

            int index = GetDialogMessagesId.FindIndex(x => x == messageId);
            if (index < 0 || index >= GetDialogParams.Count)
                return parameters;

            var csv = GetDialogParams[index];
            if (string.IsNullOrWhiteSpace(csv))
                return parameters;

            foreach (var token in csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()))
            {
                switch (token)
                {
                    case "N":
                        parameters.Add(character.Name);
                        break;
                    case "L":
                        parameters.Add(character.Level.ToString());
                        break;
                    default:
                        parameters.Add(token);
                        break;
                }
            }

            return parameters;
        }

        public bool TryGetReplyIndex(short messageId, short replyId, out int index)
        {
            NpcReplyResolution resolution;
            if (!TryResolveReply(messageId, replyId, out resolution))
            {
                index = -1;
                return false;
            }

            for (int i = 0; i < GetDialogRepliesId.Count && i < GetDialogMessagesId.Count; i++)
            {
                if (GetDialogMessagesId[i] == resolution.ResolvedMessageId &&
                    GetDialogRepliesId[i] == replyId &&
                    GetNpcTypes[i] == resolution.Type)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return true;
        }

        public bool TryResolveReply(short messageId, short replyId, out NpcReplyResolution resolution)
        {
            resolution = new NpcReplyResolution();

            var dbExactMatches = Replies?
                .Where(x => x.MessageId == messageId && x.ReplieId == replyId)
                .OrderByDescending(x => x.Type != 1 ? 1 : 0)
                .ToList() ?? new List<NpcReplie>();

            if (dbExactMatches.Count > 0)
            {
                var dbExact = dbExactMatches[0];
                resolution.Type = dbExact.Type;
                resolution.Args = dbExact.ParametersCSV ?? string.Empty;
                resolution.Source = "DB";
                resolution.ResolvedMessageId = messageId;
                resolution.Ambiguous = dbExactMatches.Count > 1;
                return true;
            }

            var dbTypedMatches = Replies?
                .Where(x => x.ReplieId == replyId && x.Type != 1)
                .OrderByDescending(x => x.MessageId == messageId ? 1 : 0)
                .ToList() ?? new List<NpcReplie>();

            if (dbTypedMatches.Count > 0)
            {
                var dbTyped = dbTypedMatches[0];
                resolution.Type = dbTyped.Type;
                resolution.Args = dbTyped.ParametersCSV ?? string.Empty;
                resolution.Source = "DB";
                resolution.ResolvedMessageId = dbTyped.MessageId;
                resolution.Ambiguous = dbTypedMatches.Count > 1;
                return true;
            }

            var memoryExact = new List<int>();
            for (int i = 0; i < GetDialogRepliesId.Count && i < GetDialogMessagesId.Count; i++)
            {
                if (GetDialogMessagesId[i] == messageId && GetDialogRepliesId[i] == replyId)
                    memoryExact.Add(i);
            }

            if (memoryExact.Count > 0)
            {
                int best = memoryExact.OrderByDescending(i => GetNpcTypes[i] != 1 ? 1 : 0).First();
                resolution.Type = GetNpcTypes[best];
                resolution.Args = GetParameters.Count > best ? GetParameters[best] ?? string.Empty : string.Empty;
                resolution.Source = "CSV";
                resolution.ResolvedMessageId = messageId;
                resolution.Ambiguous = memoryExact.Count > 1;
                return true;
            }

            var memoryFallback = new List<int>();
            for (int i = 0; i < GetDialogRepliesId.Count; i++)
            {
                if (GetDialogRepliesId[i] == replyId)
                    memoryFallback.Add(i);
            }

            if (memoryFallback.Count > 0)
            {
                int best = memoryFallback
                    .OrderByDescending(i => GetNpcTypes[i] != 1 ? 1 : 0)
                    .ThenByDescending(i => GetDialogMessagesId[i] == messageId ? 1 : 0)
                    .First();
                resolution.Type = GetNpcTypes[best];
                resolution.Args = GetParameters.Count > best ? GetParameters[best] ?? string.Empty : string.Empty;
                resolution.Source = "Fallback";
                resolution.ResolvedMessageId = GetDialogMessagesId[best];
                resolution.Ambiguous = memoryFallback.Count > 1;
                return true;
            }

            return false;
        }

        public string FormatDbRepliesForLog()
        {
            if (Replies == null)
                return "[]";

            return "[" + string.Join(",", Replies.Select(x => $"{x.ReplieId}:m{x.MessageId}:t{x.Type}:a{x.ParametersCSV}")) + "]";
        }

        private void EnsureDialogMessage(short messageId)
        {
            if (!GetAllDialogs.ContainsKey(messageId))
                GetAllDialogs.Add(messageId, new List<short>());
        }
    }
}
