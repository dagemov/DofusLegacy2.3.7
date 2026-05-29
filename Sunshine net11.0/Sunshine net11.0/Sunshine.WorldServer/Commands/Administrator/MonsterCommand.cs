using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Monsters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Commands.Moderator
{
    [CommandHandler("monster", RoleEnum.Moderator)]
    public class MonsterCommand : WorldCommand
    {
        public override void Execute()
        {
            if (Parameters.Length < 2)
                Client.Character.SendServerMessage("Unknown monster !", Color.Red);
            else
            {
                var monster = int.TryParse((string)Parameters[1], out int monsterId) ? int.Parse((string)Parameters[1]) : -1;
                var template = MonsterManager.Instance.GetMonster(monster);
                if (monster == -1 || template == null)
                {
                    Client.Character.SendServerMessage($"Monster {monster} doesn't exist !");
                    return;
                }
                var grades = MonsterManager.Instance.GetMonsterGrades(monster);
                int count = int.TryParse((string)Parameters[2], out int countId) ? int.Parse((string)Parameters[2]) : 8;
                int group = -1;
                if (Parameters.Length > 3)
                    group = int.TryParse((string)Parameters[3], out int groupId) ? int.Parse((string)Parameters[3]) : -1;
                switch (Parameters[0].ToString().ToLower())
                {
                    case "spawn":
                        if (group != -1)
                        {
                            var actor = Client.Character.Map.RolePlayActors.FirstOrDefault(x => x.Id == group);
                            if (actor == null || (actor != null && !(actor is MonsterGroup)))
                            {
                                Client.Character.SendServerMessage($"Group {group} doesn't exist !");
                                return;
                            }
                            MonsterGroup lastGroup = actor as MonsterGroup;
                            Client.Character.Map.RolePlayActors.Remove(lastGroup);
                            lastGroup.Monsters.Add(new Monster(template, grades));
                            Client.Character.Map.Refresh(lastGroup);
                            Client.Character.Map.RolePlayActors.Add(lastGroup);
                            Client.Character.SendServerMessage($"Monster {monster}, group = {lastGroup.Id} spawned.");
                        }
                        else
                        {
                            MonsterGroup newGroup = new MonsterGroup(new List<Monster> { new Monster(template, grades) }, Client.Character.Map, false, count);
                            Client.Character.Map.EnterActor(newGroup);
                            Client.Character.SendServerMessage($"Monster {monster}, group = {newGroup.Id} spawned.");
                        }
                        break;

                    case "unspawn":
                        if (group != -1)
                        {
                            var actor = Client.Character.Map.RolePlayActors.FirstOrDefault(x => x.Id == group);
                            if (actor == null || (actor != null && !(actor is MonsterGroup)))
                            {
                                Client.Character.SendServerMessage($"Group {group} doesn't exist !");
                                return;
                            }
                            MonsterGroup lastGroup = actor as MonsterGroup;
                            Client.Character.Map.RolePlayActors.Remove(lastGroup);
                            Client.Character.Map.LeaveActor(lastGroup);
                            Client.Character.SendServerMessage($"Monster {monster}, group = {lastGroup.Id} unspawned.");
                        }
                        else
                        {
                            MonsterGroup newGroup = Client.Character.Map.RolePlayActors.FirstOrDefault(x => x is MonsterGroup) as MonsterGroup;
                            var monsterInGroup = newGroup == null ? null : (newGroup as MonsterGroup).Monsters.FirstOrDefault(x => x.Record.Id == monster);
                            if (monsterInGroup == null || newGroup == null)
                            {
                                Client.Character.SendServerMessage($"Group {group} doesn't exist !");
                                return;
                            }
                            Client.Character.Map.RolePlayActors.Remove(newGroup);
                            newGroup.Monsters.Remove(monsterInGroup);
                            Client.Character.Map.RolePlayActors.Add(newGroup);
                            Client.Character.Map.Refresh(newGroup);
                            Client.Character.SendServerMessage($"Monster {monster}, group = {newGroup.Id} unspawned.");
                        }
                        break;
                }

            }
        }

        public override string Description
        {
            get { return "Allows spawn monsters."; }
        }
    }
}
