using Sunshine.BaseServer.Configuration;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Effects.Spells.Damages;
using Sunshine.WorldServer.Game.Fights.Buffs;
using Sunshine.WorldServer.Game.Fights.Buffs.Customs;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
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

        // Mirror combat log lines to the server console. Defaults to on while Enabled;
        // can be turned off with FIGHT_COMBAT_LOG_CONSOLE=false or FightCombatLogConsole=false.
        private static bool ConsoleEnabled
        {
            get
            {
                var env = Environment.GetEnvironmentVariable("FIGHT_COMBAT_LOG_CONSOLE");
                if (!string.IsNullOrWhiteSpace(env))
                    return env.Equals("true", StringComparison.OrdinalIgnoreCase) || env == "1";

                return GameConfig.GetString("FightCombatLogConsole", "true")
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

        public static void LogSpellCast(Fight fight, FightActor caster, Spell spell, short cell,
            FightSpellCastCriticalEnum critical = FightSpellCastCriticalEnum.NORMAL, int handlerCount = -1)
        {
            if (!Enabled || fight == null)
                return;

            var detail = $"event=CAST caster={FighterId(caster)} spell={SpellId(spell)} cell={cell} critical={critical}";
            if (handlerCount >= 0)
                detail += $" handlers={handlerCount}";
            Write(fight.Id, detail);
        }

        public static void LogSpellCastFailed(Fight fight, FightActor caster, Spell spell, string reason)
        {
            if (!Enabled || fight == null)
                return;

            Write(fight.Id,
                $"event=CAST_FAIL caster={FighterId(caster)} spell={SpellId(spell)} reason={reason}");
        }

        public static void LogSummonFail(Fight fight, FightActor caster, Spell spell, int monsterId, string reason)
        {
            if (!Enabled || fight == null)
                return;

            Write(fight.Id,
                $"event=SUMMON_FAIL caster={FighterId(caster)} spell={SpellId(spell)} monster={monsterId} reason={reason}");
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

        public static void LogSummonCreate(Fight fight, FightActor summoner, FightActor summon, int monsterId,
            short cell, bool usesSlot, int summonedCount, int summonLimit)
        {
            if (!Enabled || fight == null)
                return;

            Write(fight.Id,
                $"event=SUMMON_CREATE summoner={FighterId(summoner)} summon={FighterId(summon)} monster={monsterId} type={TypeName(summon)} cell={cell} usesSlot={usesSlot} count={summonedCount} limit={summonLimit}");
        }

        public static void LogTurnSkip(Fight fight, FightActor actor, string reason)
        {
            if (!Enabled || fight == null)
                return;

            Write(fight.Id, $"event=TURN_SKIP actor={FighterId(actor)} type={TypeName(actor)} reason={reason}");
        }

        public static void LogBuffAdd(Fight fight, FightActor target, Buff buff)
        {
            if (!Enabled || fight == null || buff == null)
                return;

            Write(fight.Id,
                $"event=BUFF_ADD kind={BuffKind(buff)} target={FighterId(target)} caster={FighterId(buff.Caster)} spell={SpellId(buff.Spell)} duration={buff.Duration} {BuffPayload(buff)}");
        }

        public static void LogBuffTick(Fight fight, FightActor target, Buff buff, string kind, int amount, short remaining)
        {
            if (!Enabled || fight == null || buff == null)
                return;

            Write(fight.Id,
                $"event=BUFF_TICK kind={kind} target={FighterId(target)} caster={FighterId(buff.Caster)} spell={SpellId(buff.Spell)} amount={amount} remaining={remaining}");
        }

        public static void LogBuffExpire(Fight fight, FightActor target, Buff buff, string reason)
        {
            if (!Enabled || fight == null || buff == null)
                return;

            Write(fight.Id,
                $"event=BUFF_EXPIRE kind={BuffKind(buff)} target={FighterId(target)} caster={FighterId(buff.Caster)} spell={SpellId(buff.Spell)} reason={reason}");
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

                if (ConsoleEnabled)
                    Logs.Logger.Write("[FIGHT] " + line);
            }
            catch
            {
            }
        }

        private static int FighterId(FightActor actor) => actor?.Id ?? 0;

        private static int SpellId(Spell spell) => spell?.Id ?? 0;

        private static string TypeName(FightActor actor) => actor?.GetType().Name ?? "null";

        private static string BuffKind(Buff buff)
        {
            if (buff is DamageOverTimeBuff)
                return "DOT";
            if (buff is HealOverTimeBuff)
                return "HOT";
            return buff?.GetType().Name ?? "null";
        }

        private static string BuffPayload(Buff buff)
        {
            if (buff is DamageOverTimeBuff dot)
                return $"dice={dot.DiceNum}-{dot.DiceFace} school={dot.EffectSchool}";
            if (buff is HealOverTimeBuff hot)
                return $"value={hot.Value}";
            return $"effect={buff?.Effect?.Id}";
        }
    }
}
