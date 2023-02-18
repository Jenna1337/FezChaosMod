using FezEngine.Structure.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FezGame.GameInfo
{
    internal class ScriptDescriptor
    {
        public static string ListAllScriptEntityTypeDescriptors()
        {
            IDictionary<string, EntityTypeDescriptor> types = EntityTypes.Types;
            string s0 = "{";

            s0 += String.Join(", ", types.Select(pair =>
            {
                string s = "";
                EntityTypeDescriptor t = pair.Value;
                s += $"\"{pair.Key}\": {{";
                s += $"\"Name\": \"{t.Name}\", ";
                s += $"\"Static\": {t.Static.ToString().ToLower()}, ";
                s += $"\"Model\": {(t.Model != null ? $"{{\"Name\": \"{t.Model.Name}\", \"FullName\": \"{t.Model.FullName}\"}}" : "null")}, ";
                s += $"\"RestrictTo\": {(t.RestrictTo != null ?  $"{{\"ActorTypes\": [{String.Join(", ", t.RestrictTo.Select(a=>$"\"{a.ToString()}\""))}]}}" : "null")}, ";
                s += $"\"Interface\": {{\"Name\": \"{t.Interface.Name}\", \"FullName\": \"{t.Interface.FullName}\"}}, ";
                s += $"\"Operations\": {{";
                s += String.Join(", ", t.Operations.Select(op =>
                {
                    string s2 = "";
                    s2 += $"\"{op.Key}\": {{";
                    OperationDescriptor desc = op.Value;
                    s2 += $"\"Name\": \"{desc.Name}\", ";
                    s2 += $"\"Description\": \"{desc.Description ?? ""}\", ";
                    s2 += $"\"Parameters\": [";
                    s2 += String.Join(", ", desc.Parameters.Select(par => $"{{\"Name\": \"{par.Name}\", \"Type\": {{\"Name\": \"{par.Type.Name}\", \"FullName\": \"{par.Type.FullName}\"}}}}"));
                    s2 += "]";
                    s2 += "}";
                    return s2;
                }));
                s += "}, ";
                s += $"\"Properties\": {{";
                s += String.Join(", ", t.Properties.Select(props =>
                {
                    string s2 = "";
                    s2 += $"\"{props.Key}\": {{";
                    PropertyDescriptor desc = props.Value;
                    s2 += $"\"Name\": \"{desc.Name}\", ";
                    s2 += $"\"Description\": \"{desc.Description ?? ""}\", ";
                    s2 += $"\"Type\": {{\"Name\": \"{desc.Type.Name}\", \"FullName\": \"{desc.Type.FullName}\"}}";
                    s2 += "}";
                    return s2;
                }));
                s += "}, ";
                s += $"\"Events\": {{";
                s += String.Join(", ", t.Events.Select(ev =>
                {
                    string s2 = "";
                    s2 += $"\"{ev.Key}\": {{";
                    EventDescriptor desc = ev.Value;
                    s2 += $"\"Name\": \"{desc.Name}\", ";
                    s2 += $"\"Description\": \"{desc.Description ?? ""}\"";
                    s2 += "}";
                    return s2;
                }));
                s += "}";
                s += "}";
                return s;
            }));
            s0 += "}";
            return s0;
        }
    }
}
