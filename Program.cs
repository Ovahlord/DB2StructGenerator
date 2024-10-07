using DB2StructGenerator.StructGenerators;

namespace DB2StructGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (!Directory.Exists("definitions") || Directory.GetFiles("definitions").Length == 0)
            {
                Console.WriteLine("The definitions folder does not exist or is empty. The programm cannot function this way.");
                Console.WriteLine("Please download the definitions from https://github.com/wowdev/WoWDBDefs and place the 'definitions' folder into this application's directory.");
                printExitPrompt();
                return;
            }

            Console.WriteLine("Please insert build number (e.g. 56713)");

            int buildNumber = 0;
            while (buildNumber == 0)
            {
                if (!int.TryParse(Console.ReadLine(), out int inputBuildNumber) || inputBuildNumber == 0)
                {
                    Console.WriteLine("Could not read provided build input. Please try again.");
                    continue;
                }

                buildNumber = inputBuildNumber;
            }

            DBDStorage storage = new();
            storage.ReadDefinitions(buildNumber);

            if (storage.Definitions.Count == 0)
            {
                Console.WriteLine("Definitions have been read but no valid data has been extracted. Please make sure the definitions are up to date or update the DBDefsLib if the format should have changed.");
                printExitPrompt();
                return;
            }

            CppStructGenerator cppStructGenerator = new(storage.Definitions, buildNumber);
            cppStructGenerator.GenerateStructs();

            CsStructGenerator csStruct = new(storage.Definitions, buildNumber);
            csStruct.GenerateStructs();

            Console.WriteLine($"All done. C++ and C# structs have been generated.");
            printExitPrompt();
        }

        private static void printExitPrompt()
        {
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }
    }
}
