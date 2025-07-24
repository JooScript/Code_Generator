using Microsoft.SqlServer.Management.Smo;
using System;
using System.Text.RegularExpressions;
using Utilities;

namespace CodeGenerator.Bl
{
    public class ClsGenerator
    {
        public ClsGenerator(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));
            }

            InitializeConnectionString(DASettings.connStr);
            TableName = tableName;
        }

        public static void InitializeConnectionString(string connectionString) => DatabaseHelper.Initialize(connectionString);


        public static void ClearSchemaCache() => DatabaseHelper.ClearSchemaCache();


        public enum enCodeStyle
        {
            EF = 0,
            Ado = 1
        }

        #region Properties

        private static Regex _namingRegex => new Regex("_([A-Za-z])", RegexOptions.Compiled);

        public static IReadOnlyDictionary<string, DatabaseHelper.TableSchema> DatabaseSchema => DatabaseHelper.GetDatabaseSchema(true);

        protected static string TableName { get; set; }

        public static DatabaseHelper.TableSchema CurrentTableSchema => DatabaseSchema[TableName];

        public static DatabaseHelper.ColumnInfo PrimaryKeyCol => CurrentTableSchema.Columns.FirstOrDefault(x => x.IsPrimaryKey);

        protected static string TableId => FormatId(PrimaryKeyCol.Name);

        protected static string TableIdDT => Helper.GetCSharpType(PrimaryKeyCol.DataType);

        protected static string FormattedTableId
        {
            get
            {
                return FormatHelper.CapitalizeFirstChars(_namingRegex.Replace(TableId, m => m.Groups[1].Value.ToUpper()));
            }
        }

        protected static List<DatabaseHelper.ForeignKeyInfo> foreignKeys => CurrentTableSchema.ForeignKeys;

        public static string[] Prefixes => new string[] { "Tbl_", "Tb_", "Tb" }.OrderByDescending(s => s.Length).ToArray();

        public static string ModelName
        {
            get
            {
                if (string.IsNullOrEmpty(TableName))
                {
                    return null;
                }

                string tableName = FormatHelper.Singularize(WithoutPrefixFormattedTN);

                foreach (string item in Prefixes)
                {
                    if (TableName.StartsWith(item, StringComparison.OrdinalIgnoreCase))
                    {
                        tableName = FormatHelper.CapitalizeFirstChars(_namingRegex.Replace($"{item}{tableName}", m => m.Groups[1].Value.ToUpper()));
                        break;
                    }
                }

                return tableName;
            }
        }

        public static string ModelNameProp => FormatHelper.Pluralize(ModelName);

        public static string WithoutPrefixFormattedTN
        {
            get
            {
                if (string.IsNullOrEmpty(TableName))
                {
                    return null;
                }

                string tableName = TableName;

                foreach (string item in Prefixes)
                {
                    if (tableName.StartsWith(item, StringComparison.OrdinalIgnoreCase))
                    {
                        tableName = FormatHelper.CapitalizeFirstChars(_namingRegex.Replace(tableName.Substring(item.Length), m => m.Groups[1].Value.ToUpper()));
                        break;
                    }
                }

                return tableName;
            }
        }

        protected static string LogicClsName => $"Cls{FormattedTNSingle}";

        protected static string DtoClsName => $"{FormattedTNSingle}DTO";

        protected static class MethodNames
        {
            public static string GetById => "GetById";
            public static string GetAll => "GetAll";
            public static string Add => "Add";
            public static string Update => "Update";
            public static string Save => "Save";
            public static string IsExists => "IsExists";
            public static string Count => "Count";
            public static string Delete => "Delete";
        }

        protected static IReadOnlyDictionary<string, string> RepoMethods => new Dictionary<string, string>
{
    { MethodNames.GetById, $"public Task<{ModelName}> {MethodNames.GetById}({TableIdDT} id);" },
    { MethodNames.GetAll, $"public Task<List<{ModelName}>> {MethodNames.GetAll}(int pageNumber = 1, int pageSize = 50);" },
    { MethodNames.Add, $"public Task<{TableIdDT}> {MethodNames.Add}({ModelName} {FormattedTNSingleVar});" },
    { MethodNames.Update, $"public Task<bool> {MethodNames.Update}({ModelName} {FormattedTNSingleVar});" },
    { MethodNames.Save, $"public Task<bool> {MethodNames.Save}({ModelName} {FormattedTNSingleVar});" },
    { MethodNames.IsExists, $"public Task<bool> {MethodNames.IsExists}({TableIdDT} id);" },
    { MethodNames.Count, $"public Task<{TableIdDT}> {MethodNames.Count}();" },
    { MethodNames.Delete, $"public Task<bool> {MethodNames.Delete}({TableIdDT} id);" }
};

        protected static string ImplementationMethod(string repoMethodName) => RepoMethods.TryGetValue(repoMethodName, out var method) ? method.Replace("public ", "public async ").Replace(";", "") : throw new KeyNotFoundException($"Method '{repoMethodName}' not found.");

        protected static string LogicInterfaceName => $"I{FormattedTNSingle}";

        protected static string DataInterfaceName => $"I{FormattedTNSingle}Data";

        protected static string DataClsName => $"{FormattedTNSingle}Data";

        protected static string DataObjName => $"o{DataClsName}";

        protected static string LogicObjName => $"o{FormattedTNSingle}";

        protected static string FormattedTNSingle => FormatHelper.CapitalizeFirstChars(FormatHelper.Singularize(WithoutPrefixFormattedTN) ?? string.Empty);

        protected static string FormattedTNSingleVar => FormatHelper.SmalizeFirstChar(FormattedTNSingle);

        protected static string FormattedTNPluralize => FormatHelper.CapitalizeFirstChars(FormatHelper.Pluralize(WithoutPrefixFormattedTN) ?? string.Empty);

        protected static string FormattedTNPluralizeVar => FormatHelper.SmalizeFirstChar(FormattedTNPluralize);

        protected static string AppName => _namingRegex.Replace(DASettings.AppName(), m => m.Groups[1].Value.ToUpper());

        protected static string ContextName => $"{AppName}Context";

        protected static List<DatabaseHelper.ColumnInfo> FormattedColumns => Columns
        .Select(item => new DatabaseHelper.ColumnInfo
        {
            Name = Regex.Replace(item.Name, "_([A-Za-z])", m => m.Groups[1].Value.ToUpper()),
            DataType = item.DataType,
            IsIdentity = item.IsIdentity,
            IsNullable = item.IsNullable,
            MaxLength = item.MaxLength,
            Precision = item.Precision,
            Scale = item.Scale
        }).ToList();

        protected static List<DatabaseHelper.ColumnInfo> Columns => DatabaseHelper.GetTableColumns(TableName);

        public static string MappingTxt => "Mapping.txt";

        public static string BlDiTxt => "Bl_DI.txt";

        public static string DaDiTxt => "Da_DI.txt";

        public static string StoringPath
        {
            get
            {
                string fullPath = Path.Combine(FileHelper.GetPath(FileHelper.enSpecialFolderType.Desktop), "Code Generator", AppName);

                Helper.CreateFolderIfDoesNotExist(fullPath);

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

        public bool CheckGeneratorConditions()
        {
            try
            {
                if (CurrentTableSchema == null)
                {
                    Helper.ErrorLogger(new Exception($"Table '{TableName}' does not exist in the database."));
                    return false;
                }

                if (!ValidationHelper.IsPlural(TableName))
                {
                    Helper.ErrorLogger(new Exception($"Table '{TableName}' not Plural."));
                    return false;
                }

                var columns = CurrentTableSchema.Columns;
                if (columns == null || columns.Count == 0)
                {
                    Helper.ErrorLogger(new Exception($"Table '{TableName}' has no columns."));
                    return false;
                }

                if (!columns.Any(item => item.Name == "IsDeleted"))
                {
                    Helper.ErrorLogger(new Exception($"Table '{TableName}' has no columns named IsDeleted for Soft Delete."));
                    return false;
                }

                List<string> primaryKeys = CurrentTableSchema.PrimaryKeys;
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

                if (primaryKeyColumn.Name != "Id")
                {
                    Helper.ErrorLogger(new Exception($"Primary key '{primaryKey}' in table '{TableName}' should be named Id. Found '{primaryKeyColumn.Name}'."));
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

        public static string GeneratationRequirements() =>

@$"Database Table Processing Requirements
 ======================================
 
 1. Table Naming Conventions
 ---------------------------
    • Prefixes are optional:
      - May begin with {string.Join(",", Prefixes)} only
      - Descriptive names without prefixes are equally valid
      - Names should be in PascalCase
      - Names Should Start with Capital Characters
      - Names should be Pluralized (e.g., 'Users', 'Orders')
    • Avoid using special characters or spaces
    • Names should be clear and descriptive of the table's purpose
 
 2. Table Structure Requirements
 -------------------------------
    • Soft Deletion: Table must have on column named IsDeleted
    • Existence: Table must exist in the target database
    • Columns: Must contain at least one defined column
    • Schema: Must belong to a valid database schema
 
 3. Primary Key Specifications
 -----------------------------
    • Quantity: Exactly one primary key must be defined
    • Identity: Must be configured as an IDENTITY column
    • Data Type: Must be either INT or BIGINT
    • Constraints: Should be NOT NULL
    • Name: Should be Id
 
 4. Recommendations
 ------------------
    • Use consistent naming conventions throughout the database
    • Consider future scalability when choosing between INT and BIGINT
    • Document table purposes in database documentation
 ";


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

        public void GenerateCode(enCodeStyle codeStyle = enCodeStyle.EF)
        {
            try
            {
                DatabaseHelper.Initialize(DASettings.ConnectionString());

                List<string> Excluded = new List<string> { "__EFMigrationsHistory", "AspNetRoleClaims", "AspNetRoles", "AspNetUserClaims", "AspNetUserLogins", "AspNetUserRoles", "AspNetUsers", "AspNetUserTokens" };
                List<string> tables = DatabaseHelper.GetTableNames().Except(Excluded).ToList();
                List<string> filteredTables = tables.Select(table => Regex.Replace(table, @"^(Tb(l)?_?|Tbl_?)", "", RegexOptions.IgnoreCase)).ToList();

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
                    string path = null;
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

                    bool condSuccess = new ClsGenerator(table).CheckGeneratorConditions();
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


                    bool daSuccess = new ClsDaGenerator(TableName).GenerateDalCode(codeStyle, out path);
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

                    bool dtoSuccess = new ClsDtoGenerator(TableName).GenerateDTO(codeStyle, out path);
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

                    bool blSuccess = new ClsBlGenerator(TableName).GenerateBlCode(codeStyle, out path);
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

                    bool endpointSuccess = new ClsAPIGenerator(TableName).GenerateControllerCode(codeStyle, out path);
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