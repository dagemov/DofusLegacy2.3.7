using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Game.Actors.Characters.Jobs;
using Sunshine.WorldServer.Game.Maps.Interactives;
using Sunshine.WorldServer.Game.Maps.Interactives.Skills;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Sunshine.BaseServer.Loaders.World.Maps.Interactives
{
    public static class InteractivesLoader
    {
        public static void Initialize()
        {
            JobManager.Instance.JobSkills = InteractiveManager.Instance.GetAllInteractiveSkills();

            var skillBaseType = typeof(Skill);
            var skills = Assembly.GetAssembly(skillBaseType)
                .GetTypes()
                .Where(x => skillBaseType.IsAssignableFrom(x) && !x.IsAbstract && x.GetConstructor(Type.EmptyTypes) != null);

            foreach (var skill in skills)
            {
                var handlers = skill.GetCustomAttributes(typeof(SkillHandler), true).Cast<SkillHandler>().ToArray();
                if (handlers.Length == 0)
                    continue;

                var currentSkill = skill.GetConstructor(Type.EmptyTypes);
                var function = Expression.Lambda<Func<Skill>>(Expression.New(currentSkill)).Compile();

                foreach (var attribute in handlers)
                {
                    if (!SkillManager.Instance.Skills.ContainsKey(attribute.Id))
                        SkillManager.Instance.Skills.Add(attribute.Id, function);
                }
            }
        }
    }
}
