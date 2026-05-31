using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Actors.Look;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Sunshine.WorldServer.Game.Actors
{
    public class EntityManager : Singleton<EntityManager>
    {
        public string BuildEntityLook(int breedId, bool sex, List<int> colors)
        {
            string breedLook = BreedManager.Instance.GetLook(breedId, sex);
            if (string.IsNullOrWhiteSpace(breedLook))
                return "{0}";

            var actorLook = GetActorLook(breedLook);
            var baseColors = GetBreedBaseColors(breedId, sex);
            var requestedColors = colors ?? new List<int>();

            int maxColors = Math.Max(baseColors.Count, requestedColors.Count);
            for (int index = 1; index <= maxColors; index++)
            {
                int requested = index - 1 < requestedColors.Count ? requestedColors[index - 1] : -1;
                int fallback = index - 1 < baseColors.Count ? baseColors[index - 1] : 0;
                int resolved = requested == -1 ? fallback : requested;

                if (resolved == -1)
                    continue;

                if ((resolved & unchecked((int)0xFF000000)) == 0)
                    resolved = unchecked((int)0xFF000000) | (resolved & 0xFFFFFF);

                actorLook.AddColor(index, Color.FromArgb(resolved));
            }

            return ParseEntityLook(actorLook.GetEntityLook());
        }

        public string ParseEntityLook(EntityLook look)
        {
            if (look == null)
                return "{}";

            var result = new StringBuilder();
            result.Append("{");

            int missingBars = 0;
            result.Append(look.bonesId);

            if (look.skins == null || !look.skins.Any())
            {
                missingBars++;
            }
            else
            {
                result.Append("|".ConcatCopy(missingBars + 1));
                missingBars = 0;
                result.Append(string.Join(",", look.skins));
            }

            if (look.indexedColors == null || !look.indexedColors.Any())
            {
                missingBars++;
            }
            else
            {
                result.Append("|".ConcatCopy(missingBars + 1));
                missingBars = 0;
                result.Append(string.Join(",", look.indexedColors.Select(ParseIndexedColorEntry)));
            }

            if (look.scales == null || !look.scales.Any())
            {
                missingBars++;
            }
            else
            {
                result.Append("|".ConcatCopy(missingBars + 1));
                missingBars = 0;
                result.Append(string.Join(",", look.scales));
            }

            if (look.subentities == null || !look.subentities.Any())
            {
                missingBars++;
            }
            else
            {
                result.Append("|".ConcatCopy(missingBars + 1));
                result.Append(string.Join(",", look.subentities));
            }

            result.Append("}");
            return result.ToString();
        }

        public ActorLook GetActorLook(string str)
        {
            if (string.IsNullOrWhiteSpace(str) || str[0] != '{')
            {
                Console.WriteLine("Incorrect EntityLook format : {0}", str);
                return new ActorLook();
            }

            int index = 1;
            int separator = str.IndexOf('|');
            if (separator == -1)
            {
                separator = str.IndexOf('}');
                if (separator == -1)
                    throw new Exception("Incorrect EntityLook format : " + str);
            }

            short bones = short.Parse(str.Substring(index, separator - index));
            index = separator + 1;

            short[] skins = Array.Empty<short>();
            if ((separator = str.IndexOf('|', index)) != -1 || (separator = str.IndexOf('}', index)) != -1)
            {
                skins = ParseCollection(str.Substring(index, separator - index), short.Parse);
                index = separator + 1;
            }

            Tuple<int, int>[] indexedColors = Array.Empty<Tuple<int, int>>();
            if ((separator = str.IndexOf('|', index)) != -1 || (separator = str.IndexOf('}', index)) != -1)
            {
                indexedColors = ParseCollection(str.Substring(index, separator - index), ParseIndexedColor);
                index = separator + 1;
            }

            short[] scales = Array.Empty<short>();
            if ((separator = str.IndexOf('|', index)) != -1 || (separator = str.IndexOf('}', index)) != -1)
            {
                scales = ParseCollection(str.Substring(index, separator - index), short.Parse);
                index = separator + 1;
            }

            var subLooks = new List<SubActorLook>();
            while (index < str.Length)
            {
                if (str[index] == '}')
                    break;

                int categorySeparator = str.IndexOf('@', index);
                if (categorySeparator == -1)
                    break;

                int bindingSeparator = str.IndexOf('=', categorySeparator + 1);
                if (bindingSeparator == -1)
                    break;

                if (!byte.TryParse(str.Substring(index, categorySeparator - index), out byte category) ||
                    !byte.TryParse(str.Substring(categorySeparator + 1, bindingSeparator - categorySeparator - 1), out byte bindingIndex))
                    break;

                int nesting = 0;
                int cursor = bindingSeparator + 1;
                var builder = new StringBuilder();
                while (cursor < str.Length)
                {
                    builder.Append(str[cursor]);
                    if (str[cursor] == '{')
                        nesting++;
                    else if (str[cursor] == '}')
                    {
                        nesting--;
                        if (nesting <= 0)
                            break;
                    }

                    cursor++;
                }

                var subLookString = builder.ToString();
                if (!string.IsNullOrWhiteSpace(subLookString) && subLookString[0] == '{')
                    subLooks.Add(new SubActorLook((sbyte)bindingIndex, (SubEntityBindingPointCategoryEnum)category, GetActorLook(subLookString)));

                index = cursor + 1;
                if (index < str.Length && str[index] == ',')
                    index++;
            }

            return new ActorLook(
                bones,
                skins,
                indexedColors.ToDictionary(
                    entry => entry.Item1,
                    entry =>
                    {
                        int value = entry.Item2;
                        if ((value & unchecked((int)0xFF000000)) == 0)
                            value = unchecked((int)0xFF000000) | (value & 0xFFFFFF);
                        return Color.FromArgb(value);
                    }),
                scales,
                subLooks.ToArray());
        }

        private static string ParseIndexedColorEntry(int packedColor)
        {
            int colorIndex = packedColor >> 24;
            int colorValue = packedColor & 0xFFFFFF;
            return colorIndex + "=#" + colorValue.ToString("X6");
        }

        private static List<int> GetBreedBaseColors(int breedId, bool sex)
        {
            if (!BreedManager.Instance.BreedColors.ContainsKey(breedId))
                return new List<int>();

            return sex
                ? BreedManager.Instance.BreedColors[breedId].FemaleColors.ConvertAll(entry => (int)entry)
                : BreedManager.Instance.BreedColors[breedId].MaleColors.ConvertAll(entry => (int)entry);
        }

        private Tuple<int, int> ParseIndexedColor(string str)
        {
            int separator = str.IndexOf('=');
            bool isHex = separator + 1 < str.Length && str[separator + 1] == '#';
            int index = int.Parse(str.Substring(0, separator));
            int value = int.Parse(
                str.Substring(separator + (isHex ? 2 : 1), str.Length - separator - (isHex ? 2 : 1)),
                isHex ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer);

            return Tuple.Create(index, value);
        }

        private T[] ParseCollection<T>(string str, Func<string, T> converter)
        {
            if (string.IsNullOrEmpty(str))
                return Array.Empty<T>();

            int separator = str.IndexOf(',');
            if (separator == -1)
                return new[] { converter(str) };

            int start = 0;
            var results = new T[str.CountOccurences(',', 0, str.Length) + 1];
            int resultIndex = 0;

            while (separator != -1)
            {
                results[resultIndex++] = converter(str.Substring(start, separator - start));
                start = separator + 1;
                separator = str.IndexOf(',', start);
            }

            results[resultIndex] = converter(str.Substring(start, str.Length - start));
            return results;
        }
    }
}
