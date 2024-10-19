
using DBDefsLib;
using System.Collections.Concurrent;

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

        public CppStructGenerator(ConcurrentDictionary<string /*DB2Name*/, Tuple<Structs.DBDefinition, Structs.VersionDefinitions>> dbddefinitions, int expectedBuildNumber) :
            base(dbddefinitions, expectedBuildNumber)
        {
        }

        public override void GenerateStructs()
        {

            // An existing db2 structure header will be updated
            if (File.Exists("DB2Structure.h"))
            {
                // We will read through the structs and write the updated file at the same time
                using StreamReader reader = new("DB2Structure.h");
                using StreamWriter writer = new("DB2StructureUpdated.h");

                bool isInStruct = false;
                bool isInMethodSegment = false;
                string? line = reader.ReadLine();
                while (line != null)
                {


                    // This block here will start writing the structure and fields of the db2 entry. Once this is done, we scan the rest of the file for methods
                    if (line.StartsWith("struct"))
                    {
                        // Try to extract the db2 entry name from the struct
                        string[] structHeader = line.Split(' ');
                        if (structHeader.Length <= 1)
                        {
                            Console.Write($"DB2Structure.h line with content '{line}' could not be fully read. Skipped");
                            line = reader.ReadLine();
                            continue;
                        }

                        // Try to find a definition for the db2 entry
                        string db2EntryName = structHeader[1].Replace("_", "");
                        var foundDefinitions = definitions.Where(x =>
                        {
                            return $"{x.Key.Replace("_", "")}Entry" == db2EntryName;
                        });

                        if (foundDefinitions == null || !foundDefinitions.Any())
                        {
                            Console.Write($"A definition for '{db2EntryName}' could not be found. Perhaps it got removed in this build or the definitions are not up to date. Skipped");
                            line = reader.ReadLine();
                            continue;
                        }

                        // write the new structure
                        KeyValuePair<string, Tuple<Structs.DBDefinition, Structs.VersionDefinitions>> pair = foundDefinitions.First();
                        writer.WriteLine($"// structure for {pair.Key}.db2");
                        writer.WriteLine($"struct {pair.Key.Replace("_", "")}Entry");
                        writer.WriteLine("{");
                        FieldValue[] fields = GenerateFields(pair.Value.Item1, pair.Value.Item2);
                        foreach (FieldValue field in fields)
                            writer.WriteLine($"{tabSpaces}{field.FieldType} {field.FieldName};");

                        isInStruct = true;
                    }

                    // end of the struct has been reached. Commence with plain line copying
                    if (isInStruct && line.StartsWith("};"))
                    {
                        writer.WriteLine(line);
                        line = reader.ReadLine();
                        isInStruct = false;
                        isInMethodSegment = false;
                        continue;
                    }

                    if (isInStruct && !isInMethodSegment && line.Contains('(') && line.Contains("const"))
                    {
                        writer.WriteLine("");
                        isInMethodSegment = true;
                    }

                    if (!isInStruct || isInMethodSegment)
                        writer.WriteLine(line);

                    line = reader.ReadLine();
                }

            }
            else
            {
                // Otherwise we will generate a new one from scratch. This one will need manual fixups to fit into whatever methods and standards apply to the source
                using StreamWriter writer = new("DB2StructureGenerated.h");

                writer.WriteLine(fileHeader);

                foreach (KeyValuePair<string, Tuple<Structs.DBDefinition, Structs.VersionDefinitions>> pair in definitions)
                {
                    writer.WriteLine($"// structure for {pair.Key}.db2");
                    writer.WriteLine($"struct {pair.Key}Entry");
                    writer.WriteLine("{");

                    FieldValue[] fields = GenerateFields(pair.Value.Item1, pair.Value.Item2);
                    foreach (FieldValue field in fields)
                        writer.WriteLine($"{tabSpaces}{field.FieldType} {field.FieldName};");

                    /*
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
                    */

                    writer.WriteLine("};");
                    writer.WriteLine("");
                }

                writer.WriteLine(fileFooter);
            }
        }

        public override FieldValue GenerateField(Structs.ColumnDefinition columnDefinition, Structs.Definition versionDefinition)
        {
            string fieldName = SanitizeFieldName(versionDefinition.name);
            string fieldType;
            switch (columnDefinition.type)
            {
                case "int":
                    fieldType = $"{(IsUnsignedField(versionDefinition, true) ? "u" : "")}int{versionDefinition.size}";
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

            if (fieldName.EndsWith("RaceMask") || fieldName.EndsWith("AllowableRace"))
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
