using Sunshine.AuthServer;
using Sunshine.Mysql.Database;
using Sunshine.WorldServer;
using System;
using Dapper;
using Dapper.Contrib.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Sunshine.WorldServer.Commands;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Game.Items;

namespace Sunshine.Protocol.Utils.Extensions
{
    public static class Extensions
    {
        private static bool IsWorldHandler(this MethodInfo method)
        {
            return method.GetCustomAttribute(typeof(WorldHandler)) is WorldHandler;
        }

        private static bool IsAuthHandler(this MethodInfo method)
        {
            return method.GetCustomAttribute(typeof(AuthHandler)) is AuthHandler;
        }

        private static WorldHandler GetFirstWorldHandler(this MethodInfo method)
        {
            if (method.IsWorldHandler())
                return method.GetCustomAttribute(typeof(WorldHandler)) as WorldHandler;
            else
                return null;
        }

        private static AuthHandler GetFirstAuthHandler(this MethodInfo method)
        {
            if (method.IsAuthHandler())
                return method.GetCustomAttribute(typeof(AuthHandler)) as AuthHandler;
            else
                return null;
        }

        public static MethodInfo FirstOrDefault(this MethodInfo[] method, uint id, bool isAuth = false)
        {
            if(isAuth)
                return method.FirstOrDefault(x => x.GetFirstAuthHandler() != null && x.GetFirstAuthHandler().Id == id);
            else
                return method.FirstOrDefault(x => x.GetFirstWorldHandler() != null && x.GetFirstWorldHandler().Id == id);
        }

        public static int GetUnixTimeStamp(this DateTime date)
        {
            return (int)(date - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime()).TotalSeconds;
        }

        public static byte GetUnixTimeStampByte(this DateTime date)
        {
            return (byte)(date - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime()).TotalMilliseconds;
        }

        public static DirectionsEnum GetOpposedDirection(this DirectionsEnum direction) => (DirectionsEnum)(((int)direction + 4) % 8);

        public static bool IsDiagonal(this DirectionsEnum direction) => ((int)direction) % 2 == 0;

        public static DirectionsEnum[] GetDiagonalDecomposition(this DirectionsEnum direction)
        {
            switch (direction)
            {
                case DirectionsEnum.DIRECTION_EAST:
                    return new[] { DirectionsEnum.DIRECTION_NORTH_EAST, DirectionsEnum.DIRECTION_SOUTH_EAST };
                case DirectionsEnum.DIRECTION_WEST:
                    return new[] { DirectionsEnum.DIRECTION_NORTH_WEST, DirectionsEnum.DIRECTION_SOUTH_WEST };
                case DirectionsEnum.DIRECTION_NORTH:
                    return new[] { DirectionsEnum.DIRECTION_NORTH_WEST, DirectionsEnum.DIRECTION_NORTH_EAST };
                case DirectionsEnum.DIRECTION_SOUTH:
                    return new[] { DirectionsEnum.DIRECTION_SOUTH_WEST, DirectionsEnum.DIRECTION_SOUTH_EAST };
                default:
                    return new[] { direction };
            }
        }

        public static void Add(this List<FightActor> fighters, FightActor actor)
        {
            if (fighters.Contains(actor))
                fighters.Remove(actor);

            fighters.Add(actor);
        }

        public static List<Effect> Clone(this List<Effect> effects)
        {
            List<Effect> effectsClone = new List<Effect>();
            foreach (var effect in effects)
                effectsClone.Add(effect.Clone());
            return new List<Effect>(effectsClone);
        }

        public static List<BasePlayerItem> Distinct(this IEnumerable<BasePlayerItem> items)
        {
            List<List<Effect>> listEffects = new List<List<Effect>>();
            List<BasePlayerItem> listItems = new List<BasePlayerItem>();
            foreach(var item in items.OrderBy(x => x.Price))
            {
                if (!listEffects.Any(x => IsSameEffects(x, item.Effects)))
                {
                    listEffects.Add(item.Effects);
                    listItems.Add(item);
                }
            }
            return listItems;
        }

        public static IEnumerable<T> ToIEnumerable<T>(this string chaine, char split) where T : struct
        {
            var texts = chaine.Split(split);
            List<object> list = new List<object>();
            switch(typeof(T).Name)
            {
                case "short":
                    foreach(var text in texts)
                        list.Add((short)int.Parse(text));
                    break;

                case "int":
                    foreach (var text in texts)
                        list.Add(int.Parse(text));
                    break;

                case "string":
                    return texts as IEnumerable<T>;
            }
            return list as IEnumerable<T>;
        }

        private static bool IsSameEffects(List<Effect> effects, List<Effect> secondEffects)
        {
            if (effects.Count == 0 && secondEffects.Count == 0)
                return true;
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].Value != secondEffects[i].Value)
                    return false;
            }
            return true;
        }
    }
}
