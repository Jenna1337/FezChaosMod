using System;
using System.Linq;

namespace FezGame.GameInfo
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Returns the type name. If this is a generic type, appends
        /// the list of generic type arguments between angle brackets.
        /// (Does not account for embedded / inner generic arguments.)
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.String.</returns>
        public static string GetFormattedName(this Type type)
        {
            if (type.IsGenericType)
            {
                string genericArguments = type.GetGenericArguments()
                                    .Select(GetFormattedName)
                                    .Aggregate((x1, x2) => $"{x1}, {x2}");
                return $"{type.Name.Substring(0, type.Name.IndexOf("`"))}"
                     + $"<{genericArguments}>";
            }
            return type.Name;
        }
        public static bool IsNumericPrimative(this Type t)
        {
            return t.IsAssignableFrom(typeof(byte)) || t.IsAssignableFrom(typeof(sbyte))
                || t.IsAssignableFrom(typeof(short)) || t.IsAssignableFrom(typeof(ushort))
                || t.IsAssignableFrom(typeof(int)) || t.IsAssignableFrom(typeof(uint))
                || t.IsAssignableFrom(typeof(long)) || t.IsAssignableFrom(typeof(ulong))
                || t.IsAssignableFrom(typeof(float)) || t.IsAssignableFrom(typeof(double)) || t.IsAssignableFrom(typeof(decimal));
        }
    }
}
