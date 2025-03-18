

namespace CodeGenerator_ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
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
                foreach (var table in tables)
                {
                    Console.WriteLine(table);
                }

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
    }
}
