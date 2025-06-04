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
                    ConsoleUtil.PrintColoredMessage("No tables found in the database.", ConsoleColor.Yellow);
                    return;
                }

                ConsoleUtil.PrintColoredMessage("Tables found in the database:", ConsoleColor.DarkCyan);
                ConsoleUtil.PrintColoredMessage(new string('═', 60), ConsoleColor.DarkCyan);
                ConsoleUtil.ListConsolePrinting(tables);
                Console.WriteLine("");

                short counter = 0;
                bool allSuccess = true;
                List<string> failedTables = new List<string>();

                foreach (string table in tables)
                {
                    counter++;
                    string formatedCounter = FormatUtil.FormatNumbers(counter, tables.Count);
                    string title = $"╔[{formatedCounter}] Generating Code For: {table}╗";
                    string hyphens = $"╚{new string('═', title.Length - 2)}╝";

                    Console.WriteLine();
                    Console.WriteLine();
                    ConsoleUtil.PrintColoredMessage(title, ConsoleColor.DarkCyan);
                    ConsoleUtil.PrintColoredMessage(hyphens, ConsoleColor.DarkCyan);
                    Console.WriteLine();

                    Console.Write($"- Creating Data Access Layer (DAL) for {table}... ");
                    bool dalSuccess = DataAccessGenerator.GenerateDalCode(table, codeStyle);
                    ConsoleUtil.PrintStatus(dalSuccess);

                    Console.Write($"- Creating Business Logic (BL) for {table}... ");
                    bool blSuccess = LogicGenerator.GenerateBlCode(table);
                    ConsoleUtil.PrintStatus(blSuccess);

                    Console.Write($"- Creating API Endpoints for {table}... ");
                    bool ilSuccess = APIGenerator.GenerateControllerCode(table);
                    ConsoleUtil.PrintStatus(ilSuccess);

                    if (!dalSuccess || !blSuccess || !ilSuccess)
                    {
                        failedTables.Add(table);
                        allSuccess = false;
                        string errorDetails =
                            (dalSuccess ? "" : "❌ DAL ") +
                            (blSuccess ? "" : "❌ BL ") +
                            (ilSuccess ? "" : "❌ API");

                        ConsoleUtil.PrintColoredMessage($"❌ Partial generation for table '{table}'. Failed: {errorDetails}", ConsoleColor.Red);
                    }
                    else
                    {
                        ConsoleUtil.PrintColoredMessage($"✓ Successfully generated all code for table '{table}'", ConsoleColor.Green);
                    }
                }

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();

                if (allSuccess)
                {
                    ConsoleUtil.PrintColoredMessage(
                        "╔══════════════════════════════════════════════════════════╗\n" +
                        "║ ✓ Code generation completed successfully for all tables! ║\n" +
                        "║     Check the Generated Code folder for results.         ║\n" +
                        "╚══════════════════════════════════════════════════════════╝",
                        ConsoleColor.Green
                    );
                }
                else
                {
                    ConsoleUtil.PrintColoredMessage(
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
                GeneralUtil.ErrorLogger(ex);
                ConsoleUtil.PrintColoredMessage(
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