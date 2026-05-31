using System;

namespace Sunshine.Protocol.Utils.Extensions
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Fonction permettant de déterminer si le type type possède l'interface interfaceType.
        /// </summary>
        /// <param name="type">Type auquel regarder s'il possède l'interface interfaceType.</param>
        /// <param name="interfaceType">Interface à chercher.</param>
        /// <returns>True si type possède l'interface interfaceType, sinon False.</returns>
        public static bool HasInterface(this Type type, Type interfaceType)
        {
            bool FilterByName(Type typeObj, object criteriaObj) => typeObj.ToString() == criteriaObj.ToString();

            return type.FindInterfaces(FilterByName, interfaceType).Length > 0;
        }
    }

}