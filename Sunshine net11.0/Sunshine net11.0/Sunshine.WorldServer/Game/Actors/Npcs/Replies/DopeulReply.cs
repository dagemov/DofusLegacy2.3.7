using Dapper;
using Sunshine.Logs;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Characters;
using Sunshine.MySql.Database.World.Monsters;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors.Monsters;
using Sunshine.WorldServer.Game.Fights.Types;
using Sunshine.WorldServer.Handlers.Dialogs;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Sunshine.WorldServer.Game.Actors.Npcs.Replies
{
    [ReplyHandler(11)]
    public class DopeulReply : Reply
    {
        private const double CooldownHours = 3.0;

        private static readonly ConcurrentDictionary<string, DateTime> _cooldownCache =
            new ConcurrentDictionary<string, DateTime>();

        public override bool Execute()
        {
            var character = Client.Character;

            int monsterId = -1;

            if (Parameters != null && Parameters.Count > 0 && Parameters[0] != null)
            {
                var raw = Parameters[0];

                if (raw is int i)
                    monsterId = i;
                else if (raw is long l)
                    monsterId = (int)l;
                else
                    int.TryParse(raw.ToString(), out monsterId);
            }

            if (monsterId <= 0)
            {
                Logger.WriteError($"[DopeulReply] MonsterId invalido: {Parameters?[0]}");
                character.SendServerMessage("Error de configuracion del NPC.");
                CloseDialog();
                return false;
            }

            if (!CanFight(character.Id, monsterId, out int minutesLeft))
            {
                int hours = minutesLeft / 60;
                int mins = minutesLeft % 60;

                character.SendServerMessage(
                    hours > 0
                    ? $"Puedes desafiar nuevamente al Dopeul en {hours}h {mins}min."
                    : $"Puedes desafiar nuevamente al Dopeul en {mins} minuto(s).");

                CloseDialog();
                return false;
            }

            if (character.IsInFight() || character.Fighter != null)
            {
                CloseDialog();
                return false;
            }

            CloseDialog();

            return LaunchDopeulFight(monsterId);
        }

        private bool LaunchDopeulFight(int monsterId)
        {
            var character = Client.Character;

            try
            {
                if (!MonsterManager.Instance.Monsters.TryGetValue(monsterId, out var monsterRecord))
                {
                    Logger.WriteError($"[DopeulReply] Monster {monsterId} no encontrado.");
                    return false;
                }

                MonsterGrade[] grades = MonsterManager.Instance.GetMonsterGrades(monsterId);

                if (grades == null || grades.Length <= 0)
                {
                    Logger.WriteError($"[DopeulReply] Monster {monsterId} sin grades.");
                    return false;
                }

                byte gradeId = (byte)Math.Max(1, Math.Ceiling(character.Level / 20d));

                MonsterGrade selectedGrade =
                    grades.FirstOrDefault(x => x.GradeId == gradeId)
                    ?? grades.OrderByDescending(x => x.GradeId).First();

                character.SetFight(FightTypeEnum.FIGHT_TYPE_PvM);

                var fight = character.Fight as FightPvM;

                if (fight == null)
                {
                    Logger.WriteError("[DopeulReply] FightPvM null.");
                    return false;
                }

                fight.IsDopeulFight = true;
                fight.DopeulMonsterId = monsterId;

                var monster = new Monster(monsterRecord, selectedGrade);
                var monsterFighter = new MonsterFighter(monster, fight);
                fight.AddFighter(monsterFighter);

                var characterFighter = new CharacterFighter(character);
                character.Fighter = characterFighter;
                fight.AddFighter(characterFighter, true);

                RegisterFightStart(character.Id, monsterId);

                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteError($"[DopeulReply] LaunchDopeulFight: {ex}");
                character.Fight = null;
                return false;
            }
        }

        private static bool CanFight(int characterId, int monsterId, out int minutesLeft)
        {
            minutesLeft = 0;

            string key = $"{characterId}_{monsterId}";

            DateTime lastFight;

            if (!_cooldownCache.TryGetValue(key, out lastFight))
            {
                try
                {
                    CharacterDopeulBootstrap.EnsureCooldownTable();

                    var record = DatabaseManager.Connection.QueryFirstOrDefault<CharacterDopeulCooldown>(
                        @"SELECT *
                          FROM characters_dopeul_cooldown
                          WHERE CharacterId = @charId
                          AND MonsterId = @monsterId",
                        new { charId = characterId, monsterId = monsterId });

                    if (record == null)
                        return true;

                    lastFight = record.LastFightTime;
                    _cooldownCache[key] = lastFight;
                }
                catch (Exception ex)
                {
                    Logger.WriteError($"[DopeulReply] CanFight DB error: {ex}");
                    return true;
                }
            }

            double elapsed = (DateTime.UtcNow - lastFight).TotalHours;
            double remaining = CooldownHours - elapsed;

            if (remaining <= 0)
                return true;

            minutesLeft = (int)Math.Ceiling(remaining * 60);
            return false;
        }

        private static void RegisterFightStart(int characterId, int monsterId)
        {
            var now = DateTime.UtcNow;

            string key = $"{characterId}_{monsterId}";

            _cooldownCache[key] = now;

            try
            {
                CharacterDopeulBootstrap.EnsureCooldownTable();

                DatabaseManager.Connection.Execute(
                    @"INSERT INTO characters_dopeul_cooldown
                    (CharacterId, MonsterId, LastFightTime)
                    VALUES
                    (@charId, @monsterId, @time)
                    ON DUPLICATE KEY UPDATE
                    LastFightTime = @time",
                    new { charId = characterId, monsterId = monsterId, time = now });
            }
            catch (Exception ex)
            {
                Logger.WriteError($"[DopeulReply] RegisterFightStart DB error: {ex}");
            }
        }

        private void CloseDialog()
        {
            Client.Character.Dialog = null;
            DialogHandler.SendLeaveDialogMessage(Client);
        }
    }
}
