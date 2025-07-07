using System.Text.RegularExpressions;
using Utilities;

namespace CodeGenerator.Bl
{
    public class ClsGenerator
    {
        public enum enCodeStyle
        {
            EFStyle = 0,
            AdoStyle = 1
        }

        public ClsGenerator()
        {
            DatabaseHelper.Initialize(DASettings.ConnectionString());
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
                return FormatId(DatabaseHelper.GetFirstPrimaryKey(TableName));
            }
        }

        protected static List<DatabaseHelper.ForeignKeyInfo> foreignKeys
        {
            get { return DatabaseHelper.GetForeignKeys(TableName); }
        }

        public static string ModelName
        {
            get
            {
                if (string.IsNullOrEmpty(TableName))
                {
                    return null;
                }

                string tableName = FormatHelper.Singularize(WithoutPrefixTN);

                if (TableName.StartsWith("Tbl", StringComparison.OrdinalIgnoreCase))
                {
                    tableName = $"Tbl{tableName}";
                }

                if (TableName.StartsWith("Tb", StringComparison.OrdinalIgnoreCase))
                {
                    tableName = $"Tb{tableName}";
                }

                return tableName;
            }
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

        protected static string DtoClsName
        {
            get
            {
                if (string.IsNullOrEmpty(TableName))
                {
                    return null;
                }

                return $"{FormattedTNSingle}DTO";
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

                return $"{FormattedTNSingle}Data";
            }
        }

        protected static string DataObjName
        {
            get
            {
                return $"o{DataClsName}";
            }
        }

        protected static string LogicObjName
        {
            get
            {
                return $"o{FormattedTNSingle}";
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
                return DASettings.AppName();
            }
        }

        protected static string ContextName
        {
            get
            {
                return $"{AppName}Context";
            }
        }

        protected static List<DatabaseHelper.ColumnInfo> Columns
        {
            get
            {
                return DatabaseHelper.GetTableColumns(TableName);
            }
        }

        public static string StoringPath
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

        public static string FormatId(string? input, bool smallD = true)
        {
            if (input == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            if (input.EndsWith("id", StringComparison.OrdinalIgnoreCase) && input.Length >= 2)
            {
                char[] chars = input.ToCharArray();

                chars[^2] = 'I';
                chars[^1] = smallD ? 'd' : 'D';

                return new string(chars);
            }

            return input;
        }

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
      - May begin with 'Tb' or 'Tbl' only
      - Descriptive names without prefixes are equally valid
      - Names should be in PascalCase
      - Names Should Start with Capital Characters
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

        #region Perform Code Generation

        public class CodeGenerationEventArgs : EventArgs
        {
            public string TableName { get; set; }
            public string StepName { get; set; }
            public bool Success { get; set; }
            public string Message { get; set; }
            public int Current { get; set; }
            public int Total { get; set; }
        }

        public event EventHandler<CodeGenerationEventArgs> ProgressUpdated;

        public void GenerateCode(enCodeStyle codeStyle = enCodeStyle.EFStyle)
        {
            try
            {
                DatabaseHelper.Initialize(DASettings.ConnectionString());

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
                        Message = $"- Creating Data Access Layer (DA) for {displayedTN}... ",
                        StepName = "DaGenerationStart",
                        Current = counter,
                        Total = tables.Count
                    });

                    bool daSuccess = ClsDaGenerator.GenerateDalCode(table, codeStyle);
                    OnProgressUpdated(new CodeGenerationEventArgs
                    {
                        TableName = displayedTN,
                        Success = daSuccess,
                        StepName = "DaGenerationResult",
                        Current = counter,
                        Total = tables.Count
                    });

                    // Generate Dto
                    OnProgressUpdated(new CodeGenerationEventArgs
                    {
                        TableName = displayedTN,
                        Message = $"- Creating Data Access Layer (DTO) for {displayedTN}... ",
                        StepName = "DTOGenerationStart",
                        Current = counter,
                        Total = tables.Count
                    });

                    bool dtoSuccess = ClsDaGenerator.GenerateDalCode(table, codeStyle);
                    OnProgressUpdated(new CodeGenerationEventArgs
                    {
                        TableName = displayedTN,
                        Success = daSuccess,
                        StepName = "DTOGenerationResult",
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

                    bool blSuccess = ClsBlGenerator.GenerateBlCode(table, codeStyle);
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

                    bool endpointSuccess = ClsAPIGenerator.GenerateControllerCode(table, codeStyle);
                    OnProgressUpdated(new CodeGenerationEventArgs
                    {
                        TableName = displayedTN,
                        Success = endpointSuccess,
                        StepName = "ApiGenerationResult",
                        Current = counter,
                        Total = tables.Count
                    });

                    if (!daSuccess || !blSuccess || !endpointSuccess || !condSuccess)
                    {
                        failedTables.Add(displayedTN);
                        allSuccess = false;
                        string errorDetails =
                            (condSuccess ? "" : "❌ COND ") +
                            (daSuccess ? "" : "❌ DAL ") +
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

        #endregion

    }
}