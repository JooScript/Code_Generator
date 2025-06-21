using System.Text;
using Utilities;

namespace CodeGenerator_Logic
{
    public class ClsLogicGenerator : ClsGenerator
    {
        #region Support Methods

        private static string ConstructorParameters()
        {
            var parameters = new List<string>();

            parameters.Add($"{FormattedTNSingle}DTO {FormattedTNSingleVar}DTO");
            parameters.Add("enMode cMode = enMode.AddNew");

            return string.Join(", ", parameters);
        }

        private static string GetClassNameFromColumnName(string colName)
        {

            if (string.IsNullOrEmpty(colName))
            {
                return string.Empty;
            }

            foreach (var keyTable in foreignKeys)
            {
                if (colName.ToLower() == keyTable.ColumnName.ToLower())
                {
                    return ClsFormat.Singularize(keyTable.ReferencedTable);
                }
            }

            return string.Empty;
        }

        private static string GetForeignKeyClassName(string colName)
        {
            return $"Cls{GetClassNameFromColumnName(colName)}";
        }

        private static string ConstructorAssignments()
        {
            var sb = new StringBuilder();

            foreach (var column in columns)
            {
                string propertyName = ClsFormat.CapitalizeFirstChars(ClsGlobal.FormatId(column.Name));
                if (column.IsPrimaryKey)
                {
                    sb.AppendLine($"            this.{propertyName} = {FormattedTNSingleVar}DTO.Id;");
                    continue;
                }
                sb.AppendLine($"            this.{propertyName} = {FormattedTNSingleVar}DTO.{propertyName};");

                if (column.IsForeignKey && !column.IsNullable)
                {
                    string colName = propertyName.Substring(0, propertyName.Length - 2);
                    string clsName = $"{GetForeignKeyClassName(propertyName)}";

                    sb.AppendLine($"            this.{colName}Info = {clsName}.Find(DTO.Id);");
                }
            }

            sb.AppendLine();
            sb.Append("            Mode = cMode;");

            return sb.ToString();
        }

        #endregion

        #region Class Structure

        private static string TopUsing()
        {
            return $@"using {AppName}_Data.DataAccess;
using {AppName}_Data.DTO;
using Utilities;

namespace {AppName}_Business.BusinessLogic
{{
    public class {LogicClsName}
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

        private static string DTOProperty()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"        public {FormattedTNSingle}DTO DTO");
            sb.AppendLine("        {");
            sb.AppendLine("            get");
            sb.AppendLine("            {");
            sb.Append("                return (new " + FormattedTNSingle + "DTO(");

            bool first = true;
            foreach (var column in columns)
            {
                if (!first)
                {
                    sb.Append(", ");
                }
                first = false;

                sb.Append($"this.{ClsFormat.CapitalizeFirstChars(ClsGlobal.FormatId(column.Name))}");

            }

            sb.AppendLine("));");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();

            return sb.ToString();
        }

        private static string Properties()
        {
            var sb = new StringBuilder();

            foreach (var column in columns)
            {
                string csharpType = ClsUtil.ConvertDbTypeToCSharpType(column.DataType);
                string nullableSymbol = column.IsNullable ? "?" : "";
                string propertyName = ClsFormat.CapitalizeFirstChars(ClsGlobal.FormatId(column.Name));

                sb.AppendLine($"        public {csharpType}{nullableSymbol} {propertyName}");
                sb.AppendLine("        {");
                sb.AppendLine("            get;");
                sb.AppendLine("            set;");
                sb.AppendLine("        }");
                sb.AppendLine();

                if (column.IsForeignKey)
                {
                    string colName = propertyName.Substring(0, propertyName.Length - 2);
                    string clsName = $"{GetForeignKeyClassName(propertyName)}";
                    sb.AppendLine($"        public {clsName} {colName}Info");
                    sb.AppendLine("        {");
                    sb.AppendLine("            get;");
                    sb.AppendLine("            set;");
                    sb.AppendLine("        }");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        private static string ParameterizedConstructor()
        {
            return $@"        public {LogicClsName}({ConstructorParameters()})
        {{
{ConstructorAssignments()}
        }}

";
        }

        private static string FindMethod()
        {
            return $@"        public static async Task<{LogicClsName}> FindAsync(int {TableId})
        {{
            if ({TableId} <= 0 || {TableId} == null)
            {{
                return null;
            }}

            try
            {{
                {FormattedTNSingle}DTO {FormattedTNSingleVar}DTO = await {DataClsName}.Get{FormattedTNSingle}ByIdAsync({TableId});
                return {FormattedTNSingleVar}DTO != null ? new {LogicClsName}({FormattedTNSingleVar}DTO, enMode.Update) : null;
            }}
            catch (Exception ex)
            {{
                ClsUtil.ErrorLogger(ex);
                return null;
            }}
        }}

";
        }

        private static string FindByUsernameAndPasswordMethod()
        {
            return $@"        public static async Task<{LogicClsName}> FindByUsernameAndPasswordAsync(string username, string password)
        {{
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
             {{ 
return null;
}}

            try
            {{
                {FormattedTNSingle}DTO {FormattedTNSingleVar}DTO = await {DataClsName}.Get{FormattedTNSingle}ByUsernameAndPasswordAsync(username, password);
                return {FormattedTNSingleVar}DTO != null ? new {LogicClsName}({FormattedTNSingleVar}DTO, enMode.Update) : null;
            }}
            catch (Exception ex)
            {{
                ClsUtil.ErrorLogger(ex);
                return null;
            }}
        }}

";
        }

        private static string FindByPersonIDMethod()
        {
            return $@"        public static async Task<{LogicClsName}> FindByPersonIdAsync(int personId)
        {{
            if (personId <= 0)
                return null;

            try
            {{
                {FormattedTNSingle}DTO {FormattedTNSingleVar}DTO = await {DataClsName}.Get{FormattedTNSingle}ByPersonIdAsync(personId);
                return {FormattedTNSingleVar}DTO != null ? new {LogicClsName}({FormattedTNSingleVar}DTO, enMode.Update) : null;
            }}
            catch (Exception ex)
            {{
                ClsUtil.ErrorLogger(ex);
                return null;
            }}
        }}

";
        }

        private static string FindByCountryNameMethod()
        {
            return $@"        public static async Task<{LogicClsName}> FindAsync(string CountryName)
        {{
              if (string.IsNullOrWhiteSpace(CountryName))
            {{
                return null;
            }}

            try
            {{
                {FormattedTNSingle}DTO {FormattedTNSingleVar}DTO = await {DataClsName}.Get{FormattedTNSingle}ByCountryNameAsync(CountryName);
                return {FormattedTNSingleVar}DTO != null ? new {LogicClsName}({FormattedTNSingleVar}DTO, enMode.Update) : null;
            }}
            catch (Exception ex)
            {{
                ClsUtil.ErrorLogger(ex);
                return null;
            }}
        }}

";
        }

        private static string AddNewMethod()
        {
            string PasswordValidatation = "";
            if (ClsFormat.Singularize(_TableName.ToLower()) == "user")
            {
                PasswordValidatation = @"            if (!ClsValidation.IsValidStrongPassword(this.Password))
            {
                ClsUtil.ErrorLogger(new Exception(""Password does not meet strength requirements""));
                return false;
            }
";
            }

            return $@"        private async Task<bool> _AddNew{FormattedTNSingle}Async()
        {{{PasswordValidatation}
            try
            {{
                this.{TableId} = await {DataClsName}.Add{FormattedTNSingle}Async(DTO);
                return (this.{TableId} != -1);
            }}
            catch (Exception ex)
            {{
                ClsUtil.ErrorLogger(ex);
                return false;
            }}
        }}

";
        }

        private static string UpdateMethod()
        {
            string PasswordValidatation = "";
            if (ClsFormat.Singularize(_TableName.ToLower()) == "user")
            {
                PasswordValidatation = @"            if (!ClsValidation.IsValidStrongPassword(this.Password))
            {
                ClsUtil.ErrorLogger(new Exception(""Password does not meet strength requirements""));
                return false;
            }
";
            }

            return $@"        private async Task<bool> _Update{FormattedTNSingle}Async()
        {{{PasswordValidatation}
            try
            {{
                return await {DataClsName}.Update{FormattedTNSingle}Async(DTO);
            }}
            catch (Exception ex)
            {{
                ClsUtil.ErrorLogger(ex);
                return false;
            }}
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
                    if (await _AddNew{FormattedTNSingle}Async())
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
                        return await _Update{FormattedTNSingle}Async();
                    }}
                default:
                    {{
                        return false;
                    }}
            }}
        }}

";
        }

        private static string GetAllMethod()
        {
            return $@"        public static async Task<List<{FormattedTNSingle}DTO>> GetAll{FormattedTNPluralize}Async(int pageNumber = 1, int pageSize = 50)
        {{
            try
            {{
                return await {DataClsName}.GetAll{FormattedTNPluralize}Async( pageNumber,  pageSize );
            }}
            catch (Exception ex)
            {{
                ClsUtil.ErrorLogger(ex);
                throw;
            }}
        }}

";
        }

        private static string IsExistMethod()
        {
            return $@"        public static async Task<bool> Is{FormattedTNSingle}ExistsAsync(int {TableId})
        {{
            return await {DataClsName}.Is{FormattedTNSingle}ExistsAsync({TableId});
        }}

";
        }

        private static string IsExistByUsernameMethod()
        {
            return $@"        public static async Task<bool> Is{FormattedTNSingle}ExistsByUsernameAsync(string username)
        {{
            if (string.IsNullOrWhiteSpace(username))
                return false;

            try
            {{
                return await {DataClsName}.Is{FormattedTNSingle}ExistsByUsernameAsync(username);
            }}
            catch (Exception ex)
            {{
                ClsUtil.ErrorLogger(ex);
                return false;
            }}
        }}

";
        }

        private static string IsExistByPersonIdMethod()
        {
            return $@"        public static async Task<bool> Is{FormattedTNSingle}ExistsByPersonIdAsync(int personId)
        {{
            if (personId <= 0)
                return false;

            try
            {{
                return await {DataClsName}.Is{FormattedTNSingle}ExistsByPersonIdAsync(personId);
            }}
            catch (Exception ex)
            {{
                ClsUtil.ErrorLogger(ex);
                return false;
            }}
        }}

";
        }

        private static string CountMethod()
        {
            return $@"        public static async Task<int> {FormattedTNPluralize}CountAsync()
        {{
            return await {DataClsName}.{FormattedTNPluralize}CountAsync();
        }}

";
        }

        private static string DeleteMethod()
        {
            return $@"        public static async Task<bool> Delete{FormattedTNSingle}Async(int {TableId})
        {{
            return await {DataClsName}.Delete{FormattedTNSingle}Async({TableId});
        }}

";
        }

        private static string SynchronousWrappers()
        {
            string StartRegion = $@"        #region Synchronous Wrappers

";
            string GetAll = $@"       public static List<{FormattedTNSingle}DTO> GetAll{FormattedTNPluralize}(int pageNumber = 1, int pageSize = 50)
        {{
            return GetAll{FormattedTNPluralize}Async( pageNumber ,  pageSize ).GetAwaiter().GetResult();
        }}

";
            string Count = $@"        public static int {FormattedTNPluralize}Count()
        {{
            return {FormattedTNPluralize}CountAsync().GetAwaiter().GetResult();
        }}

";
            string Find = $@"         public static {LogicClsName} Find(int {TableId})
        {{
            return FindAsync({TableId}).GetAwaiter().GetResult();
        }}

";
            string Save = $@"       public bool Save()
        {{
            return SaveAsync().GetAwaiter().GetResult();
        }}

";
            string Delete = $@"        public static bool Delete{FormattedTNSingle}(int {TableId})
        {{
            return Delete{FormattedTNSingle}Async({TableId}).GetAwaiter().GetResult();
        }}

";
            string IsExist = $@"        public static bool Is{FormattedTNSingle}Exists(int {TableId})
        {{
            return Is{FormattedTNSingle}ExistsAsync({TableId}).GetAwaiter().GetResult();
        }}

";
            string EndRegion = $@"     #endregion

";
            string FindByUserNameAndPassword = $@"         public static {LogicClsName} FindByUsernameAndPassword(string username, string password)
        {{
            return FindByUsernameAndPasswordAsync( username, password).GetAwaiter().GetResult();  
        }}

";
            string FindByCountryName = $@"         public static {LogicClsName} Find(string CountryName)
        {{
            return FindAsync(CountryName).GetAwaiter().GetResult();
        }}

";
            string IsExistsByUserName = $@"        public static bool Is{FormattedTNSingle}ExistsByUsername(string Username)
        {{
            return Is{FormattedTNSingle}ExistsByUsernameAsync(Username).GetAwaiter().GetResult();
        }}

";
            string IsExistsByPersonID = $@"        public static bool Is{FormattedTNSingle}ExistsByPersonId(int PersonID)
        {{
            return Is{FormattedTNSingle}ExistsByPersonIdAsync(PersonID).GetAwaiter().GetResult();
        }}

";
            string FindByPersonID = $@"         public static {LogicClsName} FindByPersonId(int PersonID)
        {{
            return FindByPersonIdAsync(PersonID).GetAwaiter().GetResult();
        }}

";

            StringBuilder Wrapper = new StringBuilder();

            Wrapper.Append(StartRegion);
            Wrapper.Append(Find);

            if (ClsFormat.Singularize(_TableName.ToLower()) == "user")
            {
                Wrapper.Append(FindByUserNameAndPassword);
                Wrapper.Append(FindByPersonID);
            }

            if (ClsFormat.Singularize(_TableName.ToLower()) == "country")
            {
                Wrapper.Append(FindByCountryName);
            }

            Wrapper.Append(Save);
            Wrapper.Append(GetAll);
            Wrapper.Append(IsExist);

            if (ClsFormat.Singularize(_TableName.ToLower()) == "user")
            {
                Wrapper.Append(IsExistsByUserName);
                Wrapper.Append(IsExistsByPersonID);
            }

            Wrapper.Append(Count);
            Wrapper.Append(Delete);
            Wrapper.Append(EndRegion);

            return Wrapper.ToString();
        }

        private static string Closing()
        {
            return $@"    }}
}}";
        }

        #endregion

        public static bool GenerateBlCode(string tableName, string? folderPath = null)
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
                folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"Code Generator\\{ClsDataAccessSettings.AppName()}\\BusinessLogic\\Basic");
            }

            StringBuilder blCode = new StringBuilder();

            blCode.Append(TopUsing());
            blCode.Append(ModeEnum());
            blCode.Append(DTOProperty());
            blCode.Append(Properties());
            blCode.Append(ParameterizedConstructor());
            blCode.Append(FindMethod());

            if (ClsFormat.Singularize(_TableName.ToLower()) == "user")
            {
                blCode.Append(FindByUsernameAndPasswordMethod());
                blCode.Append(FindByPersonIDMethod());
            }

            if (ClsFormat.Singularize(_TableName.ToLower()) == "country")
            {
                blCode.Append(FindByCountryNameMethod());
            }

            blCode.Append(AddNewMethod());
            blCode.Append(UpdateMethod());
            blCode.Append(SaveMethod());
            blCode.Append(GetAllMethod());
            blCode.Append(IsExistMethod());

            if (ClsFormat.Singularize(_TableName.ToLower()) == "user")
            {
                blCode.Append(IsExistByUsernameMethod());
                blCode.Append(IsExistByPersonIdMethod());
            }

            blCode.Append(CountMethod());
            blCode.Append(DeleteMethod());
            blCode.Append(SynchronousWrappers());
            blCode.Append(Closing());

            return ClsFile.StoreToFile(blCode.ToString(), $"{LogicClsName}.cs", folderPath, true);
        }

    }
}