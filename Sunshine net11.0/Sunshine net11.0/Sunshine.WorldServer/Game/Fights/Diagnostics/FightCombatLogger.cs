using Sunshine.BaseServer.Configuration;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Effects.Spells.Damages;
using Sunshine.WorldServer.Game.Fights.Buffs.Customs;
using Sunshine.WorldServer.Game.Spells;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text;

namespace Sunshine.WorldServer.Game.Fights.Diagnostics
{
    /// <summary>
    /// Combat diagnostics for develop-build (inspired by Rollback FightTelemetry).
    /// Enable with FIGHT_COMBAT_LOG_ENABLED=true or FightCombatLogEnabled in Config.xml.
    /// </summary>
    public static class FightCombatLogger
    {
        private static readonly object FileLock = new object();
        private static readonly ConcurrentDictionary<int, string> FightLogPaths = new ConcurrentDictionary<int, string>();

        public static bool Enabled
        {
            get
            {
                var env = Environment.GetEnvironmentVariable("FIGHT_COMBAT_LOG_ENABLED");
                if (!string.IsNullOrWhiteSpace(env) &&
                    (env.Equals("true", StringComparison.OrdinalIgnoreCase) || env == "1"))
                    return true;

                return GameConfig.GetString("FightCombatLogEnabled", "false")
                    .Equals("true", StringComparison.OrdinalIgnoreCase);
            }
        }

        public static void LogEffectDispatch(Fight fight, FightActor caster, Spell spell, Effect effect)
        {
            if (!Enabled || fight == null || effect == null)
                return;

            Write(fight.Id,
                $"event=DISPATCH caster={FighterId(caster)} spell={SpellId(spell)} effect={effect.Id} duration={effect.Duration} dice={effect.DiceNum}-{effect.DiceFace}");
        }

        public static void LogSpellCast(Fight fight, FightActor caster, Spell spell, short cell)
        {
            if (!Enabled || fight == null)
                return;

            Write(fight.Id, $"event=CAST caster={FighterId(caster)} spell={SpellId(spell)} cell={cell}");
        }

        public static void LogTrigger(Fight fight, FightActor target, BuffTriggerType trigger, TriggerBuff buff)
        {
            if (!Enabled || fight == null || buff == null)
                return;

            Write(fight.Id,
                $"event=TRIGGER type={trigger} buff={buff.Id} target={FighterId(target)} effect={buff.Effect?.Id}");
        }

        public static void LogDamage(Fight fight, FightActor source, FightActor target, Damage damage)
        {
            if (!Enabled || fight == null || damage == null)
                return;

            Write(fight.Id,
                $"event=DAMAGE src={FighterId(source)} tgt={FighterId(target)} amount={damage.Amount} school={damage.EffectSchool}");
        }

        public static void LogKill(Fight fight, FightActor killer, FightActor target)
        {
            if (!Enabled || fight == null)
                return;

            Write(fight.Id, $"event=KILL killer={FighterId(killer)} target={FighterId(target)}");
        }

        public static void LogSummonDie(Fight fight, FightActor summon, FightActor byFighter)
        {
            if (!Enabled || fight == null)
                return;

            Write(fight.Id, $"event=SUMMON_DIE summon={FighterId(summon)} by={FighterId(byFighter)}");
        }

        public static void LogSocket(Fight fight, string messageName, int recipients)
        {
            if (!Enabled || fight == null || recipients <= 0)
                return;

            Write(fight.Id, $"event=SOCKET msg={messageName} recipients={recipients}");
        }

        private static void Write(int fightId, string detail)
        {
            try
            {
                var line = string.Format(
                    CultureInfo.InvariantCulture,
                    "[{0:yyyy-MM-dd HH:mm:ss.fff}] fight={1} {2}",
                    DateTime.UtcNow,
                    fightId,
                    detail);

                var path = FightLogPaths.GetOrAdd(fightId, id =>
                {
                    // /app/runtime is read-only in Docker; use /app/logs/fights (writable).
                    var baseDir = Environment.GetEnvironmentVariable("FIGHT_COMBAT_LOG_DIR");
                    if (string.IsNullOrWhiteSpace(baseDir))
                        baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "fights");
                    Directory.CreateDirectory(baseDir);
                    return Path.Combine(baseDir, id + ".log");
                });

                lock (FileLock)
                {
                    File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
            }
        }

        private static int FighterId(FightActor actor) => actor?.Id ?? 0;

        private static int SpellId(Spell spell) => spell?.Id ?? 0;
    }
}
