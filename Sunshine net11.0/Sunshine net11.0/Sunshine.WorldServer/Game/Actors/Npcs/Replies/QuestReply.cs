using Sunshine.Protocol.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Npcs.Replies
{
    [ReplyHandler(5)]
    public class QuestReply : Reply
    {
        public QuestReply()
        {
        }

        public override bool Execute()
        {
            var quests = Parameters[0] as string;
            short quest = short.Parse(quests.Split(',')[0]);
            if (!Client.Character.Quests.HasQuest(quest))
            {
                Client.Character.Quests.AddQuest(Npc.Id, quest);
                NpcReplyActionDiagnostics.LogQuest(Npc, 0, quest, "started");
                return true;
            }

            Client.Character.SendServerMessage("Tu as déjà eu cette quête.");
            NpcReplyActionDiagnostics.LogQuest(Npc, 0, quest, "already_active");
            return false;
        }
    }
}
