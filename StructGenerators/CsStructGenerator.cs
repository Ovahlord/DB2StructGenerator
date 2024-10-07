using DB2StructGenerator.HardcodedData;
using DBDefsLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB2StructGenerator.StructGenerators
{
    public class CsStructGenerator : StructGeneratorBase
    {
        public CsStructGenerator(Dictionary<string /*DB2Name*/, Tuple<Structs.DBDefinition, Structs.VersionDefinitions>> dbddefinitions, int expectedBuildNumber) :
            base(dbddefinitions, expectedBuildNumber) { }

        public override void GenerateStructs()
        {
            Directory.CreateDirectory("CsStructs");

            foreach (KeyValuePair<string, Tuple<Structs.DBDefinition, Structs.VersionDefinitions>> pair in definitions)
            {
                using StreamWriter writer = new($"CsStructs\\{pair.Key.Replace("_", "")}Entry.cs");
                writer.WriteLine("using DBFileReaderLib.Attributes;");
                writer.WriteLine();
                writer.WriteLine("namespace WowPacketParser.DBC.Structures.InsertVersionHere");
                writer.WriteLine("{");
                writer.WriteLine($"{tabSpaces}[DBFile(\"{pair.Key}\")]");
                writer.WriteLine($"{tabSpaces}public sealed class {pair.Key.Replace("_", "")}Entry");
                writer.WriteLine(tabSpaces + "{");

                FieldValue[] fields = GenerateFields(pair.Value.Item1, pair.Value.Item2);
                foreach (FieldValue field in fields)
                {
                    if (field.ArraySize > 0)
                    {
                        writer.WriteLine($"{tabSpaces}{tabSpaces}[Cardinality({field.ArraySize})]");
                        writer.WriteLine($"{tabSpaces}{tabSpaces}public {field.FieldType}[] {field.FieldName} = new {field.FieldType}[{field.ArraySize}];");
                        continue;
                    }

                    if (field.Index)
                        writer.WriteLine($"{tabSpaces}{tabSpaces}[Index(true)]");

                    writer.WriteLine($"{tabSpaces}{tabSpaces}public {field.FieldType} {field.FieldName};");
                }

                writer.WriteLine(tabSpaces + "}");
                writer.WriteLine("}");
            }
        }

        public override FieldValue GenerateField(Structs.ColumnDefinition columnDefinition, Structs.Definition versionDefinition)
        {
            string fieldName = SanitizeFieldName(versionDefinition.name);
            string fieldType;
            switch (columnDefinition.type)
            {
                case "int":
                    fieldType = $"{(IsUnsignedField(versionDefinition) ? "u" : "")}";
                    switch (versionDefinition.size)
                    {
                        case 8:
                            fieldType = $"{(IsUnsignedField(versionDefinition) ? "" : "s")}byte";
                            break;
                        case 16:
                            fieldType += "short";
                            break;
                        case 32:
                            fieldType += "int";
                            break;
                        case 64:
                            fieldType += "long";
                            break;
                        default:
                            break;
                    }
                    break;
                case "locstring":
                case "string":
                    fieldType = "string";
                    break;
                case "float":
                    fieldType = "float";
                    break;
                default:
                    fieldType = "undefined";
                    break;
            }

            return new FieldValue(fieldType, fieldName, versionDefinition.arrLength, versionDefinition.isID);
        }
    }
}
