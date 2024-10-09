using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBDefsLib;

namespace DB2StructGenerator
{
    public class DBDStorage
    {
        public ConcurrentDictionary<string /*DB2Name*/, Tuple<Structs.DBDefinition, Structs.VersionDefinitions>> Definitions { get; private set; } = [];

        public void ReadDefinitions(int forBuildNumber)
        {
            Parallel.ForEach(Directory.GetFiles("definitions"), fileName =>
            {
                try
                {
                    DBDReader reader = new();
                    Structs.DBDefinition definition = reader.Read(File.OpenRead(fileName));

                    bool hasBuild = false;
                    foreach (Structs.VersionDefinitions versionDef in definition.versionDefinitions)
                    {
                        foreach (Build build in versionDef.builds)
                        {
                            if (build.build == forBuildNumber)
                            {
                                Definitions.TryAdd(Path.GetFileNameWithoutExtension(fileName), new Tuple<Structs.DBDefinition, Structs.VersionDefinitions>(definition, versionDef));
                                Console.WriteLine($"Found {Path.GetFileNameWithoutExtension(fileName)}.db2 definition for build {forBuildNumber}");
                                hasBuild = true;
                                break;
                            }
                        }

                        if (hasBuild)
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });

            Console.WriteLine($"\nSuccessfully loaded {Definitions.Count} definitions for build {forBuildNumber}\n");
        }
    }
}
