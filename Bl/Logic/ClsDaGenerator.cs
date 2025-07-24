using System.Text;
using Utilities;

namespace CodeGenerator.Bl
{
    public class ClsDaGenerator : ClsGenerator
    {
        public ClsDaGenerator(string tableName) : base(tableName)
        {

        }

        #region  Class Structure

        private static string EFContextConstrctor()
        {
            return @$"
{ContextName} ctx;
        public {DataClsName}({ContextName} context)
        {{
            ctx = context;
        }}
";
        }

        private static string IsExistByUsernameMethod(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.Ado:
                    return $@"public static async Task<bool> Is{FormattedTNSingle}ExistsByUsernameAsync(string username)
        {{
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException(""Username cannot be empty"", nameof(username));

            try
            {{
                using (SqlConnection connection = new SqlConnection(DataAccessSettings.ConnectionString()))
                using (SqlCommand command = new SqlCommand(""SP_Is{FormattedTNSingle}ExistsByUsername"", connection))
                {{
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue(""@Username"", username);
                    await connection.OpenAsync();

                    var returnParameter = command.Parameters.Add(""@ReturnVal"", SqlDbType.{GetSqlDbType()});
                    returnParameter.Direction = ParameterDirection.ReturnValue;

                    await command.ExecuteNonQueryAsync();

                    return ({TableIdDT})returnParameter.Value == 1;
                }}
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return false;
            }}
        }}

";
                case enCodeStyle.EF:
                    return $@"public async Task<bool> {MethodNames.IsExists}(string UserName)
        {{
             if (string.IsNullOrWhiteSpace(UserName))
            {{
                return false;
            }}

  const string sqlQuery = ""EXEC SP_CheckUserExistsByUserName @UserName = @UserName"";

            try
            {{
                {TableIdDT} result = await ctx.Database.SqlQueryRaw<{TableIdDT}>(sqlQuery, new SqlParameter(""@UserName"", UserName)).SingleOrDefaultAsync().ConfigureAwait(false);

                return result == 1;
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return false;
            }}
        }}

";
                default:
                    throw new ArgumentException("Invalid code style specified.");
            }


        }

        private static string IsExistByPersonIdMethod(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.Ado:
                    return $@"public static async Task<bool> Is{FormattedTNSingle}ExistsByPersonIdAsync({TableIdDT} personId)
        {{
            if (personId <= 0)
                throw new ArgumentException(""Person ID must be greater than zero"", nameof(personId));

            try
            {{
                using (SqlConnection connection = new SqlConnection(DataAccessSettings.ConnectionString()))
                using (SqlCommand command = new SqlCommand(""SP_Is{FormattedTNSingle}ExistsByPersonId"", connection))
                {{
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue(""@PersonId"", personId);
                    await connection.OpenAsync();

                    var returnParameter = command.Parameters.Add(""@ReturnVal"", SqlDbType.{GetSqlDbType()});
                    returnParameter.Direction = ParameterDirection.ReturnValue;

                    await command.ExecuteNonQueryAsync();

                    return ({TableIdDT})returnParameter.Value == 1;
                }}
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return false;
            }}
        }}

";
                case enCodeStyle.EF:
                    return $@"public async Task<bool> IsExistsByPersonIdAsync({TableIdDT} personId)
        {{
            if (personId <= 0)
            {{
                return false;
            }}

            const string sqlQuery = ""EXEC SP_CheckUserExistsByPersonID @personId = @personId"";

            try
            {{
                {TableIdDT} result = await ctx.Database.SqlQueryRaw<{TableIdDT}>(sqlQuery, new SqlParameter(""@personId"", personId)).SingleOrDefaultAsync().ConfigureAwait(false);

                return result == 1;
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return false;
            }}
        }}

";
                default:
                    throw new ArgumentException("Invalid code style specified.");
            }
        }

        private static string TopUsing(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.Ado:
                    return
   $@"using {AppName}.DTO;
using Microsoft.Data.SqlClient;
using System.Data;
using Utilities;

namespace {AppName}.Da
{{
    public partial class {DataClsName}
    {{";
                case enCodeStyle.EF:
                    return
$@"using {AppName}.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Utilities;

namespace {AppName}.Da
{{
    public class {DataClsName} : {DataInterfaceName}
    {{";
                default:
                    throw new ArgumentException("Invalid code style specified.");
            }
        }

        private static string GetAllMethod(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.Ado:
                    return $@"public static async Task<List<DtoClsName>> GetAll{FormattedTNPluralize}Async(int pageNumber = 1, int pageSize = 50)
        {{
            List<DtoClsName> {FormattedTNPluralizeVar}List = new List<DtoClsName>();

            try
            {{
                using (SqlConnection connection = new SqlConnection(DataAccessSettings.ConnectionString()))
                using (SqlCommand command = new SqlCommand(""SP_GetAll{FormattedTNPluralize}"", connection))
                {{
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue(""@PageNumber"", pageNumber);
                    command.Parameters.AddWithValue(""@PageSize"", pageSize);

                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {{
                        while (await reader.ReadAsync())
                        {{
                            {FormattedTNPluralizeVar}List.Add(new DtoClsName(
{GetReaderAssignments()}
                            ));
                        }}
                    }}
                }}

                return {FormattedTNPluralizeVar}List;
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return new List<DtoClsName>();
            }}
        }}

";
                case enCodeStyle.EF:
                    return $@"{ImplementationMethod(MethodNames.GetAll)}
        {{
            try
            {{              
                var query = ctx.{ModelNameProp}.AsNoTracking();

                var {FormattedTNPluralizeVar} = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync().ConfigureAwait(false);

                return ({FormattedTNPluralizeVar});
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                throw;
            }}
        }}

";
                default:
                    throw new ArgumentException("Invalid code style specified.");
            }
        }

        private static string GetByIDMethod(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.Ado:
                    return $@"public static async Task<DtoClsName> Get{FormattedTNSingle}ByIdAsync({TableIdDT} {TableId.ToLower()})
        {{
            if ({TableId.ToLower()} == null)
            {{
                return null;
            }}

            if ({TableId.ToLower()} <= 0)
                throw new ArgumentException(""{FormattedTNSingle} ID must be greater than zero"", nameof({TableId.ToLower()}));

            try
            {{
                using (SqlConnection connection = new SqlConnection(DataAccessSettings.ConnectionString()))
                using (SqlCommand command = new SqlCommand(""SP_Get{FormattedTNSingle}ById"", connection))
                {{
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue(""@{TableId}"", {TableId.ToLower()});
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {{
                        if (await reader.ReadAsync())
                        {{
                            return new DtoClsName(
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
                Helper.ErrorLogger(ex);
                return null;
            }}
        }}

";
                case enCodeStyle.EF:
                    return $@"{ImplementationMethod(MethodNames.GetById)}
{{
    if (id <= 0)
    {{
        throw new ArgumentException(""{FormattedTNSingle} ID must be greater than 0."", nameof(id));
    }}

    try
    {{
    var {FormattedTNSingleVar} = ctx.{ModelNameProp}.FirstOrDefault(a => a.{FormattedTableId} == id);
                return {FormattedTNSingleVar};
    }}
    catch (Exception ex)
    {{
        Helper.ErrorLogger(ex);
        throw;
    }}
}}

";
                default: throw new ArgumentException("Invalid code style specified.");
            }
        }

        private static string ADOGetByCountryNameMethod()
        {
            return $@"public static async Task<DtoClsName> Get{FormattedTNSingle}ByCountryNameAsync(string CountryName)
        {{
             if (string.IsNullOrWhiteSpace(CountryName))
            {{
                throw new ArgumentException(""Country name must be valid"", nameof(CountryName));
            }}

            try
            {{
                using (SqlConnection connection = new SqlConnection(DataAccessSettings.ConnectionString()))
                using (SqlCommand command = new SqlCommand(""SP_Get{FormattedTNSingle}ByCountryName"", connection))
                {{
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue(""@CountryName"", CountryName);
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {{
                        if (await reader.ReadAsync())
                        {{
                            return new DtoClsName(
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
                Helper.ErrorLogger(ex);
                return null;
            }}
        }}

";
        }

        private static string GetByUsernameAndPasswordMethod(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.EF:
                    return $@"public async Task<({tResultsForGetByID()})?> GetByUsernameAndPasswordAsync(string username, string password)
{{
         if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(username))
            {{
                throw new ArgumentException(""Username and Password must be declared."");
            }}

    try
    {{
        var {FormattedTNSingleVar.ToLower()}Info = await ctx.{FormattedTNPluralize}.Where(x => x.Username == username && SecurityHelper.VerifyPassword(password, x.Password))
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
        Helper.ErrorLogger(ex);
        throw;
    }}
}}

";
                case enCodeStyle.Ado:
                    return $@"public static async Task<DtoClsName> Get{FormattedTNSingle}ByUsernameAndPasswordAsync(string username, string password)
        {{
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException(""Username cannot be empty"", nameof(username));
                
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException(""Password cannot be empty"", nameof(password));

            try
            {{
                using (SqlConnection connection = new SqlConnection(DataAccessSettings.ConnectionString()))
                using (SqlCommand command = new SqlCommand(""SP_Get{FormattedTNSingle}ByUsernameAndPassword"", connection))
                {{
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue(""@Username"", username);
                    command.Parameters.AddWithValue(""@Password"", SecurityHelper.HashPassword(password));
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {{
                        if (await reader.ReadAsync())
                        {{
                            return new DtoClsName(
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
                Helper.ErrorLogger(ex);
                return null;
            }}
        }}

";
                default:
                    {
                        throw new ArgumentException("Invalid code style specified.");
                    }
            }
        }

        private static string GetByPersonIDMethod(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.EF:
                    return $@"public async Task<({tResultsForGetByID()})?> GetByPersonIdAsync({TableIdDT} personid)
{{
    if (personid <= 0)
    {{
        throw new ArgumentException(""Person ID must be greater than 0."", nameof(personid));
    }}

    try
    {{
        var {FormattedTNSingleVar.ToLower()}Info = await ctx.{FormattedTNPluralize}
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
        Helper.ErrorLogger(ex);
        throw;
    }}
}}

";
                case enCodeStyle.Ado:
                    return $@"public static async Task<DtoClsName> Get{FormattedTNSingle}ByPersonIdAsync({TableIdDT} personId)
        {{
            if (personId <= 0)
                throw new ArgumentException(""Person ID must be greater than zero"", nameof(personId));

            try
            {{
                using (SqlConnection connection = new SqlConnection(DataAccessSettings.ConnectionString()))
                using (SqlCommand command = new SqlCommand(""SP_Get{FormattedTNSingle}ByPersonId"", connection))
                {{
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue(""@PersonId"", personId);
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {{
                        if (await reader.ReadAsync())
                        {{
                            return new DtoClsName(
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
                Helper.ErrorLogger(ex);
                return null;
            }}
        }}

";
                default:
                    {
                        throw new ArgumentException("Invalid code style specified.");
                    }
            }
        }

        private static string AddNewMethod(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.Ado:
                    return $@"public static async Task<{TableIdDT}> Add{FormattedTNSingle}Async(DtoClsName {FormattedTNSingleVar.ToLower()}DTO)
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
                using (SqlConnection connection = new SqlConnection(DataAccessSettings.ConnectionString()))
                using (SqlCommand command = new SqlCommand(""SP_Add{FormattedTNSingle}"", connection))
                {{
                    command.CommandType = CommandType.StoredProcedure;

{GetCommandParametersForAddUpdate()}

                    SqlParameter outputIdParam = new SqlParameter(""@New{TableId}"", SqlDbType.{GetSqlDbType()})
                    {{
                        Direction = ParameterDirection.Output
                    }};
                    command.Parameters.Add(outputIdParam);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();

                    return ({TableIdDT})outputIdParam.Value;
                }}
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return -1;
            }}
        }}

";
                case enCodeStyle.EF:
                    return $@"{ImplementationMethod(MethodNames.Add)}
        {{
 {FormattedTNSingleVar}.{FormattedTableId} = 0;
            try
            {{
                await ctx.{ModelNameProp}.AddAsync({FormattedTNSingleVar}).ConfigureAwait(false);
                await ctx.SaveChangesAsync().ConfigureAwait(false);
                return {FormattedTNSingleVar}.{FormattedTableId};
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return -1;
            }}
        }}

";
                default:
                    throw new ArgumentException("Invalid code style specified.");
            }
        }

        private static string UpdateMethod(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.Ado:
                    return $@"public static async Task<bool> Update{FormattedTNSingle}Async(DtoClsName {FormattedTNSingleVar.ToLower()}DTO)
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
                using (var connection = new SqlConnection(DataAccessSettings.ConnectionString()))
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
                Helper.ErrorLogger(ex);
                return false;
            }}
        }}

";
                case enCodeStyle.EF:
                    return $@"{ImplementationMethod(MethodNames.Update)}
        {{
           if ({FormattedTNSingleVar}.{FormattedTableId} <= 0)
            {{
                return false;
            }}

            try
            {{
                ctx.Entry({FormattedTNSingleVar}).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                await ctx.SaveChangesAsync().ConfigureAwait(false);
                return true;
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return false;
            }}
        }}

";
                default:
                    throw new ArgumentException("Invalid code style specified.");
            }
        }

        private static string DeleteMethod(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.EF:
                    return $@"{ImplementationMethod(MethodNames.Delete)}
        {{
            if (id <= 0)
            {{
                return false;
            }}

            try
            {{
 {ModelName}? {FormattedTNSingleVar} = await {MethodNames.GetById}(id);
              

                if ({FormattedTNSingleVar} == null)
                {{
                    return false;
                }}

                  {FormattedTNSingleVar}.IsDeleted = true;
                await ctx.SaveChangesAsync().ConfigureAwait(false);
       

                return true;
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return false;
            }}
        }}

";
                case enCodeStyle.Ado:
                    return $@"public static async Task<bool> Delete{FormattedTNSingle}Async({TableIdDT} {TableId.ToLower()})
        {{
            if ({TableId.ToLower()} <= 0)
                throw new ArgumentException(""{FormattedTNSingle} ID must be greater than zero"", nameof({TableId.ToLower()}));

            try
            {{
                using (SqlConnection connection = new SqlConnection(DataAccessSettings.ConnectionString()))
                using (SqlCommand command = new SqlCommand(""SP_Delete{FormattedTNSingle}"", connection))
                {{
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue(""@{TableId}"", {TableId.ToLower()});

                    await connection.OpenAsync();
                    {TableIdDT} rowsAffected = ({TableIdDT})await command.ExecuteScalarAsync();

                    return rowsAffected == 1;
                }}
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return false;
            }}
        }}

";
                default:
                    throw new ArgumentException("Invalid code style specified.");
            }
        }

        private static string IsExistMethod(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.EF:
                    {
                        if (!new ClsSPGenerator(TableName).IsExistsSP())
                        {
                            Helper.ErrorLogger(new Exception("Faild To Create IsExists Stored Procedure"));
                        }

                        return $@"               {ImplementationMethod(MethodNames.IsExists)}
        {{
            if (id <= 0)
            {{
                return false;
            }}

            var parameter = new SqlParameter(""@{TableId}"", id);

            var returnParameter = new SqlParameter
            {{
                ParameterName = ""@ReturnVal"",
                SqlDbType = System.Data.SqlDbType.{GetSqlDbType()},
                Direction = System.Data.ParameterDirection.Output
            }};

            try
            {{
                await ctx.Database.ExecuteSqlRawAsync(""EXEC @ReturnVal = [dbo].[SP_Is{FormattedTNSingle}Exists] @{TableId}"", returnParameter, parameter);
                return ({TableIdDT})returnParameter.Value == 1;
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return false;
            }}
        }}

";
                    }
                case enCodeStyle.Ado:
                    return $@"public static async Task<bool> Is{FormattedTNSingle}ExistsAsync({TableIdDT} {TableId.ToLower()})
        {{
            if ({TableId.ToLower()} <= 0)
                throw new ArgumentException(""{FormattedTNSingle} ID must be greater than zero"", nameof({TableId.ToLower()}));

            try
            {{
                using (SqlConnection connection = new SqlConnection(DataAccessSettings.ConnectionString()))
                using (SqlCommand command = new SqlCommand(""SP_Is{FormattedTNSingle}Exists"", connection))
                {{
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue(""@{TableId}"", {TableId.ToLower()});
                    await connection.OpenAsync();

                    var returnParameter = command.Parameters.Add(""@ReturnVal"", SqlDbType.{GetSqlDbType()});
                    returnParameter.Direction = ParameterDirection.ReturnValue;

                    await command.ExecuteNonQueryAsync();

                    return ({TableIdDT})returnParameter.Value == 1;
                }}
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return false;
            }}
        }}

";
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
                    return $@"public static async Task<{TableIdDT}> {FormattedTNPluralize}{MethodNames.Count}()
        {{
            try
            {{
                using (SqlConnection connection = new SqlConnection(DataAccessSettings.ConnectionString()))
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
                Helper.ErrorLogger(ex);
                return 0;
            }}
        }}

";
                case enCodeStyle.EF:
                    {
                        if (!new ClsSPGenerator(TableName).CountSP())
                        {
                            Helper.ErrorLogger(new Exception("Failed To Create Count Stored Procedure"));
                        }

                        return $@"     {ImplementationMethod(MethodNames.Count)}
        {{
            const string sqlQuery = ""EXEC [dbo].[SP_{FormattedTNPluralize}Count]"";

            try
            {{
                using var conn = ctx.Database.GetDbConnection();
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = sqlQuery;
                cmd.CommandType = System.Data.CommandType.Text;

                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }}
            catch (Exception ex)
            {{
                Helper.ErrorLogger(ex);
                return -1;
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

        private static string ADOValidationMethod()
        {
            var sb = new StringBuilder();
            sb.AppendLine($@"private static bool _Validate{FormattedTNSingle}(DtoClsName {FormattedTNSingleVar.ToLower()})
        {{");

            // Add validation logic for required fields
            foreach (var column in Columns)
            {
                if (!column.IsNullable && !column.IsPrimaryKey && column.Name.ToLower() != TableId.ToLower())
                {
                    string propertyName = FormatHelper.CapitalizeFirstChars(FormatId(column.Name));
                    string csharpType = Helper.GetCSharpType(column.DataType);

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
                    else if (csharpType == "int" || csharpType == "long" || csharpType == "decimal" || csharpType == "double" || csharpType == "float")
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

        private static string Closing()
        {
            return $@"    }}
}}";
        }

        #endregion

        #region  Support Methods

        private static string GetSqlDbType() => TableIdDT == "long" ? "BigInt" : "Int";


        private static string GetReaderAssignments()
        {
            var sb = new StringBuilder();
            bool firstLine = true;

            foreach (var column in Columns)
            {
                if (!firstLine)
                {
                    sb.AppendLine(",");
                }
                else
                {
                    firstLine = false;
                }

                sb.Append($"                                {GetReaderAssignment(column.Name, Helper.GetCSharpType(column.DataType), column.IsNullable)}");
            }

            return sb.ToString();
        }

        private static string GetReaderAssignment(string columnName, string csharpType, bool isNullable)
        {
            return $"reader.IsDBNull(reader.GetOrdinal(\"{columnName}\")) ? {Helper.GetDefaultValue(csharpType, isNullable)} : {GetReaderMethod(csharpType)}(reader.GetOrdinal(\"{columnName}\"))";
        }

        private static string GetReaderMethod(string csharpType)
        {
            switch (csharpType)
            {
                case "string": return "reader.GetString";
                case "int": return "reader.GetInt32";
                case "decimal": return "reader.GetDecimal";
                case "double": return "reader.GetDouble";
                case "float": return "reader.GetFloat";
                case "DateTime": return "reader.GetDateTime";
                case "bool": return "reader.GetBoolean";
                case "byte[]": return "reader.GetBytes";
                case "byte": return "reader.GetByte";
                case "short": return "reader.GetInt16";
                case "long": return "reader.GetInt64";
                default: return "reader.GetValue";
            }
        }

        private static string GetCommandParametersForAddUpdate()
        {
            var sb = new StringBuilder();

            foreach (var column in Columns)
            {
                if (column.IsPrimaryKey)
                {
                    continue;
                }

                string propertyName = FormatHelper.CapitalizeFirstChars(FormatId(column.Name));

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

        private static string tResultsForGetByID()
        {
            var columnStrings = Columns.Select(column =>
            {
                string dataType = Helper.GetCSharpType(column.DataType);
                string isNullable = column.IsNullable ? "?" : string.Empty;
                return $"{dataType}{isNullable} {FormatHelper.CapitalizeFirstChars(FormatId(column.Name))}";
            });

            return string.Join(", ", columnStrings);
        }

        private static string InfoForGetByID()
        {
            if (Columns == null || !Columns.Any())
            {
                return string.Empty;
            }

            var resultBuilder = new StringBuilder();

            foreach (var column in Columns)
            {
                string formattedName = FormatHelper.CapitalizeFirstChars(FormatId(column.Name));
                string csharpType = Helper.GetCSharpType(column.DataType);

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
            if (Columns == null || !Columns.Any())
            {
                return string.Empty;
            }

            var resultBuilder = new StringBuilder();

            foreach (var column in Columns)
            {
                string formattedName = FormatHelper.CapitalizeFirstChars(FormatId(column.Name));
                resultBuilder.AppendLine($"{FormattedTNSingleVar.ToLower()}Info.{formattedName},");
            }

            if (resultBuilder.Length > 0)
            {
                resultBuilder.Length -= Environment.NewLine.Length + 1;
            }

            return resultBuilder.ToString();
        }

        #endregion

        #region Code Generation Methods

        public bool GenerateAdoDaCode(out string? filePath)
        {
            filePath = null;

            enCodeStyle style = enCodeStyle.Ado;




            StringBuilder dalCode = new StringBuilder();

            dalCode.Append(TopUsing(style));
            dalCode.Append(GetByIDMethod(style));

            if (FormatHelper.Singularize(TableName.ToLower()) == "user")
            {
                dalCode.Append(GetByUsernameAndPasswordMethod(enCodeStyle.Ado));
                dalCode.Append(GetByPersonIDMethod(enCodeStyle.Ado));
            }

            if (FormatHelper.Singularize(TableName.ToLower()) == "country")
            {
                dalCode.Append(ADOGetByCountryNameMethod());
            }

            dalCode.Append(AddNewMethod(style));
            dalCode.Append(UpdateMethod(style));
            dalCode.Append(GetAllMethod(style));
            dalCode.Append(IsExistMethod(style));

            if (FormatHelper.Singularize(TableName.ToLower()) == "user")
            {
                dalCode.Append(IsExistByUsernameMethod(enCodeStyle.Ado));
                dalCode.Append(IsExistByPersonIdMethod(enCodeStyle.Ado));
            }

            dalCode.Append(CountMethod(style));
            dalCode.Append(DeleteMethod(style));
            dalCode.Append(ADOValidationMethod());
            dalCode.Append(Closing());

            string folderPath = Path.Combine(StoringPath, "DataAccess");
            string fileName = $"{DataClsName}.cs";

            bool success = FileHelper.StoreToFile(dalCode.ToString(), fileName, folderPath, true) && new ClsSPGenerator(TableName).GenerateAllSPs();

            if (success)
            {
                filePath = Path.Combine(folderPath, fileName);
            }

            return success;
        }

        public bool GenerateEFDaCode(out string? filePath)
        {
            filePath = null;

            var style = enCodeStyle.EF;



            StringBuilder dalCode = new StringBuilder();

            dalCode.Append(TopUsing(style) + EFContextConstrctor() + GetAllMethod(style) + GetByIDMethod(style));

            if (FormatHelper.Singularize(TableName.ToLower()) == "user")
            {
                dalCode.Append(GetByPersonIDMethod(enCodeStyle.EF) + GetByUsernameAndPasswordMethod(enCodeStyle.EF));
            }

            dalCode.Append(AddNewMethod(style) + UpdateMethod(style) + DeleteMethod(style) + IsExistMethod(style));

            if (FormatHelper.Singularize(TableName.ToLower()) == "user")
            {
                dalCode.Append(IsExistByUsernameMethod(enCodeStyle.EF) + IsExistByPersonIdMethod(enCodeStyle.EF));
            }

            dalCode.Append(CountMethod(style) + Closing());

            string fileName = $"{DataClsName}.cs";
            string folderPath = Path.Combine(StoringPath, "DataAccess");

            bool success = FileHelper.StoreToFile(dalCode.ToString(), fileName, folderPath, true);

            if (success)
            {
                filePath = Path.Combine(folderPath, fileName);
            }

            return success;
        }

        public bool GenerateDalCode(enCodeStyle codeStyle, out string filePath) => (codeStyle == enCodeStyle.Ado ? GenerateAdoDaCode(out filePath) : GenerateEFDaCode(out filePath));

        #endregion

    }
}