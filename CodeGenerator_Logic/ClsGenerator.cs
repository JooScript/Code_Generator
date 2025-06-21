using Utilities;

namespace CodeGenerator_Logic
{
    public class ClsGenerator
    {

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
                ClsUtil.ErrorLogger(new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName)));
                return false;
            }

            _TableName = tableName;

            try
            {
                ClsDatabase.Initialize(ClsDataAccessSettings.ConnectionString());

                if (!ClsDatabase.TableExists(_TableName))
                {
                    ClsUtil.ErrorLogger(new Exception($"Table '{_TableName}' does not exist in the database."));
                    return false;
                }

                var columns = ClsDatabase.GetTableColumns(_TableName);
                if (columns == null || columns.Count == 0)
                {
                    ClsUtil.ErrorLogger(new Exception($"Table '{_TableName}' has no columns."));
                    return false;
                }

                List<string> primaryKeys = ClsDatabase.GetPrimaryKeys(_TableName);
                if (primaryKeys.Count != 1)
                {
                    ClsUtil.ErrorLogger(new Exception($"Table '{_TableName}' must have exactly one primary key to generate code. Found {primaryKeys.Count}."));
                    return false;
                }

                string primaryKey = primaryKeys[0];
                var primaryKeyColumn = columns.FirstOrDefault(col => col.Name == primaryKey);

                if (primaryKeyColumn == null)
                {
                    ClsUtil.ErrorLogger(new Exception($"Primary key '{primaryKey}' not found in table columns for table '{_TableName}'."));
                    return false;
                }

                if (!primaryKeyColumn.IsIdentity)
                {
                    ClsUtil.ErrorLogger(new Exception($"Primary key '{primaryKey}' in table '{_TableName}' must be an identity column to generate code."));
                    return false;
                }

                if (primaryKeyColumn.DataType != "int" && primaryKeyColumn.DataType != "bigint")
                {
                    ClsUtil.ErrorLogger(new Exception($"Primary key '{primaryKey}' in table '{_TableName}' must be of type 'int' or 'bigint' to generate code. Found '{primaryKeyColumn.DataType}'."));
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                ClsUtil.ErrorLogger(new Exception($"Error while validating table '{_TableName}' for code generation: {ex.Message}", ex));
                return false;
            }
        }

        public enum enCodeStyle
        {
            EFStyle = 0,
            AdoStyle = 1
        }

        protected static string _TableName;
        protected static string TableId;
        protected static List<ClsDatabase.ColumnInfo> columns;
        protected static List<ClsDatabase.ForeignKeyInfo> foreignKeys;
        protected static string AppName;

        #region Properties

        public static string TableName
        {
            get
            {
                return _TableName;
            }
            set
            {
                _TableName = value;
                ClsDatabase.Initialize(ClsDataAccessSettings.ConnectionString());
                columns = ClsDatabase.GetTableColumns(_TableName);
                TableId = ClsGlobal.FormatId(ClsDatabase.GetFirstPrimaryKey(_TableName));
                foreignKeys = ClsDatabase.GetForeignKeys(_TableName);
                AppName = ClsDataAccessSettings.AppName();
            }
        }

        protected static string LogicClsName
        {
            get
            {
                if (string.IsNullOrEmpty(_TableName))
                {
                    return null;
                }

                return $"Cls{FormattedTNSingle}";
            }
        }

        protected static string DataClsName
        {
            get
            {
                if (string.IsNullOrEmpty(_TableName))
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
                if (string.IsNullOrEmpty(_TableName))
                {
                    return null;
                }

                string singularized = ClsFormat.Singularize(_TableName) ?? string.Empty;
                return ClsFormat.CapitalizeFirstChars(singularized);
            }
        }

        protected static string FormattedTNPluralize
        {
            get
            {
                if (string.IsNullOrEmpty(_TableName))
                {
                    return null;
                }

                string pluralized = ClsFormat.Pluralize(_TableName) ?? string.Empty;
                return ClsFormat.CapitalizeFirstChars(pluralized);
            }
        }

        protected static string FormattedTNSingleVar
        {
            get
            {
                if (string.IsNullOrEmpty(_TableName))
                {
                    return null;
                }

                string singularized = ClsFormat.Singularize(_TableName) ?? string.Empty;
                string capitalized = ClsFormat.CapitalizeFirstChars(singularized);
                return ClsFormat.SmalizeFirstChar(capitalized);
            }
        }

        protected static string FormattedTNPluralizeVar
        {
            get
            {
                if (string.IsNullOrEmpty(_TableName))
                {
                    return null;
                }

                string pluralized = ClsFormat.Pluralize(_TableName) ?? string.Empty;
                string capitalized = ClsFormat.CapitalizeFirstChars(pluralized);
                return ClsFormat.SmalizeFirstChar(capitalized);
            }
        }

        #endregion

    }
}