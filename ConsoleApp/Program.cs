using CodeGenerator.Bl;
using System.Text.RegularExpressions;
using Utilities;
using static CodeGenerator.Bl.ClsGenerator;

namespace CodeGenerator.ConsoleApp;

internal partial class Program
{
    public static void GenerateCode(enCodeStyle codeStyle = enCodeStyle.EFStyle)
    {
        try
        {
            DatabaseHelper.Initialize(DASettings.ConnectionString());

            List<string> Excluded = new List<string> { "__EFMigrationsHistory", "AspNetRoleClaims", "AspNetRoles", "AspNetUserClaims", "AspNetUserLogins", "AspNetUserRoles", "AspNetUsers", "AspNetUserTokens" };
            List<string> tables = DatabaseHelper.GetTableNames().Except(Excluded).ToList();
            List<string> filteredTables = tables.Select(table => Regex.Replace(table, "^Tb(l)?", "")).ToList();

            if (tables == null || tables.Count == 0)
            {
                ConsoleHelper.PrintColoredMessage("No tables found in the database.", ConsoleColor.Yellow);
                return;
            }

            Console.WriteLine(ClsGenerator.GeneratationRequirements());
            Console.WriteLine("");
            ConsoleHelper.PrintColoredMessage("Tables found in the database:", ConsoleColor.DarkCyan);
            ConsoleHelper.PrintColoredMessage(new string('═', 60), ConsoleColor.DarkCyan);
            ConsoleHelper.ListConsolePrinting(filteredTables);
            Console.WriteLine("");

            short counter = 0;
            bool allSuccess = true;
            List<string> failedTables = new List<string>();

            foreach (string table in tables)
            {
                string displayedTN = Regex.Replace(table, "^Tbl?", "", RegexOptions.IgnoreCase);

                counter++;
                string formatedCounter = FormatHelper.FormatNumbers(counter, tables.Count);
                string title = $"╔[{formatedCounter}] Generating Code For: {displayedTN}╗";
                string hyphens = $"╚{new string('═', title.Length - 2)}╝";

                Console.WriteLine();
                Console.WriteLine();
                ConsoleHelper.PrintColoredMessage(title, ConsoleColor.DarkCyan);
                ConsoleHelper.PrintColoredMessage(hyphens, ConsoleColor.DarkCyan);
                Console.WriteLine();

                Console.Write($"- Checking Conditions for {displayedTN}... ");
                bool condSuccess = ClsGenerator.CheckGeneratorConditions(table);
                ConsoleHelper.PrintStatus(condSuccess);

                Console.Write($"- Creating Data Access Layer (DA) for {displayedTN}... ");
                bool daSuccess = ClsDaGenerator.GenerateDalCode(table, codeStyle);
                ConsoleHelper.PrintStatus(daSuccess);

                Console.Write($"- Creating Data Access Layer (DTO) for {displayedTN}... ");
                bool dtoSuccess = ClsDtoGenerator.GenerateDTO(table, codeStyle);
                ConsoleHelper.PrintStatus(daSuccess);

                Console.Write($"- Creating Business Logic (BL) for {displayedTN}... ");
                bool blSuccess = ClsBlGenerator.GenerateBlCode(table, codeStyle);
                ConsoleHelper.PrintStatus(blSuccess);

                Console.Write($"- Creating API Endpoints for {displayedTN}... ");
                bool endpointSuccess = ClsAPIGenerator.GenerateControllerCode(table, codeStyle);
                ConsoleHelper.PrintStatus(endpointSuccess);

                if (!daSuccess || !dtoSuccess || !blSuccess || !endpointSuccess || !condSuccess)
                {
                    failedTables.Add(displayedTN);
                    allSuccess = false;
                    string errorDetails =
                        (condSuccess ? "" : "❌ COND ") +
                        (daSuccess ? "" : "❌ DA ") +
                        (dtoSuccess ? "" : "❌ DTO ") +
                        (blSuccess ? "" : "❌ BL ") +
                        (endpointSuccess ? "" : "❌ API");

                    ConsoleHelper.PrintColoredMessage($"❌ Partial generation for table '{displayedTN}'. Failed: {errorDetails}", ConsoleColor.Red);
                }
                else
                {
                    ConsoleHelper.PrintColoredMessage($"✓ Successfully generated all code for table '{displayedTN}'", ConsoleColor.Green);
                }
            }

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            if (allSuccess)
            {
                ConsoleHelper.PrintColoredMessage(
                    "╔══════════════════════════════════════════════════════════╗\n" +
                    "║ ✓ Code generation completed successfully for all tables! ║\n" +
                    "║     Check the Generated Code folder for results.         ║\n" +
                    "╚══════════════════════════════════════════════════════════╝",
                    ConsoleColor.Green
                );
            }
            else
            {
                ConsoleHelper.PrintColoredMessage(
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
            Helper.ErrorLogger(ex);
            ConsoleHelper.PrintColoredMessage(
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
        //Console.WriteLine("Welcome to the Code Generator!");

        //ClsGenerator generator = new ClsGenerator();
        //ConsoleProgressDisplay display = new ConsoleProgressDisplay();

        //display.SubscribeToProgress(generator);
        //generator.GenerateCode(ClsDAGenerator.enCodeStyle.EFStyle);

        GenerateCode();

    }
}
