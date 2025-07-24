using System.Text;
using System.Text.RegularExpressions;
using Utilities;

namespace CodeGenerator.Bl
{
    public class ClsBlGenerator : ClsGenerator
    {
        public ClsBlGenerator(string tableName) : base(tableName)
        {

        }
        #region Support Methods

        private static string _GetClsNameFromColName(string colName)
        {

            if (string.IsNullOrEmpty(colName))
            {
                return string.Empty;
            }

            foreach (var keyTable in foreignKeys)
            {
                if (colName.ToLower() == keyTable.ColumnName.ToLower())
                {
                    return FormatHelper.Singularize(keyTable.ReferencedTable);
                }
            }

            return string.Empty;
        }

        private static string _GetForeignKeyClsName(string colName)
        {
            return $"Cls{_GetClsNameFromColName(colName)}";
        }

        private static string _GetColNameFromProperty(string propertyName) => propertyName.Substring(0, propertyName.Length - 2);

        private static string _GetClsNameFromProperty(string propertyName) => "Cls" + Regex.Replace(_GetForeignKeyClsName(propertyName).Substring(3), "^Tbl?", "", RegexOptions.IgnoreCase);

        private static string _ConstructorParameters()
        {
            var parameters = new List<string>();

            parameters.Add($"{DtoClsName} {FormattedTNSingleVar}DTO");


            parameters.Add("enMode cMode = enMode.AddNew");


            return string.Join(", ", parameters);
        }

        private static string _ConstructorAssignments()
        {


            var sb = new StringBuilder();

            foreach (var column in Columns)
            {
                string propertyName = FormatHelper.CapitalizeFirstChars(FormatId(column.Name));
                if (column.IsPrimaryKey)
                {
                    sb.AppendLine($"           this.{propertyName} = {FormattedTNSingleVar}DTO.Id;");
                    continue;
                }
                sb.AppendLine($"            this.{propertyName} = {FormattedTNSingleVar}DTO.{propertyName};");

                if (column.IsForeignKey && !column.IsNullable)
                {
                    string colName = _GetColNameFromProperty(propertyName);
                    string clsName = _GetClsNameFromProperty(propertyName);

                    sb.AppendLine($"          this.{colName}Info = {clsName}.Find(DTO.Id);");
                }
            }

            sb.AppendLine();



            sb.Append("            Mode = cMode;");



            return sb.ToString();
        }

        #endregion

        #region Class Structure

        private static string TopUsing(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.Ado:
                    return $@"using {AppName}.Da;
using {AppName}.DTO;
using Utilities;

namespace {AppName}.Bl
{{
    public class {LogicClsName}
    {{";
                case enCodeStyle.EF:
                    return $@"using {AppName}.Da;
using {AppName}.Models;
using Utilities;

namespace {AppName}.Bl
{{
    public class {LogicClsName} : {LogicInterfaceName}
    {{";
                default:
                    throw new ArgumentException("Invalid code style specified.");
            }
        }

        private static string AdoDTOProperty()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"        public {DtoClsName} DTO");
            sb.AppendLine("        {");
            sb.AppendLine("            get");
            sb.AppendLine("            {");
            sb.Append("                return (new " + FormattedTNSingle + "DTO(");

            bool first = true;
            foreach (var column in Columns)
            {
                if (!first)
                {
                    sb.Append(", ");
                }
                first = false;

                sb.Append($"this.{FormatHelper.CapitalizeFirstChars(FormatId(column.Name))}");

            }

            sb.AppendLine("));");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();

            return sb.ToString();
        }

        private static string AdoProperties()
        {
            var sb = new StringBuilder();

            foreach (var column in Columns)
            {
                string csharpType = Helper.GetCSharpType(column.DataType);
                string nullableSymbol = column.IsNullable ? "?" : "";
                string propertyName = FormatHelper.CapitalizeFirstChars(FormatId(column.Name));

                sb.AppendLine($"        public {csharpType}{nullableSymbol} {propertyName}");
                sb.AppendLine("        {");
                sb.AppendLine("            get;");
                sb.AppendLine("            set;");
                sb.AppendLine("        }");
                sb.AppendLine();

                if (column.IsForeignKey)
                {
                    string colName = _GetColNameFromProperty(propertyName);
                    string clsName = _GetClsNameFromProperty(propertyName);

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

        private static string FindMethod(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.Ado:
                    return $@"        public static async Task<{LogicClsName}> {MethodNames.GetById}({TableIdDT} {TableId})
        {{
            if ({TableId} <= 0 || {TableId} == null)
            {{
                return null;
            }}

            try
            {{
                {DtoClsName} {FormattedTNSingleVar}DTO = await {DataClsName}.Get{FormattedTNSingle}ByIdAsync({TableId});
                return {FormattedTNSingleVar}DTO != null ? new {LogicClsName}({FormattedTNSingleVar}DTO, enMode.Update) : null;
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return null;
            }}
        }}

";
                case enCodeStyle.EF:
                    return $@"               {ImplementationMethod(MethodNames.GetById)}
        {{
            if (id <= 0 || id == null)
            {{
                return null;
            }}

            try
            {{
                var {FormattedTNSingleVar} = await {DataObjName}.{MethodNames.GetById}(id);
                return {FormattedTNSingleVar};
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return null;
            }}
        }}

";
                default:
                    throw new ArgumentException("Invalid code style specified.");
            }
        }

        private static string FindByUsernameAndPasswordMethod(enCodeStyle codeStyle)
        {

            switch (codeStyle)
            {
                case enCodeStyle.Ado:
                    return $@"        public static async Task<{LogicClsName}> FindByUsernameAndPasswordAsync(string username, string password)
        {{
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
             {{ 
return null;
}}

            try
            {{
                {DtoClsName} {FormattedTNSingleVar}DTO = await {DataClsName}.Get{FormattedTNSingle}ByUsernameAndPasswordAsync(username, password);
                return {FormattedTNSingleVar}DTO != null ? new {LogicClsName}({FormattedTNSingleVar}DTO, enMode.Update) : null;
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return null;
            }}
        }}

";
                case enCodeStyle.EF:
                    return $@"        public async Task<{ModelName}> FindByUsernameAndPasswordAsync(string username, string password)
        {{
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
             {{ 
return null;
}}

            try
            {{
                {DtoClsName} {FormattedTNSingleVar}DTO = await {DataObjName}.GetByUsernameAndPasswordAsync(username, password);
                return {FormattedTNSingleVar}DTO != null ? new {LogicClsName}({FormattedTNSingleVar}DTO, enMode.Update) : null;
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return null;
            }}
        }}

";
                default:
                    throw new ArgumentException("Invalid code style specified.");
            }



        }

        private static string FindByPersonIDMethod(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.Ado:
                    return $@"        public static async Task<{LogicClsName}> FindByPersonIdAsync({TableIdDT} personId)
        {{
            if (personId <= 0)
                return null;

            try
            {{
                {DtoClsName} {FormattedTNSingleVar}DTO = await {DataClsName}.Get{FormattedTNSingle}ByPersonIdAsync(personId);
                return {FormattedTNSingleVar}DTO != null ? new {LogicClsName}({FormattedTNSingleVar}DTO, enMode.Update) : null;
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return null;
            }}
        }}

";
                case enCodeStyle.EF:
                    return $@"        public async Task<{ModelName}> FindByPersonIdAsync(int personId)
        {{
            if (personId <= 0)
                return null;

            try
            {{
                {DtoClsName} {FormattedTNSingleVar}DTO = await {DataObjName}.GetByPersonIdAsync(personId);
                return {FormattedTNSingleVar}DTO != null ? new {LogicClsName}({FormattedTNSingleVar}DTO, enMode.Update) : null;
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return null;
            }}
        }}

";
                default:
                    throw new ArgumentException("Invalid code style specified.");
            }



        }

        private static string FindByCountryNameMethod(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.Ado:
                    { return $@"        public static async Task<{LogicClsName}> {MethodNames.GetById}(string CountryName)
        {{
              if (string.IsNullOrWhiteSpace(CountryName))
            {{
                return null;
            }}

            try
            {{
                {DtoClsName} {FormattedTNSingleVar}DTO = await {DataClsName}.Get{FormattedTNSingle}ByCountryNameAsync(CountryName);
                return {FormattedTNSingleVar}DTO != null ? new {LogicClsName}({FormattedTNSingleVar}DTO, enMode.Update) : null;
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return null;
            }}
        }}

"; }
                case enCodeStyle.EF:
                    {
                        return $@"        public async Task<{ModelName}> {MethodNames.GetById}(string CountryName)
        {{
              if (string.IsNullOrWhiteSpace(CountryName))
            {{
                return null;
            }}

            try
            {{
                {DtoClsName} {FormattedTNSingleVar}DTO = await {DataObjName}.GetByCountryNameAsync(CountryName);
                return {FormattedTNSingleVar}DTO != null ? new {LogicClsName}({FormattedTNSingleVar}DTO, enMode.Update) : null;
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return null;
            }}
        }}

";
                    }
                default:
                    {
                        throw new ArgumentException("Invalid code style specified.");
                    }
            }
        }

        private static string IsExistByUsernameMethod(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.Ado:
                    {
                        return $@"        public static async Task<bool> IsExistsByUsernameAsync(string username)
        {{
            if (string.IsNullOrWhiteSpace(username))
                return false;

            try
            {{
                return await {DataClsName}.IsExistsByUsernameAsync(username);
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return false;
            }}
        }}

";
                    }
                case enCodeStyle.EF:
                    {
                        return $@"        public async Task<bool> IsExistsByUsernameAsync(string username)
        {{
            if (string.IsNullOrWhiteSpace(username))
                return false;

            try
            {{
                return await {DataObjName}.IsExistsByUsernameAsync(username);
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return false;
            }}
        }}

";
                    }
                default:
                    {
                        throw new ArgumentException("Invalid code style specified.");
                    }
            }
        }

        private static string IsExistByPersonIdMethod(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.Ado: { return $@"        public static async Task<bool> IsExistsByPersonIdAsync(int personId)
        {{
            if (personId <= 0)
                return false;

            try
            {{
                return await {DataClsName}.IsExistsByPersonIdAsync(personId);
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return false;
            }}
        }}

"; }
                case enCodeStyle.EF:
                    {
                        return $@"        public async Task<bool> IsExistsByPersonIdAsync(int personId)
        {{
            if (personId <= 0)
                return false;

            try
            {{
                return await {DataObjName}.IsExistsByPersonIdAsync(personId);
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return false;
            }}
        }}

";
                    }
                default:
                    {
                        throw new ArgumentException("Invalid code style specified.");
                    }
            }
        }

        private static string CountMethod(enCodeStyle codeStyle)
        {

            switch (codeStyle)
            {
                case enCodeStyle.Ado:
                    {
                        return $@"        public static async Task<{TableIdDT}> {MethodNames.Count}()
        {{
            return await {DataClsName}.{MethodNames.Count}();
        }}

";
                    }
                case enCodeStyle.EF:
                    {
                        return $@"        {ImplementationMethod(MethodNames.Count)}
        {{
            return await {DataObjName}.{MethodNames.Count}();
        }}

";
                    }

                default:
                    {
                        throw new ArgumentException("Invalid code style specified.");
                    }
            }
        }

        private static string EFContextConstructor()
        {
            return @$"
        {DataClsName} {DataObjName};

        public {LogicClsName}({ContextName} context)
        {{
            {DataObjName} = new {DataClsName}(context);
        }}
";
        }

        private static string SynchronousWrappers(enCodeStyle codeStyle)
        {
            string StartRegion = $@"        #region Synchronous Wrappers

";
            string GetAll, Count, Find, Save, Delete, IsExist;

            if (codeStyle == enCodeStyle.Ado)
            {
                GetAll = $@"       public static List<{DtoClsName}> GetAll(int pageNumber = 1, int pageSize = 50)
        {{
            return GetAll{FormattedTNPluralize}Async( pageNumber ,  pageSize ).GetAwaiter().GetResult();
        }}

";
                Count = $@"        public static {TableIdDT} {FormattedTNPluralize}Count()
        {{
            return {FormattedTNPluralize}{MethodNames.Count}().GetAwaiter().GetResult();
        }}

";
                Find = $@"         public static {LogicClsName} Find({TableIdDT} {FormattedTableId})
        {{
            return {MethodNames.GetById}({FormattedTableId}).GetAwaiter().GetResult();
        }}

";
                Save = $@"       public bool Save()
        {{
            return {MethodNames.Save}().GetAwaiter().GetResult();
        }}

";
                Delete = $@"        public static bool Delete{FormattedTNSingle}({TableIdDT} {FormattedTableId})
        {{
            return Delete{FormattedTNSingle}Async({FormattedTableId}).GetAwaiter().GetResult();
        }}

";
                IsExist = $@"        public static bool Is{FormattedTNSingle}Exists({TableIdDT} {FormattedTableId})
        {{
            return Is{FormattedTNSingle}ExistsAsync({FormattedTableId}).GetAwaiter().GetResult();
        }}

";
            }
            else
            {
                GetAll = $@"       public List<{ModelName}> GetAll(int pageNumber = 1, int pageSize = 50)
        {{
            return {MethodNames.GetAll}( pageNumber ,  pageSize ).GetAwaiter().GetResult();
        }}

";
                Count = $@"        public {TableIdDT} Count()
        {{
            return {MethodNames.Count}().GetAwaiter().GetResult();
        }}

";
                Find = $@"         public {ModelName} Find({TableIdDT} id)
        {{
            return {MethodNames.GetById}(id).GetAwaiter().GetResult();
        }}

";
                Save = $@"       public bool Save()
        {{
            return {MethodNames.Save}().GetAwaiter().GetResult();
        }}

";
                Delete = $@"        public bool Delete({TableIdDT} id)
        {{
            return {MethodNames.Delete}(id).GetAwaiter().GetResult();
        }}

";
                IsExist = $@"        public bool IsExists({TableIdDT} id)
        {{
            return {MethodNames.IsExists}(id).GetAwaiter().GetResult();
        }}

";
            }

            string EndRegion = $@"     #endregion

";
            string FindByUserNameAndPassword = $@"         public static {LogicClsName} FindByUsernameAndPassword(string username, string password)
    {{
        return FindByUsernameAndPasswordAsync( username, password).GetAwaiter().GetResult();  
    }}

";
            string FindByCountryName = $@"         public static {LogicClsName} Find(string CountryName)
    {{
        return {MethodNames.GetById}(CountryName).GetAwaiter().GetResult();
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

            if (FormatHelper.Singularize(TableName.ToLower()) == "user")
            {
                Wrapper.Append(FindByUserNameAndPassword);
                Wrapper.Append(FindByPersonID);
            }

            if (FormatHelper.Singularize(TableName.ToLower()) == "country")
            {
                Wrapper.Append(FindByCountryName);
            }

            Wrapper.Append(Save);
            Wrapper.Append(GetAll);
            Wrapper.Append(IsExist);

            if (FormatHelper.Singularize(TableName.ToLower()) == "user")
            {
                Wrapper.Append(IsExistsByUserName);
                Wrapper.Append(IsExistsByPersonID);
            }

            Wrapper.Append(Count);
            Wrapper.Append(Delete);
            Wrapper.Append(EndRegion);

            return Wrapper.ToString();
        }

        private static string AddNewMethod(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.Ado:
                    {
                        string PasswordValidatation = "";
                        if (FormatHelper.Singularize(TableName.ToLower()) == "user")
                        {
                            PasswordValidatation = @"            if (!ClsValidation.IsValidStrongPassword(this.Password))
            {
                Helper.ErrorLogger(new Exception(""Password does not meet strength requirements""));
                return false;
            }
";
                        }

                        return $@"        private async Task<bool> _AddAsync()
        {{{PasswordValidatation}
            try
            {{
                this.{TableId} = await {DataClsName}.{MethodNames.Add}(DTO);
                return (this.{TableId} != -1);
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return false;
            }}
        }}

"; ;
                    }
                case enCodeStyle.EF:
                    { return $@"              {ImplementationMethod(MethodNames.Add)}
        {{
              {TableIdDT} {FormattedTNSingleVar}Id = -1;
            try
            {{
                {FormattedTNSingleVar}Id = await {DataObjName}.{MethodNames.Add}({FormattedTNSingleVar});
                return {FormattedTNSingleVar}Id;
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return -1;
            }}
        }}

"; }
                default:
                    throw new ArgumentException("Invalid code style specified.");
            }
        }

        private static string UpdateMethod(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.Ado:
                    {
                        string PasswordValidatation = "";
                        if (FormatHelper.Singularize(TableName.ToLower()) == "user")
                        {
                            PasswordValidatation = @"            if (!ClsValidation.IsValidStrongPassword(this.Password))
            {
                Helper.ErrorLogger(new Exception(""Password does not meet strength requirements""));
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
                Helper.ErrorLogger(ex);
                return false;
            }}
        }}

";
                    }
                case enCodeStyle.EF:
                    { return $@"            {ImplementationMethod(MethodNames.Update)}
        {{
            try
            {{
                return await {DataObjName}.{MethodNames.Update}({FormattedTNSingleVar});
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return false;
            }}
        }}

"; }
                default:
                    throw new ArgumentException("Invalid code style specified.");
            }
        }

        private static string GetAllMethod(enCodeStyle codeStyle)
        {

            switch (codeStyle)
            {
                case enCodeStyle.Ado:
                    { return $@"        public static async Task<List<{DtoClsName}>> {MethodNames.GetAll}(int pageNumber = 1, int pageSize = 50)
        {{
            try
            {{
                return await {DataClsName}.{MethodNames.GetAll}( pageNumber,  pageSize );
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                throw;
            }}
        }}

"; }
                case enCodeStyle.EF:
                    { return $@"        {ImplementationMethod(MethodNames.GetAll)}
        {{
            try
            {{
                return await {DataObjName}.{MethodNames.GetAll}( pageNumber,  pageSize );
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                throw;
            }}
        }}

"; }
                default:
                    {
                        throw new ArgumentException("Invalid code style specified.");
                    }
            }



        }

        private static string IsExistMethod(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.Ado:
                    {
                        return $@"        public static async Task<bool> {MethodNames.IsExists}({TableIdDT} {FormattedTableId})
        {{
            return await {DataClsName}.{MethodNames.IsExists}({FormattedTableId});
        }}

";
                    }
                case enCodeStyle.EF:
                    {
                        return $@"        {ImplementationMethod(MethodNames.IsExists)}
        {{
            return await {DataObjName}.{MethodNames.IsExists}(id);
        }}

";
                    }
                default:
                    {
                        throw new ArgumentException("Invalid code style specified.");
                    }

            }
        }

        private static string DeleteMethod(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.Ado:
                    { return $@"        public static async Task<bool> {MethodNames.Delete}({TableIdDT} id)
        {{
            return await {DataClsName}.{MethodNames.Delete}(id);
        }}

"; }
                case enCodeStyle.EF:
                    { return $@"        {ImplementationMethod(MethodNames.Delete)}
        {{
            return await {DataObjName}.{MethodNames.Delete}(id);
        }}

"; }
                default:
                    throw new ArgumentException("Invalid code style specified.");
            }


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

        private static string AdoParameterizedConstructor()
        {
            return $@"        public {LogicClsName}({_ConstructorParameters()})
        {{
{_ConstructorAssignments()}
        }}

";
        }

        private static string SaveMethod(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.Ado: { return $@"        public async Task<bool> {MethodNames.Save}()
        {{
            switch (Mode)
            {{
                case enMode.AddNew:
                    if (await _AddAsync())
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
                        return await _UpdateAsync();
                    }}
                default:
                    {{
                        return false;
                    }}
            }}
        }}

"; }
                case enCodeStyle.EF:
                    {
                        return @$" {ImplementationMethod(MethodNames.Save)} => {FormattedTNSingleVar}.{FormattedTableId} <= 0 ? await {MethodNames.Add}({FormattedTNSingleVar}) != -1 : await {MethodNames.Update}({FormattedTNSingleVar});
";
                    }
                default: throw new ArgumentException("Invalid code style specified.");


            }



        }

        private static string Closing()
        {
            return $@"    }}
}}";
        }

        #endregion

        #region Generation Methods

        public bool GenerateEFBlCode(out string filePath)
        {
            filePath = null;

            var style = enCodeStyle.EF;



            StringBuilder blCode = new StringBuilder();

            blCode.Append(TopUsing(style));
            blCode.Append(EFContextConstructor());
            blCode.Append(FindMethod(style));

            if (FormatHelper.Singularize(TableName.ToLower()) == "user")
            {
                blCode.Append(FindByUsernameAndPasswordMethod(style));
                blCode.Append(FindByPersonIDMethod(style));
            }

            if (FormatHelper.Singularize(TableName.ToLower()) == "country")
            {
                blCode.Append(FindByCountryNameMethod(style));
            }

            blCode.Append(AddNewMethod(style));
            blCode.Append(UpdateMethod(style));
            blCode.Append(SaveMethod(style));
            blCode.Append(GetAllMethod(style));
            blCode.Append(IsExistMethod(style));

            if (FormatHelper.Singularize(TableName.ToLower()) == "user")
            {
                blCode.Append(IsExistByUsernameMethod(style));
                blCode.Append(IsExistByPersonIdMethod(style));
            }

            blCode.Append(CountMethod(style));
            blCode.Append(DeleteMethod(style));
            //blCode.Append(SynchronousWrappers(style));
            blCode.Append(Closing());

            string folderPath = Path.Combine(StoringPath, "Logic");
            string fileName = $"{LogicClsName}.cs";

            bool success = FileHelper.StoreToFile(blCode.ToString(), fileName, folderPath, true);
            //&& ClsInterfacesGenerator.GenerateInterfaceCode(tableName,out string interfacesFilePath);

            if (success)
            {
                filePath = Path.Combine(folderPath, fileName);
            }

            return success;
        }

        public bool GenerateAdoBlCode(out string filePath)
        {
            filePath = null;
            var style = enCodeStyle.Ado;



            StringBuilder blCode = new StringBuilder();

            blCode.Append(TopUsing(style));
            blCode.Append(ModeEnum());
            blCode.Append(AdoDTOProperty());
            blCode.Append(AdoProperties());
            blCode.Append(AdoParameterizedConstructor());
            blCode.Append(FindMethod(style));

            if (FormatHelper.Singularize(TableName.ToLower()) == "user")
            {
                blCode.Append(FindByUsernameAndPasswordMethod(style));
                blCode.Append(FindByPersonIDMethod(style));
            }

            if (FormatHelper.Singularize(TableName.ToLower()) == "country")
            {
                blCode.Append(FindByCountryNameMethod(style));
            }

            blCode.Append(AddNewMethod(style));
            blCode.Append(UpdateMethod(style));
            blCode.Append(SaveMethod(style));
            blCode.Append(GetAllMethod(style));
            blCode.Append(IsExistMethod(style));

            if (FormatHelper.Singularize(TableName.ToLower()) == "user")
            {
                blCode.Append(IsExistByUsernameMethod(style));
                blCode.Append(IsExistByPersonIdMethod(style));
            }

            blCode.Append(CountMethod(style));
            blCode.Append(DeleteMethod(style));
            blCode.Append(SynchronousWrappers(style));
            blCode.Append(Closing());

            string folderPath = Path.Combine(StoringPath, "Logic");
            string fileName = $"{LogicClsName}.cs";

            bool success = FileHelper.StoreToFile(blCode.ToString(), fileName, folderPath, true);

            if (success)
            {
                filePath = Path.Combine(folderPath, fileName);
            }

            return success;
        }

        public bool GenerateBlCode(enCodeStyle codeStyle, out string path) => (codeStyle == enCodeStyle.Ado ? GenerateAdoBlCode(out path) : GenerateEFBlCode(out path));

        #endregion

    }
}