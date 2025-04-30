using CodeGenerator_Logic;
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

                if (tables == null || tables.Count == 0)
                {
                    Console.WriteLine("No tables found in the database.");
                    return;
                }

                Console.WriteLine("Tables found in the database:");
                Console.WriteLine("-----------------------------");
                clsConsole.ListConsolePrinting(tables);

                foreach (string table in tables)
                {
                    bool dalSuccess = clsDaGenerator.GenerateDalCode(table);
                    bool blSuccess = clsBlGenerator.GenerateBlCode(table);

                    if (!dalSuccess || !blSuccess)
                    {
                        throw new Exception(
                            $"Code generation failed for table '{table}'. " +
                            $"{(dalSuccess ? "" : "DAL generation failed. ")}" +
                            $"{(blSuccess ? "" : "BL generation failed.")}");
                    }
                }

                clsConsole.PrintColoredMessage(
                    "---------------------------------------------------------------" +
                    Environment.NewLine +
                    "| Code generation completed. Check the Generated Code folder. |" +
                    Environment.NewLine +
                    "---------------------------------------------------------------",
                    ConsoleColor.Green
                );
            }
            catch (Exception ex)
            {
                clsUtil.ErrorLogger(ex);
                clsConsole.PrintColoredMessage($"An error occurred: {ex.Message}", ConsoleColor.Red);
            }
        }

        static void Main(string[] args)
        {
            GenerateCode();
        }
    }
}