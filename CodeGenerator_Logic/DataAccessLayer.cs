using Microsoft.Data.SqlClient;
using Utilities;
using System.Text;


namespace CodeGenerator_Logic
{
    public static class clsDaGenerator
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

        #region For GetByID

        private static string tResultsForGetByID()
        {
            var columnStrings = _columns.Select(column =>
            {
                string dataType = clsUtil.ConvertDbTypeToCSharpType(column.DataType);
                string isNullable = column.IsNullable ? "?" : string.Empty;
                return $"{dataType}{isNullable} {clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name))}";
            });

            return string.Join(", ", columnStrings);
        }

        private static string InfoForGetByID()
        {
            if (_columns == null || !_columns.Any())
            {
                return string.Empty;
            }

            var resultBuilder = new StringBuilder();

            foreach (var column in _columns)
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
            if (_columns == null || !_columns.Any())
            {
                return string.Empty;
            }

            var resultBuilder = new StringBuilder();

            foreach (var column in _columns)
            {
                string formattedName = clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name));
                resultBuilder.AppendLine($"{_FormattedTNSingleVar.ToLower()}Info.{formattedName},");
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
            var columnStrings = _columns
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
            if (_columns == null || !_columns.Any())
            {
                return string.Empty;
            }

            var resultBuilder = new StringBuilder();

            foreach (var column in _columns)
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
            return $"int {_TableId},{ParamatersForAddNew()}";
        }

        private static string ObjectForUpdate()
        {
            if (_columns == null || !_columns.Any())
            {
                return string.Empty;
            }

            var resultBuilder = new StringBuilder();
            bool firstLine = true;

            foreach (var column in _columns)
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
                    resultBuilder.Append($"existing{_FormattedTNSingle}.{formattedName} = clsSecure.HashPassword({formattedName});");
                    continue;
                }

                resultBuilder.Append($"existing{_FormattedTNSingle}.{formattedName} = {formattedName};");
            }

            return resultBuilder.ToString();
        }

        #endregion

        #endregion

        #region EF Class Structure

        private static string TopUsing()
        {
            return
$@"using {_AppName}_DataAccess.Data;
using {_AppName}_DataAccess.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Utilities;

namespace {_AppName}_DataAccess.DataAccess
{{
    public partial class cls{_FormattedTNSingle}Data
    {{";
        }

        private static string GetAllMethod()
        {
            return $@"public static async Task<List<{_FormattedTNSingle}>> GetAll{_FormattedTNPluralize}Async(int pageNumber = 1, int pageSize = 50)
        {{
            try
            {{
                await using var context = new AppDbContext();
                var query = context.{_FormattedTNPluralize}.AsNoTracking();

                var {_FormattedTNPluralizeVar} = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync().ConfigureAwait(false);

                return ({_FormattedTNPluralizeVar});
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                throw;
            }}
        }}

";
        }

        private static string GetByIDMethod()
        {
            return $@"public static async Task<({tResultsForGetByID()})?> Get{_FormattedTNSingle}InfoByIdAsync(int {_TableId.ToLower()})
{{
    if ({_TableId.ToLower()} <= 0)
    {{
        throw new ArgumentException(""{_FormattedTNSingle} ID must be greater than 0."", nameof({_TableId.ToLower()}));
    }}

    try
    {{
        await using var context = new AppDbContext();

        var {_FormattedTNSingleVar.ToLower()}Info = await context.{_FormattedTNPluralize}
            .Where(x => x.{_TableId} == {_TableId.ToLower()})
            .Select(x => new
            {{
                {InfoForGetByID()}             
            }})
            .AsNoTracking()
            .SingleOrDefaultAsync()
            .ConfigureAwait(false);

        if ({_FormattedTNSingleVar.ToLower()}Info == null)
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

        private static string GetByPersonIDMethod()
        {
            return $@"public static async Task<({tResultsForGetByID()})?> Get{_FormattedTNSingle}InfoByPersonIdAsync(int personid)
{{
    if (personid <= 0)
    {{
        throw new ArgumentException(""Person ID must be greater than 0."", nameof(personid));
    }}

    try
    {{
        await using var context = new AppDbContext();

        var {_FormattedTNSingleVar.ToLower()}Info = await context.{_FormattedTNPluralize}
            .Where(x => x.PersonId == personid)
            .Select(x => new
            {{
                {InfoForGetByID()}             
            }})
            .AsNoTracking()
            .SingleOrDefaultAsync()
            .ConfigureAwait(false);

        if ({_FormattedTNSingleVar.ToLower()}Info == null)
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

        private static string GetByUsernameAndPasswordMethod()
        {
            return $@"public static async Task<({tResultsForGetByID()})?> Get{_FormattedTNSingle}InfoByUsernameAndPasswordAsync(string username, string password)
{{
         if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(username))
            {{
                throw new ArgumentException(""Username and Password must be declared."");
            }}

    try
    {{
        await using var context = new AppDbContext();

        var {_FormattedTNSingleVar.ToLower()}Info = await context.{_FormattedTNPluralize}.Where(x => x.Username == username && clsSecure.VerifyPassword(password, x.Password))
            .Select(x => new
            {{
                {InfoForGetByID()}             
            }})
            .AsNoTracking()
            .SingleOrDefaultAsync()
            .ConfigureAwait(false);

        if ({_FormattedTNSingleVar.ToLower()}Info == null)
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

        private static string AddNewMethod()
        {
            return $@"public static async Task<int> AddNew{_FormattedTNSingle}Async({ParamatersForAddNew()})
        {{
            try
            {{
                {_FormattedTNSingle} new{_FormattedTNSingle} = new {_FormattedTNSingle}
                {{
{ObjectForAddNew()}   
                }};

                await using var context = new AppDbContext();
                await context.{_FormattedTNPluralize}.AddAsync(new{_FormattedTNSingle}).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);

                return new{_FormattedTNSingle}.{_TableId};
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return -1;
            }}
        }}

";
        }

        private static string UpdateMethod()
        {
            return $@"public static async Task<bool> Update{_FormattedTNSingle}Async({ParamatersForUpdate()})
        {{
            if ({_TableId} <= 0)
            {{
                return false;
            }}

            try
            {{
                await using var context = new AppDbContext();
                var existing{_FormattedTNSingle} = await context.{_FormattedTNPluralize}.SingleOrDefaultAsync(x => x.{_TableId} == {_TableId}).ConfigureAwait(false);

                if (existing{_FormattedTNSingle} == null)
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

        private static string DeleteMethod()
        {
            return $@"public static async Task<bool> Delete{_FormattedTNSingle}Async(int {_TableId})
        {{
            if ({_TableId} <= 0)
            {{
                return false;
            }}

            try
            {{
                await using var context = new AppDbContext();
                {_FormattedTNSingle}? {_FormattedTNSingleVar} = await context.{_FormattedTNPluralize}.SingleOrDefaultAsync(x => x.{_TableId} == {_TableId}).ConfigureAwait(false);

                if ({_FormattedTNSingleVar} == null)
                {{
                    return false;
                }}

                context.{_FormattedTNPluralize}.Remove({_FormattedTNSingleVar});
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

        private static string IsExistMethod()
        {
            return $@"public static async Task<bool> Is{_FormattedTNSingle}ExistsAsync(int {_TableId})
        {{
            if ({_TableId} <= 0)
            {{
                return false;
            }}

            const string sqlQuery = ""EXEC SP_Is{_FormattedTNSingle}Exists @{_TableId} = @{_TableId}"";

            try
            {{
                await using var context = new AppDbContext();
                int result = await context.Database.SqlQueryRaw<int>(sqlQuery, new SqlParameter(""@{_TableId}"", {_TableId})).SingleOrDefaultAsync().ConfigureAwait(false);

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

        private static string IsExistByUsernameMethod()
        {
            return $@"public static async Task<bool> Is{_FormattedTNSingle}ExistsAsync(string UserName)
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

        private static string IsExistByPersonIdMethod()
        {
            return $@"public static async Task<bool> Is{_FormattedTNSingle}ExistsByPersonIdAsync(int personId)
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

        private static string Closing()
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
                folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Code Generator\\{clsDataAccessSettings.AppName()}\\DataAccess\\Basic\\");
            }

            StringBuilder dalCode = new StringBuilder();

            dalCode.Append(TopUsing() + GetAllMethod() + GetByIDMethod());

            if (clsFormat.Singularize(_TableName.ToLower()) == "user")
            {
                dalCode.Append(GetByPersonIDMethod() + GetByUsernameAndPasswordMethod());
            }

            dalCode.Append(AddNewMethod() + UpdateMethod() + DeleteMethod() + IsExistMethod());

            if (clsFormat.Singularize(_TableName.ToLower()) == "user")
            {
                dalCode.Append(IsExistByUsernameMethod() + IsExistByPersonIdMethod());
            }

            dalCode.Append(Closing());

            string fileName = $"cls{_FormattedTNSingle}Data.cs";
            return clsFile.StoreToFile(dalCode.ToString(), fileName, folderPath, true);
        }

        #endregion

        #region Stored Procedures

        public static bool CreateIsExistsSP()
        {
            if (string.IsNullOrWhiteSpace(_AppName))
            {
                throw new ArgumentNullException(nameof(_AppName));
            }

            if (string.IsNullOrWhiteSpace(_TableName))
            {
                throw new ArgumentNullException(nameof(_TableName));
            }

            if (string.IsNullOrWhiteSpace(_TableId))
            {
                throw new ArgumentNullException(nameof(_TableId));
            }

            string procSql = $@"
    IF NOT EXISTS (SELECT * FROM sys.objects 
                  WHERE type = 'P' AND name = 'SP_Is{_FormattedTNSingle}Exists')
    BEGIN
        EXEC('
        USE [{_AppName}];
        
        CREATE PROCEDURE [dbo].[SP_Is{_FormattedTNSingle}Exists]
            @{_TableId} INT
        AS
        BEGIN
            SET NOCOUNT ON;
            
            IF EXISTS(SELECT 1 FROM [{_TableName}] 
                        WHERE [{_TableId}] = @{_TableId})
                RETURN 1;
            RETURN 0;
        END
        ');
    END";

            try
            {
                using (var connection = new SqlConnection(clsDataAccessSettings.ConnectionString()))
                using (var command = new SqlCommand(procSql, connection))
                {
                    connection.Open();
                    command.CommandTimeout = 30;
                    command.ExecuteNonQuery();

                    return clsDatabase.VerifyProcedureExists(connection, $"SP_Is{_FormattedTNSingle}Exists");
                }
            }
            catch (SqlException sqlEx)
            {
                clsUtil.ErrorLogger(new Exception(
                    $"Failed to create SP_Is{_FormattedTNSingle}Exists procedure. " +
                    $"Database: {_AppName}, Table: {_TableName}, ID Column: {_TableId}", sqlEx));
                return false;
            }
            catch (Exception ex)
            {
                clsUtil.ErrorLogger(new Exception(
                    $"Unexpected error creating SP_Is{_FormattedTNSingle}Exists procedure", ex));
                return false;
            }
        }

        //        public static bool CreateCountSP()
        //        {
        //            if (string.IsNullOrWhiteSpace(_AppName))
        //            {
        //                throw new ArgumentNullException(nameof(_AppName));
        //            }

        //            if (string.IsNullOrWhiteSpace(_TableName))
        //            {
        //                throw new ArgumentNullException(nameof(_TableName));
        //            }

        //            if (string.IsNullOrWhiteSpace(_TableId))
        //            {
        //                throw new ArgumentNullException(nameof(_TableId));
        //            }

        //            string procSql = $@"
        //    IF NOT EXISTS (SELECT * FROM sys.objects 
        //                  WHERE type = 'P' AND name = 'SP_Is{_FormattedTNSingle}Exists')
        //    BEGIN
        //        EXEC('
        //        USE [{_AppName}];

        //        CREATE PROCEDURE [dbo].[SP_Is{_FormattedTNSingle}Exists]
        //            @{_TableId} INT
        //        AS
        //        BEGIN
        //            SET NOCOUNT ON;

        //            IF EXISTS(SELECT 1 FROM [{_TableName}] 
        //                        WHERE [{_TableId}] = @{_TableId})
        //                RETURN 1;
        //            RETURN 0;
        //        END
        //        ');
        //    END
        //";

        //            try
        //            {
        //                using (var connection = new SqlConnection(clsDataAccessSettings.ConnectionString()))
        //                using (var command = new SqlCommand(procSql, connection))
        //                {
        //                    connection.Open();
        //                    command.CommandTimeout = 30;
        //                    command.ExecuteNonQuery();

        //                    return clsDatabase.VerifyProcedureExists(connection, $"SP_Is{_FormattedTNSingle}Exists");
        //                }
        //            }
        //            catch (SqlException sqlEx)
        //            {
        //                clsUtil.ErrorLogger(new Exception(
        //                    $"Failed to create SP_Is{_FormattedTNSingle}Exists procedure. " +
        //                    $"Database: {_AppName}, Table: {_TableName}, ID Column: {_TableId}", sqlEx));
        //                return false;
        //            }
        //            catch (Exception ex)
        //            {
        //                clsUtil.ErrorLogger(new Exception(
        //                    $"Unexpected error creating SP_Is{_FormattedTNSingle}Exists procedure", ex));
        //                return false;
        //            }
        //        }

        #endregion

        public static bool GenerateDalCode(string tableName, string? folderPath = null, enCodeStyle codeStyle = enCodeStyle.EFStyle)
        {
            return codeStyle == enCodeStyle.EFStyle ? GenerateEFDalCode(tableName, folderPath) : false;
        }

    }
}