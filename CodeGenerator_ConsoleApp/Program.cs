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
                clsConsole.ListConsolePrinting(tables);

                foreach (var table in tables)
                {
                    clsDalGenerator.GenerateDalCode(table);
                    //BlGenerator.GenerateBlCode(table);
                }

                Console.WriteLine("Code generation completed. Check the Generated Code folder.");
            }
            catch (Exception ex)
            {
                clsUtil.ErrorLogger(ex);
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        public static void PrintTables()
        {
            clsDatabase.Initialize(clsDataAccessSettings.ConnectionString());
            List<string> tables = clsDatabase.GetTableNames();

            if (tables.Count == 0)
            {
                Console.WriteLine("No tables found in the database.");
                return;
            }

            Console.WriteLine("Tables found in the database:");
            clsConsole.ListConsolePrinting(tables);
        }

        public static void PrintColumns(string tableName = "Addresses")
        {
            try
            {
                clsDatabase.Initialize(clsDataAccessSettings.ConnectionString());
                // Get the columns for the specified table
                var columns = clsDatabase.GetTableColumns(tableName);

                if (columns == null || columns.Count == 0)
                {
                    Console.WriteLine($"No columns found for table '{tableName}'");
                    return;
                }

                // Print table header
                Console.WriteLine($"\nColumns for table '{tableName}':");
                Console.WriteLine(new string('-', 90));
                Console.WriteLine("| {0,-20} | {1,-15} | {2,-8} | {3,-8} | {4,-8} | {5,-8} | {6,-5} | {7,-5} |", "Name", "DataType", "Nullable", "MaxLen", "Prec", "Scale", "PK", "FK");
                Console.WriteLine(new string('-', 90));

                // Print each column's details
                foreach (var column in columns)
                {
                    Console.WriteLine("| {0,-20} | {1,-15} | {2,-8} | {3,-8} | {4,-8} | {5,-8} | {6,-5} | {7,-5} |",
                        column.Name,
                        column.DataType,
                        column.IsNullable ? "YES" : "NO",
                        column.MaxLength?.ToString() ?? "N/A",
                        column.Precision?.ToString() ?? "N/A",
                        column.Scale?.ToString() ?? "N/A",
                        column.IsPrimaryKey ? "YES" : "NO",
                        column.IsForeignKey ? "YES" : "NO");
                }

                Console.WriteLine(new string('-', 90));

                // Print additional identity information
                var identityColumns = columns.Where(c => c.IsIdentity).ToList();
                if (identityColumns.Any())
                {
                    Console.WriteLine("\nIdentity Columns:");
                    foreach (var idCol in identityColumns)
                    {
                        Console.WriteLine($"- {idCol.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error printing columns for table '{tableName}': {ex.Message}");
            }
        }

        static void Main(string[] args)
        {
            GenerateCode();
        }
    }
}