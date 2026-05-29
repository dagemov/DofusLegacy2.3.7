using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sunshine.WorldServer.Game.Maps.Triggers.Types;
using System.Text;
using System.Threading.Tasks;
using Sunshine.WorldServer.Game.Maps.Triggers;
using System.Linq.Expressions;
using Sunshine.MySql.Database.Managers;

namespace Sunshine.BaseServer.Loaders.World.Maps.Triggers
{
    public static class TriggersLoader
    {
        public static void Initialize()
        {         
            var types = Assembly.GetAssembly(typeof(WorldServer.Game.Maps.Triggers.Types.Type)).GetTypes().Where(x => x.BaseType != null && x.BaseType.Name == "Type");
            foreach (var type in types)
            {
                foreach (var typeAttribute in type.GetCustomAttributes())
                {
                    var attribute = typeAttribute as TypeHandler;
                    var typeId = attribute.Id;
                    var currentType = type.GetConstructor(System.Type.EmptyTypes);
                    var function = Expression.Lambda<Func<WorldServer.Game.Maps.Triggers.Types.Type>>(Expression.New(currentType));
                    MapManager.Instance.Triggers.Add(typeId, function.Compile());
                }
            }
        }
    }
}
