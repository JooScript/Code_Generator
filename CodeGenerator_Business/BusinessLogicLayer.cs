using System.Text;
using Utilities;

namespace CodeGenerator_BusinessLogic
{
    public static class clsBlGenerator
    {
        public enum enCodeStyle
        {
            EFStyle = 0,
            AdoStyle = 1
        }
        private static string _TableName;
        private static List<clsDatabase.ColumnInfo> _columns;

        #region Properties

        private static string TableName
        {
            get
            {
                return _TableName;
            }
            set
            {
                if (_TableName != value)
                {
                    _TableName = value;
                    clsDatabase.Initialize(clsDataAccessSettings.ConnectionString());
                    _columns = clsDatabase.GetTableColumns(_TableName);
                }
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

        private static string Properties()
        {
            var sb = new StringBuilder();

            foreach (var column in _columns)
            {
                string csharpType = clsUtil.ConvertDbTypeToCSharpType(column.DataType);
                string nullableSymbol = column.IsNullable ? "?" : "";
                string propertyName = clsFormat.CapitalizeFirstChars(clsFormat.FormatId(column.Name));

                sb.AppendLine($"        public {csharpType}{nullableSymbol} {propertyName}");
                sb.AppendLine("        {");
                sb.AppendLine("            get;");
                sb.AppendLine("            set;");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string GenerateConstructorParameters()
        {
            var parameters = new List<string>();

            foreach (var column in _columns)
            {
                string csharpType = clsUtil.ConvertDbTypeToCSharpType(column.DataType);
                string nullableSymbol = column.IsNullable ? "?" : "";
                string parameterName = clsFormat.CapitalizeFirstChars(clsFormat.FormatId(column.Name));

                parameters.Add($"{csharpType}{nullableSymbol} {parameterName}");
            }

            return string.Join(", ", parameters);
        }

        private static string GenerateConstructorAssignments()
        {
            var sb = new StringBuilder();

            foreach (var column in _columns)
            {
                string propertyName = clsFormat.CapitalizeFirstChars(clsFormat.FormatId(column.Name));
                sb.AppendLine($"            this.{propertyName} = {propertyName};");
            }

            return sb.ToString();
        }

        private static string GenerateDefaultValues()
        {
            var sb = new StringBuilder();

            foreach (var column in _columns)
            {
                string propertyName = clsFormat.CapitalizeFirstChars(clsFormat.FormatId(column.Name));
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
                    return "-1";
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

        private static string GenerateFindMethodTupleDeconstruction()
        {
            var sb = new StringBuilder();
            sb.Append("var (");

            bool first = true;
            foreach (var column in _columns)
            {
                if (!first)
                    sb.Append(", ");

                string propertyName = clsFormat.CapitalizeFirstChars(clsFormat.FormatId(column.Name));
                sb.Append(propertyName);
                first = false;
            }

            sb.Append(") = info.Value;");
            return sb.ToString();
        }

        private static string GenerateFindMethodReturnNew()
        {
            var sb = new StringBuilder();
            sb.Append($"return new cls{_FormattedTNSingle}(");

            bool first = true;
            foreach (var column in _columns)
            {
                if (!first)
                    sb.Append(", ");

                string propertyName = clsFormat.CapitalizeFirstChars(clsFormat.FormatId(column.Name));
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
                string propertyName = clsFormat.CapitalizeFirstChars(clsFormat.FormatId(column.Name));
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
            string appName = clsDataAccessSettings.AppName();
            return $@"using {appName}_DataAccess.DataAccess;
using {appName}_DataAccess.Entities;
using Utilities;

namespace {appName}_Business.BusinessLogic
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

        private static string DefaultConstructor()
        {
            return $@"        public cls{_FormattedTNSingle}()
        {{
{GenerateDefaultValues()}
            Mode = enMode.AddNew;
        }}
";
        }

        private static string ParameterizedConstructor()
        {
            return $@"        private cls{_FormattedTNSingle}({GenerateConstructorParameters()})
        {{
{GenerateConstructorAssignments()}
            Mode = enMode.Update;
        }}
";
        }

        private static string AddNewMethod()
        {
            return $@"        private async Task<bool> _AddNew{_FormattedTNSingle}Async()
        {{
            try
            {{
                this.{_FormattedTNSingle}Id = await cls{_FormattedTNSingle}Data.AddNew{_FormattedTNSingle}Async({GenerateAddNewParameters()});
                return this.{_FormattedTNSingle}Id != -1;
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return false;
            }}
        }}
";
        }

        private static string GenerateAddNewParameters()
        {
            var parameters = new List<string>();

            foreach (var column in _columns)
            {
                if (column.IsIdentity)
                    continue;

                string propertyName = clsFormat.CapitalizeFirstChars(clsFormat.FormatId(column.Name));
                parameters.Add($"this.{propertyName}");
            }

            return string.Join(", ", parameters);
        }

        private static string UpdateMethod()
        {
            return $@"        private async Task<bool> _Update{_FormattedTNSingle}Async()
        {{
            try
            {{
                return await cls{_FormattedTNSingle}Data.Update{_FormattedTNSingle}Async({GenerateUpdateParameters()});
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return false;
            }}
        }}
";
        }

        private static string GenerateUpdateParameters()
        {
            var parameters = new List<string>();

            foreach (var column in _columns)
            {
                string propertyName = clsFormat.CapitalizeFirstChars(clsFormat.FormatId(column.Name));
                parameters.Add($"this.{propertyName}");
            }

            return string.Join(", ", parameters);
        }

        private static string FindMethod()
        {
            return $@"        public static async Task<cls{_FormattedTNSingle}?> FindAsync(int {_FormattedTNSingleVar}Id)
        {{
            if ({_FormattedTNSingleVar}Id <= 0)
            {{
                return null;
            }}

            try
            {{
                var info = await cls{_FormattedTNSingle}Data.Get{_FormattedTNSingle}InfoByIdAsync({_FormattedTNSingleVar}Id);

                if (info == null)
                {{
                    return null;
                }}

                {GenerateFindMethodTupleDeconstruction()}
                {GenerateFindMethodReturnNew()}
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return null;
            }}
        }}

        public static cls{_FormattedTNSingle}? Find(int {_FormattedTNSingleVar}Id)
        {{
            return FindAsync({_FormattedTNSingleVar}Id).GetAwaiter().GetResult();
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
            return $@"        public static async Task<bool> Delete{_FormattedTNSingle}Async(int {_FormattedTNSingleVar}Id)
        {{
            return await cls{_FormattedTNSingle}Data.Delete{_FormattedTNSingle}Async({_FormattedTNSingleVar}Id);
        }}

        public static bool Delete{_FormattedTNSingle}(int {_FormattedTNSingleVar}Id)
        {{
            return Delete{_FormattedTNSingle}Async({_FormattedTNSingleVar}Id).GetAwaiter().GetResult();
        }}
";
        }

        private static string IsExistMethod()
        {
            return $@"        public static async Task<bool> Is{_FormattedTNSingle}ExistAsync(int {_FormattedTNSingleVar}Id)
        {{
            return await cls{_FormattedTNSingle}Data.Is{_FormattedTNSingle}ExistsAsync({_FormattedTNSingleVar}Id);
        }}

        public static bool Is{_FormattedTNSingle}Exist(int {_FormattedTNSingleVar}Id)
        {{
            return Is{_FormattedTNSingle}ExistAsync({_FormattedTNSingleVar}Id).GetAwaiter().GetResult();
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
                folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Code Generator\\BusinessLogic\\");
            }

            StringBuilder blCode = new StringBuilder();

            blCode.Append(TopUsing());
            blCode.Append(ModeEnum());
            blCode.Append(Properties());
            blCode.Append(DefaultConstructor());
            blCode.Append(ParameterizedConstructor());
            blCode.Append(AddNewMethod());
            blCode.Append(UpdateMethod());
            blCode.Append(FindMethod());
            blCode.Append(SaveMethod());
            blCode.Append(GetAllMethod());
            blCode.Append(DeleteMethod());
            blCode.Append(IsExistMethod());
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
