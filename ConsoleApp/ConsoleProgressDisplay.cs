using CodeGenerator.Bl;
using System.Text.RegularExpressions;
using Utilities;

namespace CodeGenerator.ConsoleApp
{
    public class ConsoleProgressDisplay
    {

        public void SubscribeToProgress(ClsGenerator generator)
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
}