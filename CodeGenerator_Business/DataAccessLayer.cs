using System.Text;
using Utilities;

namespace CodeGenerator_BusinessLogic
{
    public static class clsDaGenerator
    {
        public enum enCodeStyle
        {
            EFStyle = 0,
            AdoStyle = 1
        }
        private static string _TableName;
        private static List<clsDatabase.ColumnInfo> _columns;

        #region Properties

        public static string TableName
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

        #region For GetByID

        private static string tResultsForGetByID()
        {
            var columnStrings = _columns.Select(column =>
            {
                string dataType = clsUtil.ConvertDbTypeToCSharpType(column.DataType);
                string isNullable = column.IsNullable ? "?" : string.Empty;
                return $"{dataType}{isNullable} {clsFormat.CapitalizeFirstChars(clsFormat.FormatId(column.Name))}";
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
                string formattedName = clsFormat.CapitalizeFirstChars(clsFormat.FormatId(column.Name));
                string csharpType = clsUtil.ConvertDbTypeToCSharpType(column.DataType);

                if (column.IsPrimaryKey)
                {
                    resultBuilder.AppendLine($"{_FormattedTNSingleVar.ToLower()}Id,");
                    continue;
                }

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
                string formattedName = clsFormat.CapitalizeFirstChars(clsFormat.FormatId(column.Name));
                if (column.IsIdentity)
                {
                    resultBuilder.AppendLine($"{_FormattedTNSingleVar.ToLower()}Info.{clsFormat.FormatId(column.Name.ToLower())},");
                }
                else
                {
                    resultBuilder.AppendLine($"{_FormattedTNSingleVar.ToLower()}Info.{formattedName},");
                }
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
                    return $"{dataType}{isNullable} {clsFormat.CapitalizeFirstChars(clsFormat.FormatId(column.Name))}";
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
                string formattedName = clsFormat.CapitalizeFirstChars(clsFormat.FormatId(column.Name));

                if (column.IsPrimaryKey)
                {
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

                string formattedName = clsFormat.CapitalizeFirstChars(clsFormat.FormatId(column.Name));

                if (!firstLine)
                {
                    resultBuilder.AppendLine();
                }
                else
                {
                    firstLine = false;
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
            string AppName = clsDataAccessSettings.AppName();

            return
$@"using {AppName}_DataAccess.Data;
using {AppName}_DataAccess.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Utilities;

namespace {AppName}_DataAccess.DataAccess
{{
    public class cls{_FormattedTNSingle}Data
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
            return $@"public static async Task<({tResultsForGetByID()})?> Get{_FormattedTNSingle}InfoByIdAsync(int {_FormattedTNSingleVar.ToLower()}Id)
{{
    if ({_FormattedTNSingleVar.ToLower()}Id <= 0)
    {{
        throw new ArgumentException(""{_FormattedTNSingle} ID must be greater than 0."", nameof({_FormattedTNSingleVar.ToLower()}Id));
    }}

    try
    {{
        await using var context = new AppDbContext();

        var {_FormattedTNSingleVar.ToLower()}Info = await context.{_FormattedTNPluralize}
            .Where(x => x.{_FormattedTNSingle}Id == {_FormattedTNSingleVar.ToLower()}Id)
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

                return new{_FormattedTNSingle}.{_FormattedTNSingle}Id;
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
            return $@"public static async Task<bool> Update{_FormattedTNSingle}Async(int {_FormattedTNSingleVar}Id, int CountryId, int GovernorateId, string StreetName, string BuildingName, int CityId, string District, string NearestLandmark, byte AddressType, int? PersonId, int? CompanyId)
        {{
            if ({_FormattedTNSingleVar}Id <= 0)
            {{
                return false;
            }}

            try
            {{
                await using var context = new AppDbContext();
                var existing{_FormattedTNSingle} = await context.Addresses.SingleOrDefaultAsync(x => x.{_FormattedTNSingle}Id == {_FormattedTNSingleVar}Id).ConfigureAwait(false);

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
            return $@"public static async Task<bool> Delete{_FormattedTNSingle}Async(int {_FormattedTNSingleVar}Id)
        {{
            if ({_FormattedTNSingleVar}Id <= 0)
            {{
                return false;
            }}

            try
            {{
                await using var context = new AppDbContext();
                {_FormattedTNSingle}? {_FormattedTNSingleVar} = await context.{_FormattedTNPluralize}.SingleOrDefaultAsync(x => x.{_FormattedTNPluralizeVar}Id == {_FormattedTNSingleVar}Id).ConfigureAwait(false);

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
            return $@"public static async Task<bool> Is{_FormattedTNSingle}ExistsAsync(int {_FormattedTNSingleVar}Id)
        {{
            if ({_FormattedTNSingleVar}Id <= 0)
            {{
                return false;
            }}

            const string sqlQuery = ""EXEC SP_Check{_FormattedTNSingle}Exists @{_FormattedTNSingle}ID = @{_FormattedTNSingle}ID"";

            try
            {{
                await using var context = new AppDbContext();
                int result = await context.Database.SqlQueryRaw<int>(sqlQuery, new SqlParameter(""@{_FormattedTNSingle}ID"", {_FormattedTNSingleVar}Id)).SingleOrDefaultAsync().ConfigureAwait(false);

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
                folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Code Generator\\DataAccess\\");
            }

            StringBuilder dalCode = new StringBuilder();

            dalCode.Append(TopUsing());
            dalCode.Append(GetAllMethod());
            dalCode.Append(GetByIDMethod());
            dalCode.Append(AddNewMethod());
            dalCode.Append(UpdateMethod());
            dalCode.Append(DeleteMethod());
            dalCode.Append(IsExistMethod());
            dalCode.Append(Closing());

            string fileName = $"cls{_FormattedTNSingle}Data.cs";
            return clsFile.StoreToFile(dalCode.ToString(), fileName, folderPath, true);
        }

        #endregion

        public static bool GenerateDalCode(string tableName, string? folderPath = null, enCodeStyle codeStyle = enCodeStyle.EFStyle)
        {
            return codeStyle == enCodeStyle.EFStyle ? GenerateEFDalCode(tableName, folderPath) : false;
        }

    }
}