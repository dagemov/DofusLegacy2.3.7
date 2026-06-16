using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Characters.Jobs;
using Sunshine.WorldServer.Game.Actors.Monsters;
using Sunshine.WorldServer.Game.Actors.Npcs;
using Sunshine.WorldServer.Game.Actors.Npcs.Actions;
using Sunshine.WorldServer.Game.Actors.Npcs.Replies;
using Sunshine.WorldServer.Handlers.Characters.Jobs;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Sunshine.WorldServer.Commands.Moderator
{
    [CommandHandler("mapinfo", RoleEnum.Moderator)]
    public class MapInfoCommand : WorldCommand
    {
        public override string Description => "QA: map id, subarea, npcs, monsters, interactives on current map.";

        public override void Execute()
        {
            var character = Client.Character;
            var map = character.Map;
            if (map == null)
                return;

            var npcs = map.RolePlayActors?.OfType<Npc>().ToList() ?? new System.Collections.Generic.List<Npc>();
            var groups = map.RolePlayActors?.OfType<MonsterGroup>().ToList() ?? new System.Collections.Generic.List<MonsterGroup>();

            var npcPart = string.Join(",", npcs.Select(n =>
                $"{n.Record.Id}:{Sanitize(n.Record.Name)}:{n.Spawn?.Cell ?? 0}"));
            var monsterPart = string.Join(",", groups.SelectMany(g =>
                (g.Monsters ?? new System.Collections.Generic.List<Monster>()).Select(m =>
                    $"{g.Id}:{m.Record.Id}:{Sanitize(m.Record.Name)}:{g.Cell}")));
            var interactivePart = string.Join(",", map.Interactives?.Select(i =>
            {
                short cell = map.Elements?.FirstOrDefault(e => e.Id == i.Element)?.Cell ?? (short)0;
                int skillId = i.Skills?.FirstOrDefault() ?? 0;
                return $"{i.Element}:{skillId}:{cell}";
            }) ?? System.Linq.Enumerable.Empty<string>());

            character.SendServerMessage($"MapId={map.Id}", Color.Aqua);
            character.SendServerMessage($"SubAreaId={map.SubAreaId}", Color.Aqua);
            character.SendServerMessage($"CellId={character.Cell.Id}", Color.Aqua);
            character.SendServerMessage($"Npcs=[{npcPart}]", Color.White);
            character.SendServerMessage($"Monsters=[{monsterPart}]", Color.White);
            character.SendServerMessage($"Interactives=[{interactivePart}]", Color.White);
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "?";
            return value.Replace(":", "_").Replace(",", "_");
        }
    }

    [CommandHandler("npcs", RoleEnum.Moderator)]
    public class NpcsListCommand : WorldCommand
    {
        public override string Description => "QA: list NPCs on current map with dialog/replies info.";

        public override void Execute()
        {
            var map = Client.Character.Map;
            var npcs = map?.RolePlayActors?.OfType<Npc>().ToList();
            if (npcs == null || npcs.Count == 0)
            {
                Client.Character.SendServerMessage("No NPCs on this map.", Color.Orange);
                return;
            }

            Client.Character.SendServerMessage($"=== NPCs map {map.Id} ===", Color.Aqua);
            foreach (var npc in npcs.OrderBy(x => x.Record.Id))
            {
                short dialogId = npc.GetFirstDialogMessageId();
                int dbReplies = npc.Replies?.Count() ?? 0;
                int visibleReplies = npc.GetDialogRepliesId?.Count ?? 0;
                Client.Character.SendServerMessage(
                    $"npcId={npc.Record.Id}, actorId={npc.Id}, name={npc.Record.Name}, cellId={npc.Spawn?.Cell ?? 0}, dialogId={dialogId}, dbReplies={dbReplies}, repliesCount={visibleReplies}",
                    Color.White);
            }
        }
    }

    [CommandHandler("jobs", RoleEnum.Moderator)]
    public class JobsQaCommand : WorldCommand
    {
        private static readonly System.Collections.Generic.Dictionary<sbyte, string> JobNames =
            new System.Collections.Generic.Dictionary<sbyte, string>
            {
                { 2, "Bûcheron" }, { 28, "Paysan" }, { 36, "Pêcheur" }, { 41, "Chasseur" },
                { 24, "Mineur" }, { 25, "Boulanger" }, { 26, "Alchimiste" }, { 27, "Sastre" },
            };

        public override string Description => "QA: list current character jobs with level and xp.";

        public override void Execute()
        {
            var jobs = Client.Character.Jobs.GetJobs().ToList();
            if (jobs.Count == 0)
            {
                Client.Character.SendServerMessage("No jobs.", Color.Orange);
                return;
            }

            Client.Character.SendServerMessage("=== Jobs ===", Color.Aqua);
            foreach (var job in jobs.OrderBy(x => x.Job))
            {
                int level = ExperienceManager.Instance.GetJobLevelExperienceFloor(job.Experience);
                string name = JobNames.TryGetValue(job.Job, out var n) ? n : $"Job#{job.Job}";
                Client.Character.SendServerMessage($"{job.Job},{name},level={level},xp={job.Experience}", Color.LawnGreen);
            }
        }
    }

    [CommandHandler("jobclear", RoleEnum.Moderator)]
    public class JobClearCommand : WorldCommand
    {
        public override string Description => "QA: remove jobs from test character (.jobclear all | .jobclear 28).";

        public override void Execute()
        {
            if (Parameters.Length == 0 || Parameters[0] == null)
            {
                Client.Character.SendServerMessage("Uso: .jobclear all | .jobclear <jobId>", Color.Orange);
                return;
            }

            string arg = Parameters[0].ToString().ToLower();
            var jobs = Client.Character.Jobs;

            if (arg == "all")
            {
                var existing = jobs.GetJobs().Select(x => x.Job).ToList();
                int removed = jobs.ClearAllJobs();
                CharacterManager.Instance.Save(Client.Character);
                JobHandler.NotifyJobsCleared(Client);
                Client.Character.SendServerMessage($"Removed {removed} job(s).", Color.Green);
                return;
            }

            if (!sbyte.TryParse(arg, out sbyte jobId))
            {
                Client.Character.SendServerMessage("Invalid jobId.", Color.Red);
                return;
            }

            if (!jobs.RemoveJob(jobId))
            {
                Client.Character.SendServerMessage($"Job {jobId} not found.", Color.Orange);
                return;
            }

            CharacterManager.Instance.Save(Client.Character);
            JobHandler.NotifyJobRemoved(Client, jobId);
            Client.Character.SendServerMessage($"Removed job {jobId}.", Color.Green);
        }

        private void RefreshJobData()
        {
            JobHandler.SendVisibleJobDataMessage(Client);
        }
    }

    [CommandHandler("npcdebug", RoleEnum.Moderator)]
    public class NpcDebugCommand : WorldCommand
    {
        public override string Description => "QA: dialog state or toggle verbose NPC logs (.npcdebug on|off).";

        public override void Execute()
        {
            if (Parameters.Length > 0 && Parameters[0] != null)
            {
                string arg = Parameters[0].ToString().ToLower();
                if (arg == "on" || arg == "off")
                {
                    bool enabled = arg == "on";
                    NpcReplyActionDiagnostics.SetVerboseFor(Client.Character.Id, enabled);
                    Client.Character.SendServerMessage($"Npc verbose logs {(enabled ? "ON" : "OFF")} for this character.", Color.Yellow);
                    return;
                }
            }

            var dialog = Client.Character.Dialog as NpcTalkAction;
            if (dialog == null)
            {
                Client.Character.SendServerMessage("Not in NPC dialog.", Color.Orange);
                Client.Character.SendServerMessage($"verboseLogs={NpcReplyActionDiagnostics.IsVerboseFor(Client.Character.Id)}", Color.Gray);
                return;
            }

            var npc = dialog.Npc;
            short messageId = dialog.CurrentMessageId;
            var replies = npc.GetDialogReplies(messageId);
            var sb = new StringBuilder();
            sb.Append("availableReplies=[");
            sb.Append(string.Join(",", replies));
            sb.Append("]");

            Client.Character.SendServerMessage($"currentNpcId={npc.Record.Id}", Color.Aqua);
            Client.Character.SendServerMessage($"currentDialogId={messageId}", Color.Aqua);
            Client.Character.SendServerMessage($"currentMessageId={messageId}", Color.Aqua);
            Client.Character.SendServerMessage(sb.ToString(), Color.White);
            Client.Character.SendServerMessage($"dbReplies={npc.FormatDbRepliesForLog()}", Color.Gray);
            Client.Character.SendServerMessage($"verboseLogs={NpcReplyActionDiagnostics.IsVerboseFor(Client.Character.Id)}", Color.Gray);
        }
    }

    [CommandHandler("goto", RoleEnum.Moderator)]
    public class GotoCommand : WorldCommand
    {
        public override string Description => "QA: teleport to mapId [cellId] (.goto 21759491 300).";

        public override void Execute()
        {
            if (Parameters.Length < 1 || !int.TryParse(Parameters[0].ToString(), out int mapId))
            {
                Client.Character.SendServerMessage("Uso: .goto <mapId> [cellId]", Color.Orange);
                return;
            }

            var map = MapManager.Instance.GetMap(mapId);
            if (map == null)
            {
                Client.Character.SendServerMessage($"Map {mapId} not found.", Color.Red);
                return;
            }

            short cellId;
            if (Parameters.Length >= 2 && short.TryParse(Parameters[1].ToString(), out short requestedCell) &&
                requestedCell >= 0 && requestedCell < map.Cells.Length && map.Cells[requestedCell].Walkable)
            {
                cellId = requestedCell;
            }
            else
            {
                var walkable = map.Cells.FirstOrDefault(x => x.Walkable && x.Id > 100);
                cellId = walkable.Id > 0 ? walkable.Id : (short)300;
            }

            Client.Character.Teleport(mapId, cellId);
            Client.Character.SendServerMessage($"Teleported to map {mapId} cell {cellId}.", Color.Green);
        }
    }
}
