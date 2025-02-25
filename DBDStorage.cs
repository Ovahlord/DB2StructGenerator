using System.Collections.Concurrent;
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

                    foreach (Structs.VersionDefinitions versionDef in definition.versionDefinitions.AsSpan())
                    {
                        if (versionDef.builds.Any(b => b.build == forBuildNumber) || versionDef.buildRanges.Any(br => (forBuildNumber >= br.minBuild.build || forBuildNumber <= br.maxBuild.build)))
                        {
                            if (Definitions.TryAdd(Path.GetFileNameWithoutExtension(fileName), new Tuple<Structs.DBDefinition, Structs.VersionDefinitions>(definition, versionDef)))
                            {
                                Console.WriteLine($"Found {Path.GetFileNameWithoutExtension(fileName)}.db2 definition for build {forBuildNumber}");
                                break;
                            }
                        }
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
