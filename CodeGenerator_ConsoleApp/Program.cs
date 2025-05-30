using CodeGenerator_Logic;
using Utilities;
using static CodeGenerator_Logic.Genrator;

namespace CodeGenerator_ConsoleApp
{
    internal class Program
    {
        public static void GenerateCode(enCodeStyle codeStyle = enCodeStyle.AdoStyle)
        {
            try
            {
                DatabaseUtil.Initialize(DataAccessSettings.ConnectionString());

                List<string> tables = DatabaseUtil.GetTableNames();

                if (tables == null || tables.Count == 0)
                {
                    Console.WriteLine("No tables found in the database.");
                    return;
                }

                Console.WriteLine("Tables found in the database:");
                Console.WriteLine("-----------------------------");
                ConsoleUtil.ListConsolePrinting(tables);

                foreach (string table in tables)
                {
                    bool dalSuccess = DataAccessGenerator.GenerateDalCode(table, codeStyle);
                    bool blSuccess = LogicGenerator.GenerateBlCode(table);
                    bool IlSuccess = APIGenerator.GenerateControllerCode(table);

                    if (!dalSuccess || !blSuccess || !IlSuccess)
                    {
                        throw new Exception(
                            $"Code generation failed for table '{table}'. " +
                            $"{(dalSuccess ? "" : "DAL generation failed. ")}" +
                            $"{(blSuccess ? "" : "BL generation failed.")}" +
                            $"{(IlSuccess ? "" : "IL generation failed.")}");
                    }
                }

                ConsoleUtil.PrintColoredMessage(
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
                GeneralUtil.ErrorLogger(ex);
                ConsoleUtil.PrintColoredMessage($"An error occurred: {ex.Message}", ConsoleColor.Red);
            }
        }

        static void Main(string[] args)
        {
            GenerateCode();
        }
    }
}