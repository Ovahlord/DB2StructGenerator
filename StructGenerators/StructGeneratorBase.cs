using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBDefsLib;

namespace DB2StructGenerator.StructGenerators
{
    public abstract class StructGeneratorBase
    {
        protected Dictionary<string /*DB2Name*/, Tuple<Structs.DBDefinition, Structs.VersionDefinitions>> definitions;
        protected int buildNumber;
        protected string tabSpaces = "    ";

        public class FieldValue
        {
            public FieldValue(string fieldType, string fieldName, int arraySize = 0, bool index = false, bool noInline = false)
            {
                FieldType = fieldType;
                FieldName = fieldName;
                ArraySize = arraySize;
                Index = index;
                NoInlinine = noInline;
            }

            public string FieldType { get; set; }
            public string FieldName { get; set; }
            public int ArraySize { get; set; }
            public bool Index { get; set; }
            public bool NoInlinine { get; set; }
        }

        public StructGeneratorBase(Dictionary<string /*DB2Name*/, Tuple<Structs.DBDefinition, Structs.VersionDefinitions>> dbddefinitions, int expectedBuildNumber)
        {
            definitions = dbddefinitions;
            buildNumber = expectedBuildNumber;
        }

        public abstract void GenerateStructs();
        public abstract FieldValue GenerateField(Structs.ColumnDefinition columnDefinition, Structs.Definition definition);

        public FieldValue[] GenerateFields(Structs.DBDefinition dBDefinition, Structs.VersionDefinitions versionDefinitions)
        {
            List<FieldValue> fields = [];

            foreach (Structs.Definition definition in versionDefinitions.definitions)
                fields.Add(GenerateField(dBDefinition.columnDefinitions[definition.name], definition));

            // Now we search for duplicate unknown field names and give them an unique identifier
            Dictionary<string, List<int>> duplicateUnknowns = [];
            for (int i = 0; i < fields.Count; ++i)
            {
                FieldValue field = fields[i];
                if (field.FieldName.StartsWith("Unknown"))
                {
                    if (!duplicateUnknowns.TryAdd(field.FieldName, [i]))
                        duplicateUnknowns[field.FieldName].Add(i);
                }
            }

            foreach (var duplicateUnknownFields in duplicateUnknowns)
            {
                if (duplicateUnknownFields.Value.Count <= 1)
                    continue;

                int duplicate = 0;
                foreach (int fieldIndex in duplicateUnknownFields.Value)
                {
                    fields[fieldIndex].FieldName += $"_{duplicate}";
                    ++duplicate;
                }
            }

            return [.. fields];
        }

        public static bool IsUnsignedField(Structs.Definition versionDefinition)
        {
            return !versionDefinition.isSigned || versionDefinition.isID || versionDefinition.isRelation;
        }

        public static string SanitizeFieldName(string fieldName)
        {
            // Unknown Fields
            if (fieldName.StartsWith("Field_"))
            {
                string[] words = fieldName.Split("_");
                if (words.Length >= 3)
                {
                    fieldName = $"Unknown{words[1]}{words[2]}{words[3]}";
                }
            }
            else
                fieldName = string.Join("", fieldName.Replace("_lang", "").Split("_").Select(s => s = char.ToUpper(s[0]) + s.Substring(1)).ToArray());

            return fieldName;
        }
    }
}
