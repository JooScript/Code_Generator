using Utilities;

namespace CodeGenerator_Logic
{
    public class ClsGenerator
    {
        public ClsGenerator()
        {
            DatabaseHelper.Initialize(ClsDataAccessSettings.ConnectionString());
        }

        #region Properties

        public static string TableName
        {
            get; set;
        }

        protected static string TableId
        {
            get
            {
                return ClsGlobal.FormatId(DatabaseHelper.GetFirstPrimaryKey(TableName));
            }
        }

        protected static List<DatabaseHelper.ForeignKeyInfo> foreignKeys
        {
            get { return DatabaseHelper.GetForeignKeys(TableName); }
        }

        public static string WithoutPrefixTN
        {
            get
            {
                if (string.IsNullOrEmpty(TableName))
                {
                    return null;
                }

                string tableName = TableName;

                if (tableName.StartsWith("Tbl", StringComparison.OrdinalIgnoreCase))
                {
                    tableName = tableName.Substring(3);
                }

                if (tableName.StartsWith("Tb", StringComparison.OrdinalIgnoreCase))
                {
                    tableName = tableName.Substring(2);
                }

                return tableName;
            }
        }

        protected static string LogicClsName
        {
            get
            {
                if (string.IsNullOrEmpty(TableName))
                {
                    return null;
                }

                return $"Cls{FormattedTNSingle}";
            }
        }

        protected static string LogicInterfaceName
        {
            get
            {
                if (string.IsNullOrEmpty(TableName))
                {
                    return null;
                }

                return $"I{FormattedTNSingle}";
            }
        }

        protected static string DataClsName
        {
            get
            {
                if (string.IsNullOrEmpty(TableName))
                {
                    return null;
                }

                return $"Cls{FormattedTNSingle}Data";
            }
        }

        protected static string FormattedTNSingle
        {
            get
            {
                return FormatHelper.CapitalizeFirstChars(FormatHelper.Singularize(WithoutPrefixTN) ?? string.Empty);
            }
        }

        protected static string FormattedTNSingleVar
        {
            get
            {
                return FormatHelper.SmalizeFirstChar(FormattedTNSingle);
            }
        }

        protected static string FormattedTNPluralize
        {
            get
            {
                return FormatHelper.CapitalizeFirstChars(FormatHelper.Pluralize(WithoutPrefixTN) ?? string.Empty);
            }
        }

        protected static string FormattedTNPluralizeVar
        {
            get
            {
                return FormatHelper.SmalizeFirstChar(FormattedTNPluralize);
            }
        }

        protected static string AppName
        {
            get
            {
                return ClsDataAccessSettings.AppName();
            }
        }

        protected static List<DatabaseHelper.ColumnInfo> Columns
        {
            get
            {
                return DatabaseHelper.GetTableColumns(TableName);
            }
        }

        public static string BasicPath
        {
            get
            {
                string desktopPath = FileHelper.GetPath(FileHelper.enSpecialFolderType.Desktop);
                string fullPath = Path.Combine(desktopPath, "Code Generator", AppName);

                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }

                return fullPath;
            }
        }

        #endregion

        /// <summary>
        /// Validates if a database table meets the necessary conditions for code generation.
        /// The table must exist, have columns, contain exactly one primary key that is an identity column of type int or bigint.
        /// </summary>
        /// <param name="tableName">Name of the database table to validate</param>
        /// <returns>
        /// True if the table meets all generation conditions:
        /// - Table exists
        /// - Table has columns
        /// - Table has exactly one primary key
        /// - Primary key is an identity column
        /// - Primary key is of type int or bigint
        /// Returns false and logs appropriate error messages if any condition fails.
        /// </returns>
        public static bool CheckGeneratorConditions(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                Helper.ErrorLogger(new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName)));
                return false;
            }

            TableName = tableName;

            try
            {
                if (!DatabaseHelper.TableExists(TableName))
                {
                    Helper.ErrorLogger(new Exception($"Table '{TableName}' does not exist in the database."));
                    return false;
                }

                var columns = DatabaseHelper.GetTableColumns(TableName);
                if (columns == null || columns.Count == 0)
                {
                    Helper.ErrorLogger(new Exception($"Table '{TableName}' has no columns."));
                    return false;
                }

                List<string> primaryKeys = DatabaseHelper.GetPrimaryKeys(TableName);
                if (primaryKeys.Count != 1)
                {
                    Helper.ErrorLogger(new Exception($"Table '{TableName}' must have exactly one primary key to generate code. Found {primaryKeys.Count}."));
                    return false;
                }

                string primaryKey = primaryKeys[0];
                var primaryKeyColumn = columns.FirstOrDefault(col => col.Name == primaryKey);

                if (primaryKeyColumn == null)
                {
                    Helper.ErrorLogger(new Exception($"Primary key '{primaryKey}' not found in table columns for table '{TableName}'."));
                    return false;
                }

                if (!primaryKeyColumn.IsIdentity)
                {
                    Helper.ErrorLogger(new Exception($"Primary key '{primaryKey}' in table '{TableName}' must be an identity column to generate code."));
                    return false;
                }

                if (primaryKeyColumn.DataType != "int" && primaryKeyColumn.DataType != "bigint")
                {
                    Helper.ErrorLogger(new Exception($"Primary key '{primaryKey}' in table '{TableName}' must be of type 'int' or 'bigint' to generate code. Found '{primaryKeyColumn.DataType}'."));
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Helper.ErrorLogger(new Exception($"Error while validating table '{TableName}' for code generation: {ex.Message}", ex));
                return false;
            }
        }

        public static string GeneratationRequirements()
        {
            return
@"Database Table Processing Requirements
 ======================================
 
 1. Table Naming Conventions
 ---------------------------
    • Prefixes are optional:
      - May begin with 'Tb' or 'Tbl'
      - Descriptive names without prefixes are equally valid
      - Names should be in PascalCase
      - Names should be Pluralized (e.g., 'Users', 'Orders')
    • Avoid using special characters or spaces
    • Names should be clear and descriptive of the table's purpose
 
 2. Table Structure Requirements
 -------------------------------
    • Existence: Table must exist in the target database
    • Columns: Must contain at least one defined column
    • Schema: Must belong to a valid database schema
 
 3. Primary Key Specifications
 -----------------------------
    • Quantity: Exactly one primary key must be defined
    • Identity: Must be configured as an IDENTITY column
    • Data Type: Must be either INT or BIGINT
    • Constraints: Should be NOT NULL
 
 4. Recommendations
 ------------------
    • Use consistent naming conventions throughout the database
    • Consider future scalability when choosing between INT and BIGINT
    • Document table purposes in database documentation
 ";
        }

        public static bool GenerateAllLayers(string tableName, ClsDataAccessGenerator.enCodeStyle codeStyle = ClsDataAccessGenerator.enCodeStyle.AdoStyle) => CheckGeneratorConditions(tableName) && ClsDataAccessGenerator.GenerateDalCode(tableName, codeStyle) && ClsLogicGenerator.GenerateBlCode(tableName) && ClsAPIGenerator.GenerateControllerCode(tableName);


    }
}




