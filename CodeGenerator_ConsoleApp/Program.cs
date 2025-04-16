using CodeGenerator_Business;
using Utilities;

namespace CodeGenerator_ConsoleApp
{
    internal class Program
    {
        public static void GenerateCode()
        {
            try
            {
                clsDatabase.Initialize(clsDataAccessSettings.ConnectionString());
                List<string> tables = clsDatabase.GetTableNames();

                if (tables.Count == 0)
                {
                    Console.WriteLine("No tables found in the database.");
                    return;
                }

                Console.WriteLine("Tables found in the database:");
                Console.WriteLine("-----------------------------");
                clsConsole.ListConsolePrinting(tables);

                foreach (var table in tables)
                {
                    clsDaGenerator.GenerateDalCode(table);
                    clsBlGenerator.GenerateBlCode(table);
                }

                Console.BackgroundColor = ConsoleColor.Green;
                Console.WriteLine("");
                Console.WriteLine("-------------------------------------------------------------");
                Console.WriteLine("|Code generation completed. Check the Generated Code folder.|");
                Console.WriteLine("-------------------------------------------------------------");
                Console.WriteLine("");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                clsUtil.ErrorLogger(ex);
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("");
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine("");
                Console.ResetColor();
            }
        }

        static void Main(string[] args)
        {
            GenerateCode();
        }
    }
}