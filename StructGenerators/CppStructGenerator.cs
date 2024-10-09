using DB2StructGenerator.HardcodedData;
using DBDefsLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DB2StructGenerator.StructGenerators
{
    public class CppStructGenerator : StructGeneratorBase
    {
        private string fileHeader =
            $"/*{Environment.NewLine}" +
            $" * This file is part of the TrinityCore Project. See AUTHORS file for Copyright information{Environment.NewLine}" +
            $" *{Environment.NewLine}" +
            $" * This program is free software; you can redistribute it and/or modify it{Environment.NewLine}" +
            $" * under the terms of the GNU General Public License as published by the{Environment.NewLine}" +
            $" * Free Software Foundation; either version 2 of the License, or (at your{Environment.NewLine}" +
            $" * option) any later version.{Environment.NewLine}" +
            $" *{Environment.NewLine}" +
            $" * This program is distributed in the hope that it will be useful, but WITHOUT{Environment.NewLine}" +
            $" * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or{Environment.NewLine}" +
            $" * FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for{Environment.NewLine}" +
            $" * more details.{Environment.NewLine}" +
            $" *{Environment.NewLine}" +
            $" * You should have received a copy of the GNU General Public License along{Environment.NewLine}" +
            $" * with this program. If not, see <http://www.gnu.org/licenses/>.{Environment.NewLine}" +
            $" */{Environment.NewLine}" +
            $"{Environment.NewLine}" +
            $"#ifndef TRINITY_DB2STRUCTURE_H{Environment.NewLine}" +
            $"#define TRINITY_DB2STRUCTURE_H{Environment.NewLine}" +
            $"{Environment.NewLine}" +
            $"#include \"Common.h\"{Environment.NewLine}" +
            $"#include \"DBCEnums.h\"{Environment.NewLine}" +
            $"#include \"FlagsArray.h\"{Environment.NewLine}" +
            $"#include \"RaceMask.h\"{Environment.NewLine}" +
            $"{Environment.NewLine}" +
            $"#pragma pack(push, 1){Environment.NewLine}";

        private string fileFooter =
            $"#pragma pack(pop){Environment.NewLine}" +
            $"#endif";

        private List<HardcodedMethods> hardcodedMethods = [];

        public CppStructGenerator(ConcurrentDictionary<string /*DB2Name*/, Tuple<Structs.DBDefinition, Structs.VersionDefinitions>> dbddefinitions, int expectedBuildNumber) :
            base(dbddefinitions, expectedBuildNumber)
        {
            hardcodedMethods.Add(new AreaTableMethods());
        }

        public override void GenerateStructs()
        {
            using StreamWriter writer = new("DB2Structure.h");

            writer.WriteLine(fileHeader);

            foreach (KeyValuePair<string, Tuple<Structs.DBDefinition, Structs.VersionDefinitions>> pair in definitions)
            {
                writer.WriteLine($"// structure for {pair.Key}.db2");
                writer.WriteLine($"struct {pair.Key.Replace("_", "")}Entry");
                writer.WriteLine("{");

                FieldValue[] fields = GenerateFields(pair.Value.Item1, pair.Value.Item2);
                foreach (FieldValue field in fields)
                    writer.WriteLine($"{tabSpaces}{field.FieldType} {field.FieldName};");

                foreach (HardcodedMethods methods in hardcodedMethods)
                {
                    if (methods.StorageName == pair.Key)
                    {
                        writer.WriteLine("");
                        writer.WriteLine(tabSpaces + "// Methods");
                        foreach (string method in methods.Methods)
                            writer.WriteLine($"{tabSpaces}{method}");
                    }
                }

                writer.WriteLine("};");
                writer.WriteLine("");
            }

            writer.WriteLine(fileFooter);
        }

        public override FieldValue GenerateField(Structs.ColumnDefinition columnDefinition, Structs.Definition versionDefinition)
        {
            string fieldName = SanitizeFieldName(versionDefinition.name);
            string fieldType;
            switch (columnDefinition.type)
            {
                case "int":
                    fieldType = $"{(IsUnsignedField(versionDefinition) ? "u" : "")}int{versionDefinition.size}";
                    break;
                case "locstring":
                    fieldType = "LocalizedString";
                    break;
                case "float":
                    fieldType = "float";
                    break;
                case "string":
                    fieldType = "char const*";
                    break;
                default:
                    fieldType = "undefined";
                    break;
            }

            if (fieldName.Contains("RaceMask"))
                fieldType = $"Trinity::RaceMask<{fieldType}>";

            if (versionDefinition.arrLength > 0)
            {
                bool isDbcPosition = false;
                if (fieldType == "float" && (versionDefinition.arrLength == 2 || (versionDefinition.arrLength == 3)))
                {
                    if (fieldName.EndsWith("Offset") || fieldName.EndsWith("Position") || fieldName.EndsWith("Pos") || fieldName.StartsWith("Pos"))
                    {
                        fieldType = $"DBCPosition{versionDefinition.arrLength}D";
                        isDbcPosition = true;
                    }
                }

                if (!isDbcPosition)
                    fieldType = $"std::array<{fieldType}, {versionDefinition.arrLength}>";
            }

            return new FieldValue(fieldType, fieldName);
        }
    }
}
