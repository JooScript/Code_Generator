using CodeGenerator_Logic;
using Utilities;

namespace CodeGenerator_ConsoleApp
{
    internal class Program
    {
        public static void GenerateCode(ClsGenerator.enCodeStyle codeStyle = ClsGenerator.enCodeStyle.AdoStyle)
        {
            try
            {
                ClsDatabase.Initialize(ClsDataAccessSettings.ConnectionString());

                List<string> tables = ClsDatabase.GetTableNames();

                if (tables == null || tables.Count == 0)
                {
                    ClsConsole.PrintColoredMessage("No tables found in the database.", ConsoleColor.Yellow);
                    return;
                }

                ClsConsole.PrintColoredMessage("Tables found in the database:", ConsoleColor.DarkCyan);
                ClsConsole.PrintColoredMessage(new string('═', 60), ConsoleColor.DarkCyan);
                ClsConsole.ListConsolePrinting(tables);
                Console.WriteLine("");

                short counter = 0;
                bool allSuccess = true;
                List<string> failedTables = new List<string>();

                foreach (string table in tables)
                {
                    counter++;
                    string formatedCounter = ClsFormat.FormatNumbers(counter, tables.Count);
                    string title = $"╔[{formatedCounter}] Generating Code For: {table}╗";
                    string hyphens = $"╚{new string('═', title.Length - 2)}╝";

                    Console.WriteLine();
                    Console.WriteLine();
                    ClsConsole.PrintColoredMessage(title, ConsoleColor.DarkCyan);
                    ClsConsole.PrintColoredMessage(hyphens, ConsoleColor.DarkCyan);
                    Console.WriteLine();

                    Console.Write($"- Checking Conditions for {table}... ");
                    bool condSuccess = ClsGenerator.CheckGeneratorConditions(table);
                    ClsConsole.PrintStatus(condSuccess);

                    Console.Write($"- Creating Data Access Layer (DAL) for {table}... ");
                    bool dalSuccess = ClsDataAccessGenerator.GenerateDalCode(table, codeStyle);
                    ClsConsole.PrintStatus(dalSuccess);

                    Console.Write($"- Creating Business Logic (BL) for {table}... ");
                    bool blSuccess = ClsLogicGenerator.GenerateBlCode(table);
                    ClsConsole.PrintStatus(blSuccess);

                    Console.Write($"- Creating API Endpoints for {table}... ");
                    bool ilSuccess = ClsAPIGenerator.GenerateControllerCode(table);
                    ClsConsole.PrintStatus(ilSuccess);

                    if (!dalSuccess || !blSuccess || !ilSuccess || !condSuccess)
                    {
                        failedTables.Add(table);
                        allSuccess = false;
                        string errorDetails =
                            (condSuccess ? "" : "❌ COND ") +
                            (dalSuccess ? "" : "❌ DAL ") +
                            (blSuccess ? "" : "❌ BL ") +
                            (ilSuccess ? "" : "❌ API");

                        ClsConsole.PrintColoredMessage($"❌ Partial generation for table '{table}'. Failed: {errorDetails}", ConsoleColor.Red);
                    }
                    else
                    {
                        ClsConsole.PrintColoredMessage($"✓ Successfully generated all code for table '{table}'", ConsoleColor.Green);
                    }
                }

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();

                if (allSuccess)
                {
                    ClsConsole.PrintColoredMessage(
                        "╔══════════════════════════════════════════════════════════╗\n" +
                        "║ ✓ Code generation completed successfully for all tables! ║\n" +
                        "║     Check the Generated Code folder for results.         ║\n" +
                        "╚══════════════════════════════════════════════════════════╝",
                        ConsoleColor.Green
                    );
                }
                else
                {
                    ClsConsole.PrintColoredMessage(
                        $"╔══════════════════════════════════════════════════════════╗\n" +
                        $"║ ❌ Code generation completed with {failedTables.Count} failures          ║\n" +
                        $"║     Check the following tables: {string.Join(", ", failedTables)} ║\n" +
                        $"╚══════════════════════════════════════════════════════════╝",
                        ConsoleColor.Yellow
                    );
                }
            }
            catch (Exception ex)
            {
                ClsUtil.ErrorLogger(ex);
                ClsConsole.PrintColoredMessage(
                    "╔══════════════════════════════════════════════════════════╗\n" +
                    "║ ❌ CRITICAL ERROR: Code generation process failed!        ║\n" +
                    $"║     Error: {ex.Message.PadRight(40)} ║\n" +
                    "╚══════════════════════════════════════════════════════════╝",
                    ConsoleColor.Red
                );
            }
        }

        static void Main(string[] args)
        {
            GenerateCode();
        }
    }
}