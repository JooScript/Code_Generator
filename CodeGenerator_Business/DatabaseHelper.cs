using Microsoft.Data.SqlClient;

namespace CodeGenerator_Business
{
    public class DatabaseHelper
    {
        public static List<string> GetTableNames()
        {
            List<string> tables = new List<string>();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString()))
            {
                connection.Open();

                string query = @"
                    SELECT TABLE_NAME 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_TYPE = 'BASE TABLE' 
                    AND TABLE_NAME NOT IN ('sysdiagrams', '__EFMigrationsHistory')";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string? tableName = reader["TABLE_NAME"].ToString();

                            if (tableName != null)
                            {
                                tables.Add(tableName);
                            }
                        }
                    }
                }
            }
            return tables;
        }
    }
}