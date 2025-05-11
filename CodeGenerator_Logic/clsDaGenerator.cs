using Utilities;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace CodeGenerator_Logic
{
    public class clsDaGenerator : clsGenrator
    {
        #region DTO
        private static string ConstructorAssignments()
        {
            var sb = new StringBuilder();

            foreach (var column in columns)
            {
                string propertyName = clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name));

                if (column.IsPrimaryKey)
                {
                    sb.AppendLine($"            this.Id = {propertyName};");
                    continue;
                }

                sb.AppendLine($"            this.{propertyName} = {propertyName};");
            }

            return sb.ToString();
        }

        private static string ConstructorParameters()
        {
            var parameters = new List<string>();

            foreach (var column in columns)
            {
                string csharpType = clsUtil.ConvertDbTypeToCSharpType(column.DataType);
                string nullableSymbol = column.IsNullable ? "?" : "";
                string parameterName = clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name));

                parameters.Add($"{csharpType}{nullableSymbol} {parameterName}");
            }

            return string.Join(", ", parameters);
        }

        private static string ParameterizedConstructor()
        {
            return $@"        public {FormattedTNSingle}DTO({ConstructorParameters()})
        {{
{ConstructorAssignments()}}}

";
        }

        private static string Properties()
        {
            var sb = new StringBuilder();

            foreach (var column in columns)
            {
                string csharpType = clsUtil.ConvertDbTypeToCSharpType(column.DataType);
                string nullableSymbol = column.IsNullable ? "?" : "";
                string propertyName = clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name));

                if (column.IsPrimaryKey)
                {
                    sb.AppendLine($"        public {csharpType}{nullableSymbol} Id");
                }
                else
                {
                    sb.AppendLine($"        public {csharpType}{nullableSymbol} {propertyName}");
                }

                sb.AppendLine("        {");
                sb.AppendLine("            get;");
                sb.AppendLine("            set;");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public static bool GenerateDTO(string tableName, string? folderPath = null)
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
                folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Code Generator\\{AppName}\\DTO\\");
            }

            string TopUsing = $@"using {AppName}_Data.DataAccess;

namespace {AppName}_Data.DTO
{{
    public class {FormattedTNSingle}DTO
    {{";

            return clsFile.StoreToFile(new StringBuilder().Append(TopUsing + ParameterizedConstructor() + Properties() + $@"}}}}").ToString(), $"{FormattedTNSingle}DTO.cs", folderPath, true);
        }

        #endregion

        #region Stored Procedures

        public static bool IsExistsSP()
        {
            string procedureName = $"SP_Is{FormattedTNSingle}Exists";

            string procedureBody = $@"
    @{TableId} INT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS(SELECT 1 FROM [{_TableName}] WHERE [{TableId}] = @{TableId})
        RETURN 1;
    ELSE
        RETURN 0;
END";

            return clsDatabase.CreateStoredProcedure(procedureName, procedureBody);
        }

        public static bool CountSP()
        {
            string procedureName = $"SP_{FormattedTNPluralize}Count";

            string procedureBody = $@"
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT COUNT(*) AS TotalCount FROM [{_TableName}];
END";

            return clsDatabase.CreateStoredProcedure(procedureName, procedureBody);
        }

        public static bool GetAllSP()
        {
            string procedureName = $"SP_GetAll{FormattedTNPluralize}";

            string procedureBody = $@"
    @PageNumber INT,
    @PageSize INT
AS
BEGIN
    
    SELECT {GetColumnList()}
    FROM [{_TableName}]
    ORDER BY [{TableId}]
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END";

            return clsDatabase.CreateStoredProcedure(procedureName, procedureBody);
        }

        public static bool GetByIdSP()
        {
            string procedureName = $"SP_Get{FormattedTNSingle}ById";

            string procedureBody = $@"
    @{TableId} INT
AS
BEGIN
    
    SELECT {GetColumnList()}
    FROM [{_TableName}]
    WHERE [{TableId}] = @{TableId};
END";

            return clsDatabase.CreateStoredProcedure(procedureName, procedureBody);
        }

        public static bool AddNewSP()
        {
            string procedureName = $"SP_Add{FormattedTNSingle}";

            string procedureBody = $@"
{GetSPParameters(false)}
AS
BEGIN
    
    INSERT INTO [{_TableName}] (
        {GetInsertColumnList()}
    )
    VALUES (
        {GetInsertValuesList()}
    );
    
    SELECT SCOPE_IDENTITY() AS New{TableId};
END";

            return clsDatabase.CreateStoredProcedure(procedureName, procedureBody);
        }

        public static bool UpdateSP()
        {
            string procedureName = $"SP_Update{FormattedTNSingle}";

            string procedureBody = $@"
{GetSPParameters(true)}
AS
BEGIN
    
    UPDATE [{_TableName}]
    SET
{GetUpdateSetClause()}
    WHERE [{TableId}] = @{TableId};
END";

            return clsDatabase.CreateStoredProcedure(procedureName, procedureBody);
        }

        public static bool DeleteSP()
        {
            string procedureName = $"SP_Delete{FormattedTNSingle}";

            string procedureBody = $@"
    @{TableId} INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DELETE FROM [{_TableName}]
    WHERE [{TableId}] = @{TableId};
    
    SELECT @@ROWCOUNT;
END";

            return clsDatabase.CreateStoredProcedure(procedureName, procedureBody);
        }

        public static bool FindByCountryNameSP()
        {
            string procedureName = $"SP_Find{FormattedTNSingle}ByCountryName";

            string procedureBody = $@"
    @CountryName NVARCHAR(MAX)
AS
BEGIN  
    SELECT {GetColumnList()}
    FROM [{_TableName}]
    WHERE Name = @CountryName;
END";

            return clsDatabase.CreateStoredProcedure(procedureName, procedureBody);
        }

        public static bool FindByPersonIdSP()
        {
            string procedureName = $"SP_Find{FormattedTNSingle}ByPersonId";

            string procedureBody = $@"
    @PersonId INT
AS
BEGIN
    SELECT {GetColumnList()}
    FROM [{_TableName}]
    WHERE PersonId = @PersonId;
END";

            return clsDatabase.CreateStoredProcedure(procedureName, procedureBody);
        }

        public static bool FindByUsernameAndPasswordSP()
        {
            string procedureName = $"SP_Find{FormattedTNSingle}ByUsernameAndPassword";

            string procedureBody = $@"
    @Username NVARCHAR(50),
    @Password NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT {GetColumnList()}
    FROM [{_TableName}]
    WHERE Username = @Username AND Password = @Password;
END";

            return clsDatabase.CreateStoredProcedure(procedureName, procedureBody);
        }

        public static bool IsExistsByUsernameSP()
        {
            string procedureName = $"SP_Is{FormattedTNSingle}ExistsByUsername";

            string procedureBody = $@"
    @Username NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS(SELECT 1 FROM [{_TableName}] WHERE Username = @Username)
        SELECT 1 AS Result;
    ELSE
        SELECT 0 AS Result;
END";

            return clsDatabase.CreateStoredProcedure(procedureName, procedureBody);
        }

        public static bool IsExistsByPersonIdSP()
        {
            string procedureName = $"SP_Is{FormattedTNSingle}ExistsByPersonId";

            string procedureBody = $@"
    @PersonId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS(SELECT 1 FROM [{_TableName}] WHERE PersonId = @PersonId)
        SELECT 1 AS Result;
    ELSE
        SELECT 0 AS Result;
END";

            return clsDatabase.CreateStoredProcedure(procedureName, procedureBody);
        }

        public static bool GenerateAllSPs()
        {
            bool allSuccess = true;

            allSuccess &= GetAllSP();
            allSuccess &= GetByIdSP();
            allSuccess &= AddNewSP();
            allSuccess &= UpdateSP();
            allSuccess &= DeleteSP();
            allSuccess &= IsExistsSP();
            allSuccess &= CountSP();

            if (clsFormat.Singularize(_TableName.ToLower()) == "user")
            {
                allSuccess &= FindByPersonIdSP();
                allSuccess &= FindByUsernameAndPasswordSP();
                allSuccess &= IsExistsByUsernameSP();
                allSuccess &= IsExistsByPersonIdSP();
            }

            if (clsFormat.Singularize(_TableName.ToLower()) == "country")
            {
                allSuccess &= FindByCountryNameSP();
            }

            return allSuccess;
        }

        #region Helper Methods

        private static string GetColumnList()
        {
            return string.Join(", ", columns.Select(c => $"[{c.Name}]"));
        }

        private static string GetInsertColumnList()
        {
            var columns = clsGenrator.columns.Where(c => !c.IsPrimaryKey);
            return string.Join(",\n            ", columns.Select(c => $"[{c.Name}]"));
        }

        private static string GetInsertValuesList()
        {
            var columns = clsGenrator.columns.Where(c => !c.IsIdentity);
            return string.Join(",\n            ", columns.Select(c => $"@{c.Name}"));
        }

        private static string GetUpdateSetClause()
        {
            var columns = clsGenrator.columns.Where(c => !c.IsPrimaryKey);
            return string.Join(",\n            ", columns.Select(c => $"[{c.Name}] = @{c.Name}"));
        }

        private static string GetSPParameters(bool includeId)
        {
            var parameters = new List<string>();

            if (includeId)
            {
                parameters.Add($"        @{TableId} INT");
            }

            foreach (var column in columns)
            {
                if (column.IsPrimaryKey)
                {
                    continue;
                }

                string sqlType = GetSqlType(column.DataType);
                string nullability = column.IsNullable ? " = NULL" : "";
                parameters.Add($"        @{column.Name} {sqlType}{nullability}");
            }

            return string.Join(",\n", parameters);
        }

        private static string GetSqlType(string dataType)
        {
            switch (dataType.ToLower())
            {
                case "int":
                case "smallint":
                case "tinyint":
                    return "INT";
                case "varchar":
                case "nvarchar":
                    return $"{dataType.ToUpper()}(MAX)";
                case "datetime":
                    return "DATETIME";
                case "bit":
                    return "BIT";
                case "decimal":
                case "numeric":
                    return "DECIMAL(18, 2)";
                case "float":
                    return "FLOAT";
                case "uniqueidentifier":
                    return "UNIQUEIDENTIFIER";
                case "binary":
                case "varbinary":
                    return "VARBINARY(MAX)";
                default:
                    return dataType.ToUpper();
            }
        }

        #endregion

        #endregion

        #region ADO Code

        #region ADO Class Structure

        private static string ADOTopUsing()
        {
            return
        $@"using {AppName}_Data.DTO;
using Microsoft.Data.SqlClient;
using System.Data;
using Utilities;

namespace {AppName}_Data.DataAccess
{{
    public partial class cls{FormattedTNSingle}Data
    {{";
        }

        private static string ADOGetAllMethod()
        {
            return $@"public static async Task<List<{FormattedTNSingle}DTO>> GetAll{FormattedTNPluralize}Async(int pageNumber = 1, int pageSize = 50)
        {{
            List<{FormattedTNSingle}DTO> {FormattedTNPluralizeVar}List = new List<{FormattedTNSingle}DTO>();

            try
            {{
                using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString()))
                using (SqlCommand command = new SqlCommand(""SP_GetAll{FormattedTNPluralize}"", connection))
                {{
                    command.CommandType = CommandType.StoredProcedure;

                    // Add pagination parameters
                    command.Parameters.AddWithValue(""@PageNumber"", pageNumber);
                    command.Parameters.AddWithValue(""@PageSize"", pageSize);

                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {{
                        while (await reader.ReadAsync())
                        {{
                            {FormattedTNPluralizeVar}List.Add(new {FormattedTNSingle}DTO(
{GetReaderAssignments()}
                            ));
                        }}
                    }}
                }}

                return {FormattedTNPluralizeVar}List;
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return new List<{FormattedTNSingle}DTO>();
            }}
        }}

";
        }

        private static string ADOGetByIDMethod()
        {
            return $@"public static async Task<{FormattedTNSingle}DTO> Get{FormattedTNSingle}ByIdAsync(int {TableId.ToLower()})
        {{
            if ({TableId.ToLower()} == null)
            {{
                return null;
            }}

            if ({TableId.ToLower()} <= 0)
                throw new ArgumentException(""{FormattedTNSingle} ID must be greater than zero"", nameof({TableId.ToLower()}));

            try
            {{
                using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString()))
                using (SqlCommand command = new SqlCommand(""SP_Get{FormattedTNSingle}ById"", connection))
                {{
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue(""@{TableId}"", {TableId.ToLower()});
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {{
                        if (await reader.ReadAsync())
                        {{
                            return new {FormattedTNSingle}DTO(
{GetReaderAssignments()}
                            );
                        }}
                        else
                        {{
                            return null;
                        }}
                    }}
                }}
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return null;
            }}
        }}

";
        }

        private static string ADOGetByCountryNameMethod()
        {
            return $@"public static async Task<{FormattedTNSingle}DTO> Get{FormattedTNSingle}ByCountryNameAsync(string CountryName)
        {{
             if (string.IsNullOrWhiteSpace(CountryName))
            {{
                throw new ArgumentException(""Country name must be valid"", nameof(CountryName));
            }}

            try
            {{
                using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString()))
                using (SqlCommand command = new SqlCommand(""SP_Get{FormattedTNSingle}ByCountryName"", connection))
                {{
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue(""@CountryName"", CountryName);
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {{
                        if (await reader.ReadAsync())
                        {{
                            return new {FormattedTNSingle}DTO(
{GetReaderAssignments()}
                            );
                        }}
                        else
                        {{
                            return null;
                        }}
                    }}
                }}
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return null;
            }}
        }}

";
        }

        private static string ADOGetByUsernameAndPasswordMethod()
        {
            return $@"public static async Task<{FormattedTNSingle}DTO> Get{FormattedTNSingle}ByUsernameAndPasswordAsync(string username, string password)
        {{
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException(""Username cannot be empty"", nameof(username));
                
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException(""Password cannot be empty"", nameof(password));

            try
            {{
                using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString()))
                using (SqlCommand command = new SqlCommand(""SP_Get{FormattedTNSingle}ByUsernameAndPassword"", connection))
                {{
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue(""@Username"", username);
                    command.Parameters.AddWithValue(""@Password"", clsSecure.HashPassword(password));
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {{
                        if (await reader.ReadAsync())
                        {{
                            return new {FormattedTNSingle}DTO(
{GetReaderAssignments()}
                            );
                        }}
                        else
                        {{
                            return null;
                        }}
                    }}
                }}
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return null;
            }}
        }}

";
        }

        private static string ADOGetByPersonIDMethod()
        {
            return $@"public static async Task<{FormattedTNSingle}DTO> Get{FormattedTNSingle}ByPersonIdAsync(int personId)
        {{
            if (personId <= 0)
                throw new ArgumentException(""Person ID must be greater than zero"", nameof(personId));

            try
            {{
                using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString()))
                using (SqlCommand command = new SqlCommand(""SP_Get{FormattedTNSingle}ByPersonId"", connection))
                {{
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue(""@PersonId"", personId);
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {{
                        if (await reader.ReadAsync())
                        {{
                            return new {FormattedTNSingle}DTO(
{GetReaderAssignments()}
                            );
                        }}
                        else
                        {{
                            return null;
                        }}
                    }}
                }}
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return null;
            }}
        }}

";
        }

        private static string ADOAddNewMethod()
        {
            return $@"public static async Task<int> Add{FormattedTNSingle}Async({FormattedTNSingle}DTO {FormattedTNSingleVar.ToLower()}DTO)
        {{
            if ({FormattedTNSingleVar.ToLower()}DTO == null)
            {{
                throw new ArgumentNullException(nameof({FormattedTNSingleVar.ToLower()}DTO));
            }}

            if (!_Validate{FormattedTNSingle}({FormattedTNSingleVar.ToLower()}DTO))
            {{
                return -1;
            }}

            try
            {{
                using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString()))
                using (SqlCommand command = new SqlCommand(""SP_Add{FormattedTNSingle}"", connection))
                {{
                    command.CommandType = CommandType.StoredProcedure;

{GetCommandParametersForAddUpdate()}

                    SqlParameter outputIdParam = new SqlParameter(""@New{TableId}"", SqlDbType.Int)
                    {{
                        Direction = ParameterDirection.Output
                    }};
                    command.Parameters.Add(outputIdParam);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();

                    return (int)outputIdParam.Value;
                }}
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return -1;
            }}
        }}

";
        }

        private static string ADOUpdateMethod()
        {
            return $@"public static async Task<bool> Update{FormattedTNSingle}Async({FormattedTNSingle}DTO {FormattedTNSingleVar.ToLower()}DTO)
        {{
            if ({FormattedTNSingleVar.ToLower()}DTO == null)
            {{
                throw new ArgumentNullException(nameof({FormattedTNSingleVar.ToLower()}DTO));
            }}

            if ({FormattedTNSingleVar.ToLower()}DTO.Id <= 0)
            {{
                throw new ArgumentException(""{FormattedTNSingle} ID must be greater than zero"", nameof({FormattedTNSingleVar.ToLower()}DTO.Id));
            }}

            _Validate{FormattedTNSingle}({FormattedTNSingleVar.ToLower()}DTO);

            try
            {{
                using (var connection = new SqlConnection(clsDataAccessSettings.ConnectionString()))
                using (var command = new SqlCommand(""SP_Update{FormattedTNSingle}"", connection))
                {{
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue(""@{TableId}"", {FormattedTNSingleVar.ToLower()}DTO.Id);
{GetCommandParametersForAddUpdate()}

                    await connection.OpenAsync();
                    return await command.ExecuteNonQueryAsync() == 1;
                }}
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return false;
            }}
        }}

";
        }

        private static string ADODeleteMethod()
        {
            return $@"public static async Task<bool> Delete{FormattedTNSingle}Async(int {TableId.ToLower()})
        {{
            if ({TableId.ToLower()} <= 0)
                throw new ArgumentException(""{FormattedTNSingle} ID must be greater than zero"", nameof({TableId.ToLower()}));

            try
            {{
                using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString()))
                using (SqlCommand command = new SqlCommand(""SP_Delete{FormattedTNSingle}"", connection))
                {{
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue(""@{TableId}"", {TableId.ToLower()});

                    await connection.OpenAsync();
                    int rowsAffected = (int)await command.ExecuteScalarAsync();

                    return rowsAffected == 1;
                }}
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return false;
            }}
        }}

";
        }

        private static string ADOIsExistMethod()
        {
            return $@"public static async Task<bool> Is{FormattedTNSingle}ExistsAsync(int {TableId.ToLower()})
        {{
            if ({TableId.ToLower()} <= 0)
                throw new ArgumentException(""{FormattedTNSingle} ID must be greater than zero"", nameof({TableId.ToLower()}));

            try
            {{
                using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString()))
                using (SqlCommand command = new SqlCommand(""SP_Is{FormattedTNSingle}Exists"", connection))
                {{
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue(""@{TableId}"", {TableId.ToLower()});
                    await connection.OpenAsync();

                    object result = await command.ExecuteScalarAsync();
                    return (result != null && Convert.ToInt32(result) == 1);
                }}
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return false;
            }}
        }}

";
        }

        private static string ADOIsExistByUsernameMethod()
        {
            return $@"public static async Task<bool> Is{FormattedTNSingle}ExistsByUsernameAsync(string username)
        {{
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException(""Username cannot be empty"", nameof(username));

            try
            {{
                using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString()))
                using (SqlCommand command = new SqlCommand(""SP_Is{FormattedTNSingle}ExistsByUsername"", connection))
                {{
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue(""@Username"", username);
                    await connection.OpenAsync();

                    object result = await command.ExecuteScalarAsync();
                    return (result != null && Convert.ToInt32(result) == 1);
                }}
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return false;
            }}
        }}

";
        }

        private static string ADOIsExistByPersonIdMethod()
        {
            return $@"public static async Task<bool> Is{FormattedTNSingle}ExistsByPersonIdAsync(int personId)
        {{
            if (personId <= 0)
                throw new ArgumentException(""Person ID must be greater than zero"", nameof(personId));

            try
            {{
                using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString()))
                using (SqlCommand command = new SqlCommand(""SP_Is{FormattedTNSingle}ExistsByPersonId"", connection))
                {{
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue(""@PersonId"", personId);
                    await connection.OpenAsync();

                    object result = await command.ExecuteScalarAsync();
                    return (result != null && Convert.ToInt32(result) == 1);
                }}
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return false;
            }}
        }}

";
        }

        private static string ADOCountMethod()
        {
            return $@"public static async Task<int> {FormattedTNPluralize}CountAsync()
        {{
            try
            {{
                using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString()))
                using (SqlCommand command = new SqlCommand(""SP_{FormattedTNPluralize}Count"", connection))
                {{
                    command.CommandType = CommandType.StoredProcedure;
                    await connection.OpenAsync();

                    object result = await command.ExecuteScalarAsync();
                    if (result != DBNull.Value)
                    {{
                        return Convert.ToInt32(result);
                    }}
                    else
                    {{
                        return 0;
                    }}
                }}
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return 0;
            }}
        }}

";
        }

        private static string ADOValidationMethod()
        {
            var sb = new StringBuilder();
            sb.AppendLine($@"private static bool _Validate{FormattedTNSingle}({FormattedTNSingle}DTO {FormattedTNSingleVar.ToLower()})
        {{");

            // Add validation logic for required fields
            foreach (var column in columns)
            {
                if (!column.IsNullable && !column.IsIdentity && column.Name.ToLower() != TableId.ToLower())
                {
                    string propertyName = clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name));
                    string csharpType = clsUtil.ConvertDbTypeToCSharpType(column.DataType);

                    if (csharpType == "string")
                    {
                        sb.AppendLine($@"            if (string.IsNullOrWhiteSpace({FormattedTNSingleVar.ToLower()}.{propertyName}))
            {{
                throw new ArgumentException(""{propertyName} is required"", nameof({FormattedTNSingleVar.ToLower()}.{propertyName}));
            }}");
                    }
                    else if (csharpType == "DateTime")
                    {
                        sb.AppendLine($@"            if ({FormattedTNSingleVar.ToLower()}.{propertyName} > DateTime.Now)
            {{
                throw new ArgumentException(""{propertyName} cannot be in the future"", nameof({FormattedTNSingleVar.ToLower()}.{propertyName}));
            }}

            if ({FormattedTNSingleVar.ToLower()}.{propertyName} < DateTime.Now.AddYears(-150))
            {{
                throw new ArgumentException(""{propertyName} is too far in the past"", nameof({FormattedTNSingleVar.ToLower()}.{propertyName}));
            }}");
                    }
                    else if (csharpType == "int" || csharpType == "decimal" || csharpType == "double" || csharpType == "float")
                    {
                        sb.AppendLine($@"            if ({FormattedTNSingleVar.ToLower()}.{propertyName} <= 0)
            {{
                throw new ArgumentException(""{propertyName} must be greater than zero"", nameof({FormattedTNSingleVar.ToLower()}.{propertyName}));
            }}");
                    }
                }
            }

            sb.AppendLine(@"
            return true;
        }");

            return sb.ToString();
        }

        private static string ADOClosing()
        {
            return $@"    }}
}}";
        }

        #endregion

        #region ADO Support Methods
        private static string GetReaderAssignments()
        {
            var sb = new StringBuilder();
            bool firstLine = true;

            foreach (var column in columns)
            {
                if (!firstLine)
                {
                    sb.AppendLine(",");
                }
                else
                {
                    firstLine = false;
                }

                sb.Append($"                                {GetReaderMethod(clsUtil.ConvertDbTypeToCSharpType(column.DataType))}(reader.GetOrdinal(\"{column.Name}\"))");
            }

            return sb.ToString();
        }

        private static string GetCommandParametersForAddUpdate()
        {
            var sb = new StringBuilder();

            foreach (var column in columns)
            {
                if (column.IsIdentity)
                {
                    continue;
                }

                string propertyName = clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name));

                if (column.IsNullable)
                {
                    sb.AppendLine($@"                    command.Parameters.AddWithValue(""@{propertyName}"", {FormattedTNSingleVar.ToLower()}DTO.{propertyName} ?? (object)DBNull.Value);");
                }
                else
                {
                    sb.AppendLine($@"                    command.Parameters.AddWithValue(""@{propertyName}"", {FormattedTNSingleVar.ToLower()}DTO.{propertyName});");
                }

            }

            return sb.ToString();
        }

        private static string GetReaderMethod(string csharpType)
        {
            switch (csharpType)
            {
                case "string":
                    return "reader.GetString";
                case "int":
                    return "reader.GetInt32";
                case "decimal":
                    return "reader.GetDecimal";
                case "double":
                    return "reader.GetDouble";
                case "float":
                    return "reader.GetFloat";
                case "DateTime":
                    return "reader.GetDateTime";
                case "bool":
                    return "reader.GetBoolean";
                case "byte[]":
                    return "reader.GetBytes";
                case "byte":
                    return "reader.GetByte";
                case "short":
                    return "reader.GetInt16";
                default:
                    return "reader.GetValue";
            }
        }

        #endregion

        public static bool GenerateAdoDalCode(string tableName, string? folderPath = null)
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
                folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Code Generator\\{AppName}\\DataAccess\\Basic\\");
            }

            StringBuilder dalCode = new StringBuilder();

            dalCode.Append(ADOTopUsing());
            dalCode.Append(ADOGetByIDMethod());

            if (clsFormat.Singularize(_TableName.ToLower()) == "user")
            {
                dalCode.Append(ADOGetByUsernameAndPasswordMethod());
                dalCode.Append(ADOGetByPersonIDMethod());
            }

            if (clsFormat.Singularize(_TableName.ToLower()) == "country")
            {
                dalCode.Append(ADOGetByCountryNameMethod());
            }

            dalCode.Append(ADOAddNewMethod());
            dalCode.Append(ADOUpdateMethod());
            dalCode.Append(ADOGetAllMethod());
            dalCode.Append(ADOIsExistMethod());

            if (clsFormat.Singularize(_TableName.ToLower()) == "user")
            {
                dalCode.Append(ADOIsExistByUsernameMethod());
                dalCode.Append(ADOIsExistByPersonIdMethod());
            }

            dalCode.Append(ADOCountMethod());
            dalCode.Append(ADODeleteMethod());
            dalCode.Append(ADOValidationMethod());
            dalCode.Append(ADOClosing());

            return clsFile.StoreToFile(dalCode.ToString(), $"cls{FormattedTNSingle}Data.cs", folderPath, true);
        }

        #endregion

        #region EF Code

        #region EF Support Methods

        #region For GetByID

        private static string tResultsForGetByID()
        {
            var columnStrings = columns.Select(column =>
            {
                string dataType = clsUtil.ConvertDbTypeToCSharpType(column.DataType);
                string isNullable = column.IsNullable ? "?" : string.Empty;
                return $"{dataType}{isNullable} {clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name))}";
            });

            return string.Join(", ", columnStrings);
        }

        private static string InfoForGetByID()
        {
            if (columns == null || !columns.Any())
            {
                return string.Empty;
            }

            var resultBuilder = new StringBuilder();

            foreach (var column in columns)
            {
                string formattedName = clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name));
                string csharpType = clsUtil.ConvertDbTypeToCSharpType(column.DataType);

                if (column.IsNullable)
                {
                    resultBuilder.AppendLine($"{formattedName} = x.{formattedName} != null ? x.{formattedName} : null,");
                }
                else
                {
                    resultBuilder.AppendLine($"x.{formattedName},");
                }
            }

            if (resultBuilder.Length > 0)
            {
                resultBuilder.Length -= Environment.NewLine.Length + 1;
            }

            return resultBuilder.ToString();
        }

        private static string returnsForGetByID()
        {
            if (columns == null || !columns.Any())
            {
                return string.Empty;
            }

            var resultBuilder = new StringBuilder();

            foreach (var column in columns)
            {
                string formattedName = clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name));
                resultBuilder.AppendLine($"{FormattedTNSingleVar.ToLower()}Info.{formattedName},");
            }

            if (resultBuilder.Length > 0)
            {
                resultBuilder.Length -= Environment.NewLine.Length + 1;
            }

            return resultBuilder.ToString();
        }

        #endregion

        #region For AddNew

        private static string ParamatersForAddNew()
        {
            var columnStrings = columns
                .Where(column => !column.IsIdentity)
                .Select(column =>
                {
                    string dataType = clsUtil.ConvertDbTypeToCSharpType(column.DataType);
                    string isNullable = column.IsNullable ? "?" : string.Empty;
                    return $"{dataType}{isNullable} {clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name))}";
                });

            return string.Join(", ", columnStrings);
        }

        private static string ObjectForAddNew()
        {
            if (columns == null || !columns.Any())
            {
                return string.Empty;
            }

            var resultBuilder = new StringBuilder();

            foreach (var column in columns)
            {
                string formattedName = clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name));

                if (column.IsPrimaryKey)
                {
                    continue;
                }
                if (formattedName.ToLower() == "password")
                {
                    resultBuilder.AppendLine($"{formattedName} = clsSecure.HashPassword({formattedName}),");
                    continue;
                }
                resultBuilder.AppendLine($"{formattedName} = {formattedName},");
            }

            if (resultBuilder.Length > 0)
            {
                resultBuilder.Length -= Environment.NewLine.Length + 1;
            }

            return resultBuilder.ToString();
        }

        #endregion

        #region For Update

        private static string ParamatersForUpdate()
        {
            return $"int {TableId},{ParamatersForAddNew()}";
        }

        private static string ObjectForUpdate()
        {
            if (columns == null || !columns.Any())
            {
                return string.Empty;
            }

            var resultBuilder = new StringBuilder();
            bool firstLine = true;

            foreach (var column in columns)
            {
                if (column.IsPrimaryKey)
                {
                    continue;
                }

                string formattedName = clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name));

                if (!firstLine)
                {
                    resultBuilder.AppendLine();
                }
                else
                {
                    firstLine = false;
                }

                if (formattedName.ToLower() == "password")
                {
                    resultBuilder.Append($"existing{FormattedTNSingle}.{formattedName} = clsSecure.HashPassword({formattedName});");
                    continue;
                }

                resultBuilder.Append($"existing{FormattedTNSingle}.{formattedName} = {formattedName};");
            }

            return resultBuilder.ToString();
        }

        #endregion

        #endregion

        #region EF Class Structure

        private static string EFTopUsing()
        {
            return
$@"using {AppName}_Data.Data;
using {AppName}_Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Utilities;

namespace {AppName}_Data.DataAccess
{{
    public partial class cls{FormattedTNSingle}Data
    {{";
        }

        private static string EFGetAllMethod()
        {
            return $@"public static async Task<List<{FormattedTNSingle}>> GetAll{FormattedTNPluralize}Async(int pageNumber = 1, int pageSize = 50)
        {{
            try
            {{
                await using var context = new AppDbContext();
                var query = context.{FormattedTNPluralize}.AsNoTracking();

                var {FormattedTNPluralizeVar} = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync().ConfigureAwait(false);

                return ({FormattedTNPluralizeVar});
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                throw;
            }}
        }}

";
        }

        private static string EFGetByIDMethod()
        {
            return $@"public static async Task<({tResultsForGetByID()})?> Get{FormattedTNSingle}InfoByIdAsync(int {TableId.ToLower()})
{{
    if ({TableId.ToLower()} <= 0)
    {{
        throw new ArgumentException(""{FormattedTNSingle} ID must be greater than 0."", nameof({TableId.ToLower()}));
    }}

    try
    {{
        await using var context = new AppDbContext();

        var {FormattedTNSingleVar.ToLower()}Info = await context.{FormattedTNPluralize}
            .Where(x => x.{TableId} == {TableId.ToLower()})
            .Select(x => new
            {{
                {InfoForGetByID()}             
            }})
            .AsNoTracking()
            .SingleOrDefaultAsync()
            .ConfigureAwait(false);

        if ({FormattedTNSingleVar.ToLower()}Info == null)
        {{
            return null;
        }}

        return (
{returnsForGetByID()}
);
    }}
    catch (Exception ex)
    {{
        clsUtil.ErrorLogger(ex);
        throw;
    }}
}}

";
        }

        private static string EFGetByUsernameAndPasswordMethod()
        {
            return $@"public static async Task<({tResultsForGetByID()})?> Get{FormattedTNSingle}InfoByUsernameAndPasswordAsync(string username, string password)
{{
         if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(username))
            {{
                throw new ArgumentException(""Username and Password must be declared."");
            }}

    try
    {{
        await using var context = new AppDbContext();

        var {FormattedTNSingleVar.ToLower()}Info = await context.{FormattedTNPluralize}.Where(x => x.Username == username && clsSecure.VerifyPassword(password, x.Password))
            .Select(x => new
            {{
                {InfoForGetByID()}             
            }})
            .AsNoTracking()
            .SingleOrDefaultAsync()
            .ConfigureAwait(false);

        if ({FormattedTNSingleVar.ToLower()}Info == null)
        {{
            return null;
        }}

        return (
{returnsForGetByID()}
);
    }}
    catch (Exception ex)
    {{
        clsUtil.ErrorLogger(ex);
        throw;
    }}
}}

";
        }

        private static string EFGetByPersonIDMethod()
        {
            return $@"public static async Task<({tResultsForGetByID()})?> Get{FormattedTNSingle}InfoByPersonIdAsync(int personid)
{{
    if (personid <= 0)
    {{
        throw new ArgumentException(""Person ID must be greater than 0."", nameof(personid));
    }}

    try
    {{
        await using var context = new AppDbContext();

        var {FormattedTNSingleVar.ToLower()}Info = await context.{FormattedTNPluralize}
            .Where(x => x.PersonId == personid)
            .Select(x => new
            {{
                {InfoForGetByID()}             
            }})
            .AsNoTracking()
            .SingleOrDefaultAsync()
            .ConfigureAwait(false);

        if ({FormattedTNSingleVar.ToLower()}Info == null)
        {{
            return null;
        }}

        return (
{returnsForGetByID()}
);
    }}
    catch (Exception ex)
    {{
        clsUtil.ErrorLogger(ex);
        throw;
    }}
}}

";
        }

        private static string EFAddNewMethod()
        {
            return $@"public static async Task<int> AddNew{FormattedTNSingle}Async({ParamatersForAddNew()})
        {{
            try
            {{
                {FormattedTNSingle} new{FormattedTNSingle} = new {FormattedTNSingle}
                {{
{ObjectForAddNew()}   
                }};

                await using var context = new AppDbContext();
                await context.{FormattedTNPluralize}.AddAsync(new{FormattedTNSingle}).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);

                return new{FormattedTNSingle}.{TableId};
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return -1;
            }}
        }}

";
        }

        private static string EFUpdateMethod()
        {
            return $@"public static async Task<bool> Update{FormattedTNSingle}Async({ParamatersForUpdate()})
        {{
            if ({TableId} <= 0)
            {{
                return false;
            }}

            try
            {{
                await using var context = new AppDbContext();
                var existing{FormattedTNSingle} = await context.{FormattedTNPluralize}.SingleOrDefaultAsync(x => x.{TableId} == {TableId}).ConfigureAwait(false);

                if (existing{FormattedTNSingle} == null)
                {{
                    return false;
                }}

{ObjectForUpdate()}               

                await context.SaveChangesAsync().ConfigureAwait(false);
                return true;
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return false;
            }}
        }}

";
        }

        private static string EFDeleteMethod()
        {
            return $@"public static async Task<bool> Delete{FormattedTNSingle}Async(int {TableId})
        {{
            if ({TableId} <= 0)
            {{
                return false;
            }}

            try
            {{
                await using var context = new AppDbContext();
                {FormattedTNSingle}? {FormattedTNSingleVar} = await context.{FormattedTNPluralize}.SingleOrDefaultAsync(x => x.{TableId} == {TableId}).ConfigureAwait(false);

                if ({FormattedTNSingleVar} == null)
                {{
                    return false;
                }}

                context.{FormattedTNPluralize}.Remove({FormattedTNSingleVar});
                await context.SaveChangesAsync().ConfigureAwait(false);

                return true;
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return false;
            }}
        }}

";
        }

        private static string EFIsExistMethod()
        {
            return $@"public static async Task<bool> Is{FormattedTNSingle}ExistsAsync(int {TableId})
        {{
            if ({TableId} <= 0)
            {{
                return false;
            }}

            const string sqlQuery = ""EXEC SP_Is{FormattedTNSingle}Exists @{TableId} = @{TableId}"";

            try
            {{
                await using var context = new AppDbContext();
                int result = await context.Database.SqlQueryRaw<int>(sqlQuery, new SqlParameter(""@{TableId}"", {TableId})).SingleOrDefaultAsync().ConfigureAwait(false);

                return result == 1;
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return false;
            }}
        }}

";
        }

        private static string EFIsExistByUsernameMethod()
        {
            return $@"public static async Task<bool> Is{FormattedTNSingle}ExistsAsync(string UserName)
        {{
             if (string.IsNullOrWhiteSpace(UserName))
            {{
                return false;
            }}

  const string sqlQuery = ""EXEC SP_CheckUserExistsByUserName @UserName = @UserName"";

            try
            {{
                await using var context = new AppDbContext();
                int result = await context.Database.SqlQueryRaw<int>(sqlQuery, new SqlParameter(""@UserName"", UserName)).SingleOrDefaultAsync().ConfigureAwait(false);

                return result == 1;
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return false;
            }}
        }}

";
        }

        private static string EFIsExistByPersonIdMethod()
        {
            return $@"public static async Task<bool> Is{FormattedTNSingle}ExistsByPersonIdAsync(int personId)
        {{
            if (personId <= 0)
            {{
                return false;
            }}

            const string sqlQuery = ""EXEC SP_CheckUserExistsByPersonID @personId = @personId"";

            try
            {{
                await using var context = new AppDbContext();
                int result = await context.Database.SqlQueryRaw<int>(sqlQuery, new SqlParameter(""@personId"", personId)).SingleOrDefaultAsync().ConfigureAwait(false);

                return result == 1;
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return false;
            }}
        }}

";
        }

        private static string EFClosing()
        {
            return $@"}}}}";
        }

        #endregion

        public static bool GenerateEFDalCode(string tableName, string? folderPath = null)
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
                folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Code Generator\\{AppName}\\DataAccess\\Basic\\");
            }

            StringBuilder dalCode = new StringBuilder();

            dalCode.Append(EFTopUsing() + EFGetAllMethod() + EFGetByIDMethod());

            if (clsFormat.Singularize(_TableName.ToLower()) == "user")
            {
                dalCode.Append(EFGetByPersonIDMethod() + EFGetByUsernameAndPasswordMethod());
            }

            dalCode.Append(EFAddNewMethod() + EFUpdateMethod() + EFDeleteMethod() + EFIsExistMethod());

            if (clsFormat.Singularize(_TableName.ToLower()) == "user")
            {
                dalCode.Append(EFIsExistByUsernameMethod() + EFIsExistByPersonIdMethod());
            }

            dalCode.Append(EFClosing());

            string fileName = $"cls{FormattedTNSingle}Data.cs";
            return clsFile.StoreToFile(dalCode.ToString(), fileName, folderPath, true);
        }

        #endregion

        public static bool GenerateDalCode(string tableName, enCodeStyle codeStyle = enCodeStyle.AdoStyle, string? folderPath = null)
        {
            return (codeStyle == enCodeStyle.AdoStyle ? GenerateAdoDalCode(tableName, folderPath) : GenerateEFDalCode(tableName, folderPath)) && GenerateAllSPs() && GenerateDTO(tableName, folderPath);
        }

    }

}