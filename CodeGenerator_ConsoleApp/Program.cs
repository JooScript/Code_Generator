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
                List<string> tables = DatabaseHelper.GetTableNames();

                if (tables.Count == 0)
                {
                    Console.WriteLine("No tables found in the database.");
                    return;
                }

                Console.WriteLine("Tables found in the database:");
                clsConsoleUtil.ListConsolePrinting(tables);

                foreach (var table in tables)
                {
                    DalGenerator.GenerateDalCode(table);
                    BlGenerator.GenerateBlCode(table);
                }

                Console.WriteLine("Code generation completed. Check the GeneratedCode folder.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        static void Main(string[] args)
        {

        }
    }
}