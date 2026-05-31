using Sunshine.Protocol.Types;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Sunshine.Protocol.Types
{
    public class ProtocolTypeManager
    {
        private static readonly Dictionary<uint, Func<object>> Types = new Dictionary<uint, Func<object>>();

        public static void Initialize()
        {
            var _types = Assembly.GetAssembly(typeof(Version)).GetTypes().Where(x => x.Namespace != null && x.Namespace == "Sunshine.Protocol.Types");
            foreach (Type type in _types)
            {
                FieldInfo field = type.GetField("Id");
                if (field != null)
                {
                    short id = (short)field.GetValue(type);
                    Expression body = Expression.New(type);
                    var cmp = Expression.Lambda<Func<object>>(body).Compile();
                    Types.Add((ushort)id, cmp);
                }
            }
        }

        public static T GetInstance<T>(short id) where T : class
        {
            if (!Types.ContainsKey((uint)id))
            {
                throw new Exception("type dosen't existe");
            }else
            {

                var instance = Types[(uint)id]() as T;
                return instance;              
            }
        }

    }
}

