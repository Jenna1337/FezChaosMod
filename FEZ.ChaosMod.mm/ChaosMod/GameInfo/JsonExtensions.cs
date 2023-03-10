using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Linq;

namespace FezGame.GameInfo
{
    public static class JsonExtensions
    {
        public static string ToJsonString(this Vector3 vector3)
        {
            return $"{{\"X\": {vector3.X}, \"Y\": {vector3.Y}, \"Z\": {vector3.Z}}}";
        }
        public static string ToJsonString(this IDictionary d)
        {
            string s = "{";

            bool notfirst = false;
            foreach(var key in d.Keys)
            {
                if(notfirst)
                {
                    s += ", ";
                }
                s += $"\"{key}\": ";
                object val = d[key];
                Type valtype = val?.GetType();
                if (valtype == null) s += "null";
                else if (valtype.IsAssignableFrom(typeof(bool))) s += ((bool)val) ? "true" : "false";
                else if (valtype.IsNumericPrimative()) s += val;
                else if (valtype.IsAssignableFrom(typeof(Vector3))) s += ((Vector3)val).ToJsonString();
                else if (valtype.IsAssignableFrom(typeof(string))) s += $"\"{val}\"";
                else throw new ArgumentException($"Value type \"{valtype.GetFormattedName()}\" is not supported.");
                notfirst = true;
            }

            return s + "}";
        }
    }
}
