using CodeGenerator_Logic;
using System.Text.RegularExpressions;
using Utilities;

namespace CodeGenerator_ConsoleApp
{
    internal class Program
    {
        //public static void GenerateCode(ClsDataAccessGenerator.enCodeStyle codeStyle = ClsDataAccessGenerator.enCodeStyle.AdoStyle)
        //{
        //    try
        //    {
        //        DatabaseHelper.Initialize(ClsDataAccessSettings.ConnectionString());

        //        List<string> Excluded = new List<string> { "__EFMigrationsHistory", "AspNetRoleClaims", "AspNetRoles", "AspNetUserClaims", "AspNetUserLogins", "AspNetUserRoles", "AspNetUsers", "AspNetUserTokens" };
        //        List<string> tables = DatabaseHelper.GetTableNames().Except(Excluded).ToList();
        //        List<string> filteredTables = tables.Select(table => Regex.Replace(table, "^Tb(l)?", "")).ToList();

        //        if (tables == null || tables.Count == 0)
        //        {
        //            ConsoleHelper.PrintColoredMessage("No tables found in the database.", ConsoleColor.Yellow);
        //            return;
        //        }

        //        Console.WriteLine(ClsGenerator.GeneratationRequirements());
        //        Console.WriteLine("");
        //        ConsoleHelper.PrintColoredMessage("Tables found in the database:", ConsoleColor.DarkCyan);
        //        ConsoleHelper.PrintColoredMessage(new string('═', 60), ConsoleColor.DarkCyan);
        //        ConsoleHelper.ListConsolePrinting(filteredTables);
        //        Console.WriteLine("");

        //        short counter = 0;
        //        bool allSuccess = true;
        //        List<string> failedTables = new List<string>();

        //        foreach (string table in tables)
        //        {
        //            string displayedTN = Regex.Replace(table, "^Tbl?", "", RegexOptions.IgnoreCase);

        //            counter++;
        //            string formatedCounter = FormatHelper.FormatNumbers(counter, tables.Count);
        //            string title = $"╔[{formatedCounter}] Generating Code For: {displayedTN}╗";
        //            string hyphens = $"╚{new string('═', title.Length - 2)}╝";

        //            Console.WriteLine();
        //            Console.WriteLine();
        //            ConsoleHelper.PrintColoredMessage(title, ConsoleColor.DarkCyan);
        //            ConsoleHelper.PrintColoredMessage(hyphens, ConsoleColor.DarkCyan);
        //            Console.WriteLine();

        //            Console.Write($"- Checking Conditions for {displayedTN}... ");
        //            bool condSuccess = ClsGenerator.CheckGeneratorConditions(table);
        //            ConsoleHelper.PrintStatus(condSuccess);

        //            Console.Write($"- Creating Data Access Layer (DAL) for {displayedTN}... ");
        //            bool dalSuccess = ClsDataAccessGenerator.GenerateDalCode(table, codeStyle);
        //            ConsoleHelper.PrintStatus(dalSuccess);

        //            Console.Write($"- Creating Business Logic (BL) for {displayedTN}... ");
        //            bool blSuccess = ClsLogicGenerator.GenerateBlCode(table);
        //            ConsoleHelper.PrintStatus(blSuccess);

        //            Console.Write($"- Creating API Endpoints for {displayedTN}... ");
        //            bool endpointSuccess = ClsAPIGenerator.GenerateControllerCode(table);
        //            ConsoleHelper.PrintStatus(endpointSuccess);

        //            if (!dalSuccess || !blSuccess || !endpointSuccess || !condSuccess)
        //            {
        //                failedTables.Add(displayedTN);
        //                allSuccess = false;
        //                string errorDetails =
        //                    (condSuccess ? "" : "❌ COND ") +
        //                    (dalSuccess ? "" : "❌ DAL ") +
        //                    (blSuccess ? "" : "❌ BL ") +
        //                    (endpointSuccess ? "" : "❌ API");

        //                ConsoleHelper.PrintColoredMessage($"❌ Partial generation for table '{displayedTN}'. Failed: {errorDetails}", ConsoleColor.Red);
        //            }
        //            else
        //            {
        //                ConsoleHelper.PrintColoredMessage($"✓ Successfully generated all code for table '{displayedTN}'", ConsoleColor.Green);
        //            }
        //        }

        //        Console.WriteLine();
        //        Console.WriteLine();
        //        Console.WriteLine();

        //        if (allSuccess)
        //        {
        //            ConsoleHelper.PrintColoredMessage(
        //                "╔══════════════════════════════════════════════════════════╗\n" +
        //                "║ ✓ Code generation completed successfully for all tables! ║\n" +
        //                "║     Check the Generated Code folder for results.         ║\n" +
        //                "╚══════════════════════════════════════════════════════════╝",
        //                ConsoleColor.Green
        //            );
        //        }
        //        else
        //        {
        //            ConsoleHelper.PrintColoredMessage(
        //                $"╔══════════════════════════════════════════════════════════╗\n" +
        //                $"║ ❌ Code generation completed with {failedTables.Count} failures          ║\n" +
        //                $"║     Check the following tables: {string.Join(", ", failedTables)} ║\n" +
        //                $"╚══════════════════════════════════════════════════════════╝",
        //                ConsoleColor.Yellow
        //            );
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Helper.ErrorLogger(ex);
        //        ConsoleHelper.PrintColoredMessage(
        //            "╔══════════════════════════════════════════════════════════╗\n" +
        //            "║ ❌ CRITICAL ERROR: Code generation process failed!        ║\n" +
        //            $"║     Error: {ex.Message.PadRight(40)} ║\n" +
        //            "╚══════════════════════════════════════════════════════════╝",
        //            ConsoleColor.Red
        //        );
        //    }
        //}

        // First, define event args for our progress updates

        public class CodeGenerationEventArgs : EventArgs
        {
            public string TableName { get; set; }
            public string StepName { get; set; }
            public bool Success { get; set; }
            public string Message { get; set; }
            public int Current { get; set; }
            public int Total { get; set; }
        }

        public class CodeGenerationProgress
        {
            // Define our event
            public event EventHandler<CodeGenerationEventArgs> ProgressUpdated;

            public void GenerateCode(ClsDataAccessGenerator.enCodeStyle codeStyle = ClsDataAccessGenerator.enCodeStyle.AdoStyle)
            {
                try
                {
                    DatabaseHelper.Initialize(ClsDataAccessSettings.ConnectionString());

                    List<string> Excluded = new List<string> { "__EFMigrationsHistory", "AspNetRoleClaims", "AspNetRoles", "AspNetUserClaims", "AspNetUserLogins", "AspNetUserRoles", "AspNetUsers", "AspNetUserTokens" };
                    List<string> tables = DatabaseHelper.GetTableNames().Except(Excluded).ToList();
                    List<string> filteredTables = tables.Select(table => Regex.Replace(table, "^Tb(l)?", "")).ToList();

                    if (tables == null || tables.Count == 0)
                    {
                        OnProgressUpdated(new CodeGenerationEventArgs
                        {
                            Message = "No tables found in the database.",
                            Success = false
                        });
                        return;
                    }

                    // Show initial table list
                    OnProgressUpdated(new CodeGenerationEventArgs
                    {
                        Message = ClsGenerator.GeneratationRequirements(),
                        StepName = "Header"
                    });

                    OnProgressUpdated(new CodeGenerationEventArgs
                    {
                        Message = "Tables found in the database:",
                        StepName = "TableListHeader"
                    });

                    OnProgressUpdated(new CodeGenerationEventArgs
                    {
                        Message = new string('═', 60),
                        StepName = "TableListSeparator"
                    });

                    foreach (var table in filteredTables)
                    {
                        OnProgressUpdated(new CodeGenerationEventArgs
                        {
                            Message = table,
                            StepName = "TableListItem"
                        });
                    }

                    short counter = 0;
                    bool allSuccess = true;
                    List<string> failedTables = new List<string>();

                    foreach (string table in tables)
                    {
                        string displayedTN = Regex.Replace(table, "^Tbl?", "", RegexOptions.IgnoreCase);
                        counter++;
                        string formatedCounter = FormatHelper.FormatNumbers(counter, tables.Count);

                        // Table header
                        OnProgressUpdated(new CodeGenerationEventArgs
                        {
                            TableName = displayedTN,
                            Message = $"╔[{formatedCounter}] Generating Code For: {displayedTN}╗",
                            StepName = "TableHeader",
                            Current = counter,
                            Total = tables.Count
                        });

                        OnProgressUpdated(new CodeGenerationEventArgs
                        {
                            TableName = displayedTN,
                            Message = $"╚{new string('═', $"╔[{formatedCounter}] Generating Code For: {displayedTN}╗".Length - 2)}╝",
                            StepName = "TableHeaderUnderline",
                            Current = counter,
                            Total = tables.Count
                        });

                        // Check conditions
                        OnProgressUpdated(new CodeGenerationEventArgs
                        {
                            TableName = displayedTN,
                            Message = $"- Checking Conditions for {displayedTN}... ",
                            StepName = "ConditionCheckStart",
                            Current = counter,
                            Total = tables.Count
                        });

                        bool condSuccess = ClsGenerator.CheckGeneratorConditions(table);
                        OnProgressUpdated(new CodeGenerationEventArgs
                        {
                            TableName = displayedTN,
                            Success = condSuccess,
                            StepName = "ConditionCheckResult",
                            Current = counter,
                            Total = tables.Count
                        });

                        // Generate DAL
                        OnProgressUpdated(new CodeGenerationEventArgs
                        {
                            TableName = displayedTN,
                            Message = $"- Creating Data Access Layer (DAL) for {displayedTN}... ",
                            StepName = "DalGenerationStart",
                            Current = counter,
                            Total = tables.Count
                        });

                        bool dalSuccess = ClsDataAccessGenerator.GenerateDalCode(table, codeStyle);
                        OnProgressUpdated(new CodeGenerationEventArgs
                        {
                            TableName = displayedTN,
                            Success = dalSuccess,
                            StepName = "DalGenerationResult",
                            Current = counter,
                            Total = tables.Count
                        });

                        // Generate BL
                        OnProgressUpdated(new CodeGenerationEventArgs
                        {
                            TableName = displayedTN,
                            Message = $"- Creating Business Logic (BL) for {displayedTN}... ",
                            StepName = "BlGenerationStart",
                            Current = counter,
                            Total = tables.Count
                        });

                        bool blSuccess = ClsLogicGenerator.GenerateBlCode(table);
                        OnProgressUpdated(new CodeGenerationEventArgs
                        {
                            TableName = displayedTN,
                            Success = blSuccess,
                            StepName = "BlGenerationResult",
                            Current = counter,
                            Total = tables.Count
                        });

                        // Generate API
                        OnProgressUpdated(new CodeGenerationEventArgs
                        {
                            TableName = displayedTN,
                            Message = $"- Creating API Endpoints for {displayedTN}... ",
                            StepName = "ApiGenerationStart",
                            Current = counter,
                            Total = tables.Count
                        });

                        bool endpointSuccess = ClsAPIGenerator.GenerateControllerCode(table);
                        OnProgressUpdated(new CodeGenerationEventArgs
                        {
                            TableName = displayedTN,
                            Success = endpointSuccess,
                            StepName = "ApiGenerationResult",
                            Current = counter,
                            Total = tables.Count
                        });

                        if (!dalSuccess || !blSuccess || !endpointSuccess || !condSuccess)
                        {
                            failedTables.Add(displayedTN);
                            allSuccess = false;
                            string errorDetails =
                                (condSuccess ? "" : "❌ COND ") +
                                (dalSuccess ? "" : "❌ DAL ") +
                                (blSuccess ? "" : "❌ BL ") +
                                (endpointSuccess ? "" : "❌ API");

                            OnProgressUpdated(new CodeGenerationEventArgs
                            {
                                TableName = displayedTN,
                                Message = $"❌ Partial generation for table '{displayedTN}'. Failed: {errorDetails}",
                                Success = false,
                                StepName = "TableErrorSummary",
                                Current = counter,
                                Total = tables.Count
                            });
                        }
                        else
                        {
                            OnProgressUpdated(new CodeGenerationEventArgs
                            {
                                TableName = displayedTN,
                                Message = $"✓ Successfully generated all code for table '{displayedTN}'",
                                Success = true,
                                StepName = "TableSuccessSummary",
                                Current = counter,
                                Total = tables.Count
                            });
                        }
                    }

                    // Final summary
                    if (allSuccess)
                    {
                        OnProgressUpdated(new CodeGenerationEventArgs
                        {
                            Message = "╔══════════════════════════════════════════════════════════╗\n" +
                                     "║ ✓ Code generation completed successfully for all tables! ║\n" +
                                     "║     Check the Generated Code folder for results.         ║\n" +
                                     "╚══════════════════════════════════════════════════════════╝",
                            Success = true,
                            StepName = "FinalSuccess"
                        });
                    }
                    else
                    {
                        OnProgressUpdated(new CodeGenerationEventArgs
                        {
                            Message = $"╔══════════════════════════════════════════════════════════╗\n" +
                                     $"║ ❌ Code generation completed with {failedTables.Count} failures          ║\n" +
                                     $"║     Check the following tables: {string.Join(", ", failedTables)} ║\n" +
                                     $"╚══════════════════════════════════════════════════════════╝",
                            Success = false,
                            StepName = "FinalWithErrors"
                        });
                    }
                }
                catch (Exception ex)
                {
                    Helper.ErrorLogger(ex);
                    OnProgressUpdated(new CodeGenerationEventArgs
                    {
                        Message = "╔══════════════════════════════════════════════════════════╗\n" +
                                  "║ ❌ CRITICAL ERROR: Code generation process failed!        ║\n" +
                                  $"║     Error: {ex.Message.PadRight(40)} ║\n" +
                                  "╚══════════════════════════════════════════════════════════╝",
                        Success = false,
                        StepName = "CriticalError"
                    });
                }
            }

            protected virtual void OnProgressUpdated(CodeGenerationEventArgs e)
            {
                ProgressUpdated?.Invoke(this, e);
            }
        }

        public class ConsoleProgressDisplay
        {
            public void SubscribeToProgress(CodeGenerationProgress generator)
            {
                generator.ProgressUpdated += (sender, e) =>
                {
                    switch (e.StepName)
                    {
                        case "Header":
                            Console.WriteLine(e.Message);
                            Console.WriteLine();
                            break;

                        case "TableListHeader":
                            ConsoleHelper.PrintColoredMessage(e.Message, ConsoleColor.DarkCyan);
                            break;

                        case "TableListSeparator":
                            ConsoleHelper.PrintColoredMessage(e.Message, ConsoleColor.DarkCyan);
                            break;

                        case "TableListItem":
                            ConsoleHelper.ListConsolePrinting(new List<string> { e.Message });
                            break;

                        case "TableHeader":
                        case "TableHeaderUnderline":
                            Console.WriteLine();
                            Console.WriteLine();
                            ConsoleHelper.PrintColoredMessage(e.Message, ConsoleColor.DarkCyan);
                            break;

                        case "ConditionCheckStart":
                        case "DalGenerationStart":
                        case "BlGenerationStart":
                        case "ApiGenerationStart":
                            Console.Write(e.Message);
                            break;

                        case "ConditionCheckResult":
                        case "DalGenerationResult":
                        case "BlGenerationResult":
                        case "ApiGenerationResult":
                            ConsoleHelper.PrintStatus(e.Success);
                            break;

                        case "TableErrorSummary":
                            ConsoleHelper.PrintColoredMessage(e.Message, ConsoleColor.Red);
                            break;

                        case "TableSuccessSummary":
                            ConsoleHelper.PrintColoredMessage(e.Message, ConsoleColor.Green);
                            break;

                        case "FinalSuccess":
                            Console.WriteLine();
                            Console.WriteLine();
                            Console.WriteLine();
                            ConsoleHelper.PrintColoredMessage(e.Message, ConsoleColor.Green);
                            break;

                        case "FinalWithErrors":
                            Console.WriteLine();
                            Console.WriteLine();
                            Console.WriteLine();
                            ConsoleHelper.PrintColoredMessage(e.Message, ConsoleColor.Yellow);
                            break;

                        case "CriticalError":
                            Console.WriteLine();
                            Console.WriteLine();
                            Console.WriteLine();
                            ConsoleHelper.PrintColoredMessage(e.Message, ConsoleColor.Red);
                            break;
                    }
                };
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the Code Generator!");

            //var generator = new CodeGenerationProgress();
            //var display = new ConsoleProgressDisplay();

            //display.SubscribeToProgress(generator);
            //generator.GenerateCode(ClsDataAccessGenerator.enCodeStyle.AdoStyle);


            //GenerateCode();


            DatabaseHelper.Initialize(ClsDataAccessSettings.ConnectionString());
            var path = Path.Combine(FileHelper.GetPath(FileHelper.enSpecialFolderType.Desktop), "Databases");

            Console.WriteLine(DatabaseHelper.BackupDatabase(ref path));

        }
    }
}