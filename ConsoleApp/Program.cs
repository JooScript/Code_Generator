using CodeGenerator.Bl;
using Microsoft.SqlServer.Management.Smo;
using Utilities;

namespace CodeGenerator.ConsoleApp;

internal partial class Program
{
    public class CodeGenerationOptions
    {
        public ClsGenerator.enCodeStyle CodeStyle { get; set; } = ClsGenerator.enCodeStyle.EF;
        public string DaPath { get; set; } = null;
        public string LogicPath { get; set; } = null;
        public string BlInterfacePath { get; set; } = null;
        public string DaInterfacePath { get; set; } = null;
        public string DtoPath { get; set; } = null;
        public string ControllerPath { get; set; } = null;
    }

    public static void GenerateCode(CodeGenerationOptions options)
    {



        try
        {
            ClsGenerator.InitializeConnectionString(DASettings.ConnectionString());

            Console.WriteLine(ClsGenerator.GeneratationRequirements());
            List<string> Excluded = new List<string> { "__EFMigrationsHistory", "AspNetRoleClaims", "AspNetRoles", "AspNetUserClaims", "AspNetUserLogins", "AspNetUserRoles", "AspNetUsers", "AspNetUserTokens" };
            List<string> tables = new();



            bool allCondsSuccess = false;
            int tCounter = 0;
            while (!allCondsSuccess)
            {
                Console.WriteLine($"- Loading Schema... ");
                ClsGenerator.ClearSchemaCache();
                tables = ClsGenerator.DatabaseSchema.Keys.ToList().Except(Excluded).ToList();
                int condCounter = tables.Count;

                if (tables == null || condCounter == 0)
                {
                    ConsoleHelper.PrintColoredMessage("No tables found in the database.", ConsoleColor.Yellow);
                    return;
                }

                foreach (var table in tables)
                {
                    condCounter--;
                    tCounter++;
                    string condFormatedCounter = FormatHelper.FormatNumbers(tCounter, tables.Count);
                    Console.Write($"{condFormatedCounter}- Checking Conditions for {table}... ");
                    bool condSuccess = new ClsGenerator(table).CheckGeneratorConditions();
                    allCondsSuccess = condSuccess && condCounter == 0;
                    ConsoleHelper.PrintStatus(condSuccess);
                }
            }





            Console.WriteLine("");
            ConsoleHelper.PrintColoredMessage("Tables found in the database:", ConsoleColor.DarkCyan);
            ConsoleHelper.PrintColoredMessage(new string('═', 60), ConsoleColor.DarkCyan);
            ConsoleHelper.ListConsolePrinting(tables);
            Console.WriteLine("");

            short counter = 0;
            bool allSuccess = true;
            List<string> failedTables = new List<string>();
            ClearOlderCode(options);
            foreach (string table in tables)
            {
                string path = string.Empty;
                counter++;
                string formatedCounter = FormatHelper.FormatNumbers(counter, tables.Count);
                string title = $"╔[{formatedCounter}] Generating Code For: {table}╗";
                string hyphens = $"╚{new string('═', title.Length - 2)}╝";

                Console.WriteLine();
                Console.WriteLine();
                ConsoleHelper.PrintColoredMessage(title, ConsoleColor.DarkCyan);
                ConsoleHelper.PrintColoredMessage(hyphens, ConsoleColor.DarkCyan);
                Console.WriteLine();


                Console.Write($"- Creating Data Access Layer (DA) for {table}... ");
                bool daSuccess = new ClsDaGenerator(table).GenerateDalCode(options.CodeStyle, out path);

                if (Helper.CreateFolderIfDoesNotExist(options.DaPath))
                {
                    FileHelper.CopyFileToFolder(options.DaPath, ref path, false, true);
                }

                ConsoleHelper.PrintStatus(daSuccess);

                Console.Write($"- Creating Data Transfering Objects (DTO) for {table}... ");
                bool dtoSuccess = new ClsDtoGenerator(table).GenerateDTO(options.CodeStyle, out path);


                if (Helper.CreateFolderIfDoesNotExist(options.DtoPath))
                {
                    FileHelper.CopyFileToFolder(options.DtoPath, ref path, false, true);
                }

                ConsoleHelper.PrintStatus(daSuccess);

                Console.Write($"- Creating Business Logic (BL) for {table}... ");
                bool blSuccess = new ClsBlGenerator(table).GenerateBlCode(options.CodeStyle, out path);

                if (Helper.CreateFolderIfDoesNotExist(options.LogicPath))
                {
                    FileHelper.CopyFileToFolder(options.LogicPath, ref path, false, true);
                }

                ConsoleHelper.PrintStatus(blSuccess);

                Console.Write($"- Creating Business Interfaces (BLI) for {table}... ");
                bool BlInterfacesSuccess = new ClsInterfacesGenerator(table).GenerateBlInterfaceCode(out path);

                if (Helper.CreateFolderIfDoesNotExist(options.BlInterfacePath))
                {
                    FileHelper.CopyFileToFolder(options.BlInterfacePath, ref path, false, true);
                }

                ConsoleHelper.PrintStatus(BlInterfacesSuccess);


                Console.Write($"- Creating Data Interfaces (DAI) for {table}... ");
                bool DaInterfacesSuccess = new ClsInterfacesGenerator(table).GenerateDaInterfaceCode(out path);

                if (Helper.CreateFolderIfDoesNotExist(options.DaInterfacePath))
                {
                    FileHelper.CopyFileToFolder(options.DaInterfacePath, ref path, false, true);
                }

                ConsoleHelper.PrintStatus(DaInterfacesSuccess);


                Console.Write($"- Creating API Endpoints for {table}... ");
                bool endpointSuccess = new ClsAPIGenerator(table).GenerateControllerCode(options.CodeStyle, out path);

                if (Helper.CreateFolderIfDoesNotExist(options.ControllerPath))
                {
                    FileHelper.CopyFileToFolder(options.ControllerPath, ref path, false, true);
                }

                ConsoleHelper.PrintStatus(endpointSuccess);

                if (!daSuccess || !dtoSuccess || !blSuccess || !endpointSuccess || !allCondsSuccess || !BlInterfacesSuccess || !DaInterfacesSuccess)
                {
                    failedTables.Add(table);
                    allSuccess = false;
                    string errorDetails =
                        (allCondsSuccess ? "" : "❌ COND ") +
                        (daSuccess ? "" : "❌ DA ") +
                        (dtoSuccess ? "" : "❌ DTO ") +
                        (blSuccess ? "" : "❌ BL ") +
                        (BlInterfacesSuccess ? "" : "❌ BLI ") +
                        (DaInterfacesSuccess ? "" : "❌ DAI ") +
                        (endpointSuccess ? "" : "❌ API");

                    ConsoleHelper.PrintColoredMessage($"❌ Partial generation for table '{table}'. Failed: {errorDetails}", ConsoleColor.Red);
                }
                else
                {
                    ConsoleHelper.PrintColoredMessage($"✓ Successfully generated all code for table '{table}'", ConsoleColor.Green);
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

    public static bool ClearOlderCode(CodeGenerationOptions options) => Helper.DeleteFolder(ClsGenerator.StoringPath, true) && Helper.DeleteFolder(options.ControllerPath, true) && Helper.DeleteFolder(options.DaPath, true) && Helper.DeleteFolder(options.LogicPath, true) && Helper.DeleteFolder(options.BlInterfacePath, true) && Helper.DeleteFolder(options.DtoPath, true) && Helper.DeleteFolder(options.DaInterfacePath, true);

    static void Main(string[] args)
    {
        //Console.WriteLine("Welcome to the Code Generator!");

        //ClsGenerator generator = new ClsGenerator();
        //ConsoleProgressDisplay display = new ConsoleProgressDisplay();

        //display.SubscribeToProgress(generator);
        //generator.GenerateCode(ClsDAGenerator.enCodeStyle.EFStyle);

        var options = new CodeGenerationOptions
        {
            CodeStyle = ClsGenerator.enCodeStyle.EF,
            DaPath = "C:\\Users\\Yousef\\Documents\\GitHub\\E-Commerce\\E-Commerce_API\\DA\\DataAccess",
            LogicPath = "C:\\Users\\Yousef\\Documents\\GitHub\\E-Commerce\\E-Commerce_API\\BL\\Logic",
            BlInterfacePath = "C:\\Users\\Yousef\\Documents\\GitHub\\E-Commerce\\E-Commerce_API\\Domains\\ILogic",
            DaInterfacePath = "C:\\Users\\Yousef\\Documents\\GitHub\\E-Commerce\\E-Commerce_API\\Domains\\IData",
            DtoPath = "C:\\Users\\Yousef\\Documents\\GitHub\\E-Commerce\\E-Commerce_API\\WebAPI\\DTO",
            ControllerPath = "C:\\Users\\Yousef\\Documents\\GitHub\\E-Commerce\\E-Commerce_API\\WebAPI\\Controllers"
        };

        GenerateCode(options);

    }
}
