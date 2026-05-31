using Sunshine.Protocol.Types;
using Sunshine.WorldServer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.Protocol.Utils
{
    public static class Utils
    {
        public static string RandomString(int lenght, bool upper)
        {
            string str = string.Empty;
            Random rdn = new Random();
            for (int i = 1; i <= lenght; i++)
            {
                int num = rdn.Next(0, 26);
                str += (char)('a' + num);
            }
            return upper ? str.ToUpper() : str;
        }

        public static int CountOccurences(this string str, char chr, int startIndex, int count)
        {
            int occurences = 0;

            for (int i = startIndex; i < startIndex + count; i++)
            {
                if (str[i] == chr)
                    occurences++;
            }

            return occurences;
        }

        public static int CountOccurences(this string str, char chr)
        {
            return CountOccurences(str, chr, 0, str.Length);
        }

        public static string ConcatCopy(this string str, int times)
        {
            var builder = new StringBuilder(str.Length * times);

            for (int i = 0; i < times; i++)
            {
                builder.Append(str);
            }

            return builder.ToString();
        }

        public static string GetMD5Hash(string text)
        {
            MD5 _md5 = MD5.Create();
            byte[] _inputBytes = System.Text.Encoding.ASCII.GetBytes(text);
            byte[] _hash = _md5.ComputeHash(_inputBytes);
            StringBuilder _builder = new StringBuilder();
            for (int i = 0; i < _hash.Length; i++)
            {
                _builder.Append(_hash[i].ToString("x2"));
            }
            return _builder.ToString();
        }

        public static byte[] GetHexaToByteArray(string s)
        {
            if (s == null || s == "")
                return new byte[0];

            int len = s.ToArray().Length;
            byte[] data = new byte[len / 2];
            for (int i = 0; i < len; i += 2)
                data[i / 2] = (byte)((Convert.ToInt32(s[i].ToString(), 16) << 4) + Convert.ToInt32(s[i + 1].ToString(), 16));
            return data;
        }

        public static Color GetColorSpell(int spellId)
        {
            switch (spellId)
            {
                case 1493:
                case 2007:
                    return Color.FromArgb(5911580);
                case 1688:
                    return Color.FromArgb(10219757);
                case 1499:
                    return Color.FromArgb(0);
                case 1497:
                    return Color.FromArgb(3222918);
                case 1495:
                    return Color.FromArgb(6888565);
                case 5387:
                    return Color.White;
                default:
                    return Color.FromArgb(2258204);
            }
        }

        #region GenerateName
        public static char GetChar(bool vowel, System.Random rand)
        {
            return vowel ? RandomVowel(rand) : RandomConsonant(rand);
        }

        public static char RandomVowel(System.Random rand)
        {
            return "aeiouy"[rand.Next(0, "aeiouy".Length - 1)];
        }

        public static char RandomConsonant(System.Random rand)
        {
            return "bcdfghjklmnpqrstvwxz"[rand.Next(0, "bcdfghjklmnpqrstvwxz".Length - 1)];
        }
        #endregion
    }
}
