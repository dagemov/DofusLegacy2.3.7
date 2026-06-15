using Sunshine.Logs;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors.Characters.Jobs;
using System.Collections.Generic;

namespace Sunshine.WorldServer.Game.Actors.Npcs.Replies
{
    public static class NpcReplyActionDiagnostics
    {
        private static readonly Dictionary<int, string> HandlerNames = new Dictionary<int, string>
        {
            { 0, "EndDialogReply" },
            { 2, "TeleportReply" },
            { 3, "HasItemReply" },
            { 4, "CinematicReply" },
            { 5, "QuestReply" },
            { 6, "UpdateObjectiveReply" },
            { 7, "AddItemReply" },
            { 8, "LearnJobReply" },
            { 9, "BuyItemReply" },
            { 10, "BankReply" },
            { 11, "DopeulReply" },
        };

        private static readonly Dictionary<sbyte, string> JobNames = new Dictionary<sbyte, string>
        {
            { 2, "Bûcheron" }, { 28, "Paysan" }, { 36, "Pêcheur" }, { 41, "Chasseur" },
            { 24, "Mineur" }, { 25, "Boulanger" }, { 26, "Alchimiste" },
        };

        public static string GetHandlerName(int actionType)
        {
            if (HandlerNames.TryGetValue(actionType, out var name))
                return name;
            if (actionType == 1)
                return "Navigate";
            if (actionType < 0)
                return "QuestBranchMarker";
            return "Unhandled";
        }

        public static string GetJobName(sbyte jobId)
        {
            return JobNames.TryGetValue(jobId, out var name) ? name : $"Job#{jobId}";
        }

        public static void LogReplySelection(WorldClient client, Npc npc, short dialogId, short replyId, int actionType, string actionArgs, string result)
        {
            if (client?.Character == null || npc == null)
                return;

            var handler = GetHandlerName(actionType);
            Logger.WriteInfo(
                $"[NpcReply] charId={client.Character.Id} npcId={npc.Record.Id} npcName={npc.Record.Name} mapId={client.Character.Map.Id} dialogId={dialogId} replyId={replyId} replyTextId= actionType={actionType} actionArgs={actionArgs ?? string.Empty} handler={handler} result={result}");
        }

        public static void LogUnhandled(WorldClient client, Npc npc, short replyId, int actionType, string actionArgs)
        {
            if (client?.Character == null || npc == null)
                return;

            Logger.WriteWarning(
                $"[NpcAction] npcId={npc.Record.Id} npcName={npc.Record.Name} replyId={replyId} actionType={actionType} args={actionArgs ?? string.Empty} result=Unhandled");
        }

        public static void LogQuestBranchMarker(WorldClient client, Npc npc, short replyId, int actionType, string actionArgs)
        {
            if (client?.Character == null || npc == null)
                return;

            Logger.WriteInfo(
                $"[NpcAction] type=QuestBranch npcId={npc.Record.Id} replyId={replyId} actionType={actionType} args={actionArgs ?? string.Empty} result=SkippedDispatch");
        }

        public static void LogLearnJob(Npc npc, short replyId, sbyte jobId, string result)
        {
            Logger.WriteInfo(
                $"[NpcAction] type=LearnJob npcId={npc?.Record?.Id} replyId={replyId} jobId={jobId} jobName={GetJobName(jobId)} result={result}");
        }

        public static void LogQuest(Npc npc, short replyId, short questId, string result)
        {
            Logger.WriteInfo(
                $"[NpcAction] type=Quest npcId={npc?.Record?.Id} replyId={replyId} questId={questId} result={result}");
        }

        public static void LogTeleport(Npc npc, short replyId, string args, string result)
        {
            Logger.WriteInfo(
                $"[NpcAction] type=Teleport npcId={npc?.Record?.Id} replyId={replyId} args={args ?? string.Empty} result={result}");
        }
    }
}
