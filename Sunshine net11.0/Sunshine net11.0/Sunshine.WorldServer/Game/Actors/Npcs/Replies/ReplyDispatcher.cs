using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Handlers.Dialogs;
using Sunshine.Logs;
using System.Collections.Generic;

namespace Sunshine.WorldServer.Game.Actors.Npcs.Replies
{
    public static class ReplyDispatcher
    {
        public static bool Dispatch(WorldClient client, Npc npc, short dialogId, short replyId, List<object> parameters)
        {
            if (client == null || npc == null)
            {
                Logger.WriteError($"Cannot reply to npc: client or npc is null");
                return false;
            }

            int typeId = (int)parameters[0];
            string actionArgs = parameters.Count > 1 ? parameters[1] as string ?? string.Empty : string.Empty;

            if (typeId < 0)
            {
                NpcReplyActionDiagnostics.LogQuestBranchMarker(client, npc, replyId, typeId, actionArgs);
                NpcReplyActionDiagnostics.LogReplySelection(client, npc, dialogId, replyId, typeId, actionArgs, "SkippedDispatch");
                return true;
            }

            if (typeId == 1)
            {
                NpcReplyActionDiagnostics.LogReplySelection(client, npc, dialogId, replyId, typeId, actionArgs, "Navigate");
                return true;
            }

            if (!NpcManager.Instance.Replies.ContainsKey(typeId))
            {
                NpcReplyActionDiagnostics.LogUnhandled(client, npc, replyId, typeId, actionArgs);
                NpcReplyActionDiagnostics.LogReplySelection(client, npc, dialogId, replyId, typeId, actionArgs, "Unhandled");
                DialogHandler.SendLeaveDialogMessage(client);
                return false;
            }

            parameters.RemoveAt(0);
            var handler = NpcManager.Instance.Replies[typeId]();
            bool success = handler.Initialize(client, npc, parameters);
            string result = success ? "Success" : "Failed";
            NpcReplyActionDiagnostics.LogReplySelection(client, npc, dialogId, replyId, typeId, actionArgs, result);
            return success;
        }
    }
}
