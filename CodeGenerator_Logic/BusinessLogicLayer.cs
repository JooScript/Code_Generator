using System.Text;
using Utilities;

namespace CodeGenerator_Logic
{
    public static class clsBlGenerator
    {
        public enum enCodeStyle
        {
            EFStyle = 0,
            AdoStyle = 1
        }

        private static string _TableName;
        private static string _TableId;
        private static List<clsDatabase.ColumnInfo> _columns;
        private static string _AppName;

        #region Properties

        public static string TableName
        {
            set
            {
                _TableName = value;
                clsDatabase.Initialize(clsDataAccessSettings.ConnectionString());
                _columns = clsDatabase.GetTableColumns(_TableName);
                _TableId = clsGlobal.FormatId(clsDatabase.GetFirstPrimaryKey(_TableName));
                _AppName = clsDataAccessSettings.AppName();
            }
        }

        private static string _FormattedTNSingle
        {
            get
            {
                if (string.IsNullOrEmpty(_TableName))
                {
                    return null;
                }

                string singularized = clsFormat.Singularize(_TableName) ?? string.Empty;
                return clsFormat.CapitalizeFirstChars(singularized);
            }
        }

        private static string _FormattedTNPluralize
        {
            get
            {
                if (string.IsNullOrEmpty(_TableName))
                {
                    return null;
                }

                string pluralized = clsFormat.Pluralize(_TableName) ?? string.Empty;
                return clsFormat.CapitalizeFirstChars(pluralized);
            }
        }

        private static string _FormattedTNSingleVar
        {
            get
            {
                if (string.IsNullOrEmpty(_TableName))
                {
                    return null;
                }

                string singularized = clsFormat.Singularize(_TableName) ?? string.Empty;
                string capitalized = clsFormat.CapitalizeFirstChars(singularized);
                return clsFormat.SmalizeFirstChar(capitalized);
            }
        }

        private static string _FormattedTNPluralizeVar
        {
            get
            {
                if (string.IsNullOrEmpty(_TableName))
                {
                    return null;
                }

                string pluralized = clsFormat.Pluralize(_TableName) ?? string.Empty;
                string capitalized = clsFormat.CapitalizeFirstChars(pluralized);
                return clsFormat.SmalizeFirstChar(capitalized);
            }
        }

        #endregion

        #region EF Code

        #region EF Support Methods

        private static bool IsClassName(string columnName)
        {
            if (_TableName == null)
            {
                throw new InvalidOperationException("Table name not specified");
            }

            if (string.IsNullOrWhiteSpace(columnName))
            {
                return false;
            }

            string colName = clsFormat.Pluralize(columnName.Substring(0, columnName.Length - 2));
            foreach (string tableName in clsDatabase.GetTableNames())
            {
                if (tableName == colName)
                {
                    return true;
                }
            }

            return false;
        }

        private static string AddNewParameters()
        {
            var parameters = new List<string>();

            foreach (var column in _columns)
            {
                if (column.IsIdentity)
                    continue;

                string propertyName = clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name));
                parameters.Add($"this.{propertyName}");
            }

            return string.Join(", ", parameters);
        }

        private static string GetClassNameFromColumnName(string columnName)
        {
            if (IsClassName(columnName))
            {
                return columnName.Substring(0, columnName.Length - 2);
            }

            if (string.IsNullOrEmpty(columnName))
            {
                return string.Empty;
            }

            foreach (string tableName in clsDatabase.GetTableNames())
            {
                if (columnName.Contains(clsFormat.Singularize(tableName)))
                {
                    return clsFormat.Singularize(tableName);
                }
            }

            return string.Empty;
        }

        private static string UpdateParameters()
        {
            var parameters = new List<string>();

            foreach (var column in _columns)
            {
                string propertyName = clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name));
                parameters.Add($"this.{propertyName}");
            }

            return string.Join(", ", parameters);
        }

        private static string ConstructorParameters()
        {
            var parameters = new List<string>();

            foreach (var column in _columns)
            {
                string csharpType = clsUtil.ConvertDbTypeToCSharpType(column.DataType);
                string nullableSymbol = column.IsNullable ? "?" : "";
                string parameterName = clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name));

                parameters.Add($"{csharpType}{nullableSymbol} {parameterName}");
            }

            return string.Join(", ", parameters);
        }

        private static string ConstructorAssignments()
        {
            var sb = new StringBuilder();

            foreach (var column in _columns)
            {
                string propertyName = clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name));
                sb.AppendLine($"            this.{propertyName} = {propertyName};");
                if (column.IsForeignKey && !column.IsNullable)
                {
                    string colName = propertyName.Substring(0, propertyName.Length - 2);
                    string className = GetClassNameFromColumnName(propertyName);
                    if (column.IsNullable)
                    {
                        sb.AppendLine($"            this.{colName}Info = cls{className}.Find({propertyName}.Value);");
                    }
                    else
                    {
                        sb.AppendLine($"            this.{colName}Info = cls{className}.Find({propertyName});");
                    }
                }
            }

            return sb.ToString();
        }

        private static string DefaultValues()
        {
            var sb = new StringBuilder();

            foreach (var column in _columns)
            {
                string propertyName = clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name));
                string defaultValue = GetDefaultValueForType(column.DataType, column.IsNullable);

                sb.AppendLine($"            this.{propertyName} = {defaultValue};");
            }

            return sb.ToString();
        }

        private static string GetDefaultValueForType(string dbType, bool isNullable)
        {
            string csharpType = clsUtil.ConvertDbTypeToCSharpType(dbType);

            if (isNullable)
                return "null";

            switch (csharpType)
            {
                case "int":
                case "short":
                case "long":
                case "byte":
                    return "0";
                case "string":
                    return "string.Empty";
                case "bool":
                    return "false";
                case "DateTime":
                    return "DateTime.MinValue";
                case "decimal":
                case "double":
                case "float":
                    return "0";
                default:
                    return "null";
            }
        }

        private static string FindMethodTupleDeconstruction()
        {
            var sb = new StringBuilder();
            sb.Append("var (");

            bool first = true;
            foreach (var column in _columns)
            {
                if (!first)
                {
                    sb.Append(", ");
                }
                string propertyName = clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name));
                if (column.IsPrimaryKey)
                {
                    propertyName = propertyName.ToLower();
                }
                sb.Append(propertyName);
                first = false;
            }

            sb.Append(") = info.Value;");
            return sb.ToString();
        }

        private static string FindMethodReturnNew()
        {
            var sb = new StringBuilder();
            sb.Append($"return new cls{_FormattedTNSingle}(");

            bool first = true;
            foreach (var column in _columns)
            {
                if (!first)
                    sb.Append(", ");

                string propertyName = clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name));
                if (column.IsPrimaryKey)
                {
                    propertyName = propertyName.ToLower();
                }
                sb.Append(propertyName);
                first = false;
            }

            sb.Append(");");
            return sb.ToString();
        }

        private static string GenerateMappingFromDalEntity()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"                {_FormattedTNSingleVar} => new cls{_FormattedTNSingle}");
            sb.AppendLine("                {");

            foreach (var column in _columns)
            {
                string propertyName = clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name));
                string sourceProperty = column.IsNullable ? $"{_FormattedTNSingleVar}.{propertyName} ?? {GetDefaultValueForType(column.DataType, false)}"
                                                         : $"{_FormattedTNSingleVar}.{propertyName}";

                sb.AppendLine($"                    {propertyName} = {sourceProperty},");
            }

            sb.AppendLine("                }).ToList();");
            return sb.ToString();
        }

        #endregion

        #region EF Class Structure

        private static string TopUsing()
        {

            return $@"using {_AppName}_DataAccess.DataAccess;
using {_AppName}_DataAccess.Entities;
using Utilities;

namespace {_AppName}_Business.BusinessLogic
{{
    public partial class cls{_FormattedTNSingle}
    {{";
        }

        private static string ModeEnum()
        {
            return $@"        public enum enMode
        {{
            AddNew = 0,
            Update = 1
        }};

        public enMode Mode = enMode.AddNew;

";
        }

        private static string Properties()
        {
            var sb = new StringBuilder();

            foreach (var column in _columns)
            {
                string csharpType = clsUtil.ConvertDbTypeToCSharpType(column.DataType);
                string nullableSymbol = column.IsNullable ? "?" : "";
                string propertyName = clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name));

                sb.AppendLine($"        public {csharpType}{nullableSymbol} {propertyName}");
                sb.AppendLine("        {");
                sb.AppendLine("            get;");
                sb.AppendLine("            set;");
                sb.AppendLine("        }");
                sb.AppendLine();
                if (column.IsForeignKey && !column.IsNullable)
                {
                    string colName = propertyName.Substring(0, propertyName.Length - 2);
                    string className = GetClassNameFromColumnName(propertyName);
                    sb.AppendLine($"public cls{className} {colName}Info;");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        private static string DefaultConstructor()
        {
            return $@"        public cls{_FormattedTNSingle}()
        {{
{DefaultValues()}
            Mode = enMode.AddNew;
        }}

";
        }

        private static string ParameterizedConstructor()
        {
            return $@"        private cls{_FormattedTNSingle}({ConstructorParameters()})
        {{
{ConstructorAssignments()}
            Mode = enMode.Update;
        }}

";
        }

        private static string AddNewMethod()
        {
            string PasswordValidatation = "";
            if (clsFormat.Singularize(_TableName.ToLower()) == "user")
            {
                PasswordValidatation = @" if (!clsValidate.IsValidStrongPassword(this.Password))
                {
                    clsUtil.ErrorLogger(new Exception(""Password does not meet strength requirements""));
                    return false;
                }
";
            }

            return $@"private async Task<bool> _AddNew{_FormattedTNSingle}Async()
        {{
           {PasswordValidatation}
            try
            {{
                this.{_TableId} = await cls{_FormattedTNSingle}Data.AddNew{_FormattedTNSingle}Async({AddNewParameters()});
                return this.{_TableId} != -1;
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return false;
            }}
        }}

";
        }

        private static string UpdateMethod()
        {
            string PasswordValidatation = "";
            if (clsFormat.Singularize(_TableName.ToLower()) == "user")
            {
                PasswordValidatation = @" if (!clsValidate.IsValidStrongPassword(this.Password))
                {
                    clsUtil.ErrorLogger(new Exception(""Password does not meet strength requirements""));
                    return false;
                }
";
            }

            return $@"        private async Task<bool> _Update{_FormattedTNSingle}Async()
        {{
   {PasswordValidatation}
            try
            {{
                return await cls{_FormattedTNSingle}Data.Update{_FormattedTNSingle}Async({UpdateParameters()});
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return false;
            }}
        }}

";
        }

        private static string FindMethod()
        {
            return $@"        public static async Task<cls{_FormattedTNSingle}?> FindAsync(int {_TableId})
        {{
            if ({_TableId} <= 0)
            {{
                return null;
            }}

            try
            {{
                var info = await cls{_FormattedTNSingle}Data.Get{_FormattedTNSingle}InfoByIdAsync({_TableId});

                if (info == null)
                {{
                    return null;
                }}

                {FindMethodTupleDeconstruction()}
                {FindMethodReturnNew()}
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return null;
            }}
        }}

        public static cls{_FormattedTNSingle}? Find(int {_TableId})
        {{
            return FindAsync({_TableId}).GetAwaiter().GetResult();
        }}

";
        }

        private static string FindByPersonIDMethod()
        {
            return $@"        public static async Task<cls{_FormattedTNSingle}?> FindByPersonIDAsync(int PersonID)
        {{
            if (PersonID <= 0)
            {{
                return null;
            }}

            try
            {{
                var info = await cls{_FormattedTNSingle}Data.Get{_FormattedTNSingle}InfoByPersonIdAsync(PersonID);

                if (info == null)
                {{
                    return null;
                }}

                {FindMethodTupleDeconstruction()}
                {FindMethodReturnNew()}
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return null;
            }}
        }}

        public static cls{_FormattedTNSingle}? FindByPersonID(int PersonID)
        {{
            return FindByPersonIDAsync(PersonID).GetAwaiter().GetResult();
        }}

";
        }

        private static string FindByUsernameAndPasswordMethod()
        {
            return $@"        public static async Task<cls{_FormattedTNSingle}?> FindByUsernameAndPasswordAsync(string userName, string password)
        {{
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {{
                return null;
            }}

            try
            {{
                var info = await cls{_FormattedTNSingle}Data.Get{_FormattedTNSingle}InfoByUsernameAndPasswordAsync(userName, password);

                if (info == null)
                {{
                    return null;
                }}

                {FindMethodTupleDeconstruction()}
                {FindMethodReturnNew()}
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return null;
            }}
        }}

        public static cls{_FormattedTNSingle}? FindByUsernameAndPassword(string userName, string password)
        {{
            return FindByUsernameAndPasswordAsync(userName, password).GetAwaiter().GetResult();
        }}

";
        }

        private static string SaveMethod()
        {
            return $@"        public async Task<bool> SaveAsync()
        {{
            switch (Mode)
            {{
                case enMode.AddNew:
                    if (await _AddNew{_FormattedTNSingle}Async())
                    {{
                        Mode = enMode.Update;
                        return true;
                    }}
                    else
                    {{
                        return false;
                    }}
                case enMode.Update:
                    {{
                        return await _Update{_FormattedTNSingle}Async();
                    }}
            }}
            return false;
        }}

        public bool Save()
        {{
            return SaveAsync().GetAwaiter().GetResult();
        }}

";
        }

        private static string GetAllMethod()
        {
            return $@"        public static async Task<List<cls{_FormattedTNSingle}>> GetAll{_FormattedTNPluralize}Async(int pageNumber = 1, int pageSize = 50)
        {{
            try
            {{
                List<{_FormattedTNSingle}> {_FormattedTNPluralizeVar} = await cls{_FormattedTNSingle}Data.GetAll{_FormattedTNPluralize}Async(pageNumber, pageSize);

                List<cls{_FormattedTNSingle}> cls{_FormattedTNPluralize} = {_FormattedTNPluralizeVar}.Select(
{GenerateMappingFromDalEntity()}

                return cls{_FormattedTNPluralize};
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                throw;
            }}
        }}

        public static List<cls{_FormattedTNSingle}> GetAll{_FormattedTNPluralize}(int pageNumber = 1, int pageSize = 50)
        {{
            return GetAll{_FormattedTNPluralize}Async(pageNumber, pageSize).GetAwaiter().GetResult();
        }}

";
        }

        private static string DeleteMethod()
        {
            return $@"        public static async Task<bool> Delete{_FormattedTNSingle}Async(int {_TableId})
        {{
            return await cls{_FormattedTNSingle}Data.Delete{_FormattedTNSingle}Async({_TableId});
        }}

        public static bool Delete{_FormattedTNSingle}(int {_TableId})
        {{
            return Delete{_FormattedTNSingle}Async({_TableId}).GetAwaiter().GetResult();
        }}

";
        }

        private static string IsExistMethod()
        {
            return $@"        public static async Task<bool> Is{_FormattedTNSingle}ExistsAsync(int {_TableId})
        {{
            return await cls{_FormattedTNSingle}Data.Is{_FormattedTNSingle}ExistsAsync({_TableId});
        }}

        public static bool Is{_FormattedTNSingle}Exists(int {_TableId})
        {{
            return Is{_FormattedTNSingle}ExistsAsync({_TableId}).GetAwaiter().GetResult();
        }}

";
        }

        private static string IsExistByUsernameMethod()
        {
            return $@"        public static async Task<bool> Is{_FormattedTNSingle}ExistsAsync(string UserName)
        {{
            return await cls{_FormattedTNSingle}Data.Is{_FormattedTNSingle}ExistsAsync(UserName);
        }}

        public static bool Is{_FormattedTNSingle}Exists(string UserName)
        {{
            return Is{_FormattedTNSingle}ExistsAsync(UserName).GetAwaiter().GetResult();
        }}

";
        }

        private static string IsExistByPersonIdMethod()
        {
            return $@"        public static async Task<bool> Is{_FormattedTNSingle}ExistsByPersonIdAsync(int PersonId)
        {{
            return await cls{_FormattedTNSingle}Data.Is{_FormattedTNSingle}ExistsByPersonIdAsync(PersonId);
        }}

        public static bool Is{_FormattedTNSingle}ExistsByPersonId(int PersonId)
        {{
            return Is{_FormattedTNSingle}ExistsByPersonIdAsync(PersonId).GetAwaiter().GetResult();
        }}

";
        }

        private static string Closing()
        {
            return $@"    }}
}}";
        }

        #endregion

        public static bool GenerateEFBlCode(string tableName, string? folderPath = null)
        {
            if (tableName == null)
            {
                return false;
            }
            else
            {
                TableName = tableName;
            }

            if (string.IsNullOrEmpty(folderPath))
            {
                folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Code Generator\\{clsDataAccessSettings.AppName()}\\BusinessLogic\\Basic");
            }

            StringBuilder blCode = new StringBuilder();

            blCode.Append(TopUsing() + ModeEnum() + Properties() + DefaultConstructor() + ParameterizedConstructor() + AddNewMethod() + UpdateMethod() + FindMethod());

            if (clsFormat.Singularize(_TableName.ToLower()) == "user")
            {
                blCode.Append(FindByPersonIDMethod() + FindByUsernameAndPasswordMethod());
            }

            blCode.Append(SaveMethod() + GetAllMethod()).Append(DeleteMethod() + IsExistMethod());

            if (clsFormat.Singularize(_TableName.ToLower()) == "user")
            {
                blCode.Append(IsExistByUsernameMethod() + IsExistByPersonIdMethod());
            }

            blCode.Append(Closing());

            string fileName = $"cls{_FormattedTNSingle}.cs";
            return clsFile.StoreToFile(blCode.ToString(), fileName, folderPath, true);
        }

        #endregion

        public static bool GenerateBlCode(string tableName, string? folderPath = null, enCodeStyle codeStyle = enCodeStyle.EFStyle)
        {
            return codeStyle == enCodeStyle.EFStyle ? GenerateEFBlCode(tableName, folderPath) : false;
        }

    }
}