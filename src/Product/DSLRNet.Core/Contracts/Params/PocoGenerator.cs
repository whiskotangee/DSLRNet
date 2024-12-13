namespace DSLRNet.Core.Contracts.Params;
using System;
using System.Collections.Generic;
using System.Text;

public class PocoGenerator
{
    private static readonly Dictionary<Type, string> typeMap = new Dictionary<Type, string>
    {
        { typeof(int), "int" },
        { typeof(float), "float" },
        { typeof(double), "double" },
        { typeof(decimal), "decimal" },
        { typeof(long), "long" },
        { typeof(bool), "bool" },
        { typeof(byte), "byte" },
        { typeof(sbyte), "sbyte" },
        { typeof(char), "char" },
        { typeof(short), "short" },
        { typeof(uint), "uint" },
        { typeof(ulong), "ulong" },
        { typeof(ushort), "ushort" },
        { typeof(object), "object" },
        { typeof(string), "string" }
    };

    public static string GenerateClass(string className, PARAM.Row row)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using System;");
        sb.AppendLine();
        sb.AppendLine($"public class {className} : ParamBase<{className}>");
        sb.AppendLine("{");

        sb.AppendLine($"    public int ID {{ get {{ return this.GenericParam.GetValue<int>(\"ID\"); }} set {{ this.GenericParam.SetValue(\"ID\", value); }} }}");
        sb.AppendLine($"    public string Name {{ get {{ return this.GenericParam.GetValue<string>(\"Name\"); }} set {{ this.GenericParam.SetValue(\"Name\", value); }} }}");

        foreach (var cell in row.Cells)
        {
            string propertyName = cell.Def.InternalName;
            string propertyType = GetFriendlyTypeName(cell.Value.GetType());

            sb.AppendLine($"    public {propertyType} {propertyName} {{ get {{ return this.GenericParam.GetValue<{propertyType}>(\"{propertyName}\"); }} set {{ this.GenericParam.SetValue(\"{propertyName}\", value); }} }}");
        }

        sb.AppendLine("}");

        File.WriteAllText($"DAL\\Generated\\{className}.cs", sb.ToString());

        return sb.ToString();
    }

    private static string GetFriendlyTypeName(Type type)
    {
        return typeMap.TryGetValue(type, out string friendlyName) ? friendlyName : type.Name;
    }
}

