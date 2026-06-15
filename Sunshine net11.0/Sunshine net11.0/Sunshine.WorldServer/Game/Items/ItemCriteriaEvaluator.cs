using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Stats;
using Sunshine.WorldServer.Game.Characters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Items
{
    public static class ItemCriteriaEvaluator
    {
        private static readonly string[] Operators = { ">=", "<=", ">", "<", "=", "!", "#", "~", "s", "S", "e", "E", "v", "i", "X", "/" };

        public static bool IsRespected(Character character, string criteria)
        {
            if (character == null)
                return false;

            if (string.IsNullOrWhiteSpace(criteria) || criteria.Equals("null", StringComparison.OrdinalIgnoreCase))
                return true;

            var normalized = criteria.Replace(" ", string.Empty);

            if (normalized.Contains("|"))
            {
                return normalized.Split('|').Any(group => EvaluateGroup(character, group));
            }

            return EvaluateGroup(character, normalized);
        }

        private static bool EvaluateGroup(Character character, string group)
        {
            if (string.IsNullOrWhiteSpace(group))
                return true;

            if (group.StartsWith("(") && group.EndsWith(")"))
                group = group.Substring(1, group.Length - 2);

            if (group.Contains("&"))
            {
                return group.Split('&').All(part => EvaluateSingle(character, part));
            }

            return EvaluateSingle(character, group);
        }

        private static bool EvaluateSingle(Character character, string criterion)
        {
            if (string.IsNullOrWhiteSpace(criterion))
                return true;

            if (!TryParseCriterion(criterion, out var reference, out var op, out var expected))
                return true;

            int actual = GetCriterionValue(character, reference);
            return Compare(actual, op, expected);
        }

        private static bool TryParseCriterion(string criterion, out string reference, out string op, out int expected)
        {
            reference = string.Empty;
            op = string.Empty;
            expected = 0;

            if (criterion.Length < 3)
                return false;

            reference = criterion.Substring(0, 2);

            foreach (var candidate in Operators.OrderByDescending(x => x.Length))
            {
                var index = criterion.IndexOf(candidate, 2, StringComparison.Ordinal);
                if (index != 2)
                    continue;

                op = candidate;
                var valuePart = criterion.Substring(2 + candidate.Length);
                return int.TryParse(valuePart, out expected);
            }

            return false;
        }

        private static int GetCriterionValue(Character character, string reference)
        {
            switch (reference)
            {
                case "PL":
                    return character.Level;
                case "PG":
                    return (int)character.Breed;
                case "Cs":
                    return character.Stats[StatsEnum.Strength].Base;
                case "CS":
                    return GetTotalCharacteristic(character.Stats[StatsEnum.Strength]);
                case "Ca":
                    return character.Stats[StatsEnum.Agility].Base;
                case "CA":
                    return GetTotalCharacteristic(character.Stats[StatsEnum.Agility]);
                case "Cc":
                    return character.Stats[StatsEnum.Chance].Base;
                case "CC":
                    return GetTotalCharacteristic(character.Stats[StatsEnum.Chance]);
                case "Ci":
                    return character.Stats[StatsEnum.Intelligence].Base;
                case "CI":
                    return GetTotalCharacteristic(character.Stats[StatsEnum.Intelligence]);
                case "Cv":
                    return character.Stats[StatsEnum.Vitality].Base;
                case "CV":
                    return GetTotalCharacteristic(character.Stats[StatsEnum.Vitality]);
                case "Cw":
                    return character.Stats[StatsEnum.Wisdom].Base;
                case "CW":
                    return GetTotalCharacteristic(character.Stats[StatsEnum.Wisdom]);
                case "CM":
                    return GetTotalCharacteristic(character.Stats[StatsEnum.MP]);
                case "CP":
                    return GetTotalCharacteristic(character.Stats[StatsEnum.AP]);
                case "Ct":
                    return GetTotalCharacteristic(character.Stats[StatsEnum.TackleEvade]);
                case "CT":
                    return GetTotalCharacteristic(character.Stats[StatsEnum.TackleBlock]);
                case "CL":
                    return character.Stats.Health.Total;
                case "CH":
                    return character.Alignment != null ? character.Alignment.Honor : 0;
                case "CD":
                    return character.Alignment != null ? character.Alignment.Dishonor : 0;
                default:
                    return 0;
            }
        }

        private static int GetTotalCharacteristic(StatsData stats)
        {
            if (stats == null)
                return 0;

            return stats.Base + stats.Equiped + stats.Context;
        }

        private static bool Compare(int actual, string op, int expected)
        {
            switch (op)
            {
                case ">":
                    return actual > expected;
                case "<":
                    return actual < expected;
                case "=":
                    return actual == expected;
                case "!":
                    return actual != expected;
                case ">=":
                    return actual >= expected;
                case "<=":
                    return actual <= expected;
                default:
                    return false;
            }
        }
    }
}
