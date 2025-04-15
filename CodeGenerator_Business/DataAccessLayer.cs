using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using Utilities;

namespace CodeGenerator_Business
{
    public class clsDalGenerator
    {
        public clsDalGenerator(string TName)
        {
            _TableName = TName ?? "Empty";
        }

        #region Prop

        private static string _TableName
        {
            get;
            set;
        }

        private static string FormattedTNSingle
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

        private static string FormattedTNPluralize
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

        private static string FormattedTNSingleVar
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

        private static string FormattedTNPluralizeVar
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

        #region Support Func

        #region For GetByID

        private static string tResultsForGetByID()
        {
            clsDatabase.Initialize(clsDataAccessSettings.ConnectionString());

            var columns = clsDatabase.GetTableColumns(_TableName);

            var columnStrings = columns.Select(column =>
            {
                string dataType = clsUtil.ConvertDbTypeToCSharpType(column.DataType);
                string isNullable = column.IsNullable ? "?" : string.Empty;
                return $"{dataType}{isNullable} {clsFormat.CapitalizeFirstChars(clsFormat.FormatId(column.Name))}";
            });

            return string.Join(", ", columnStrings);
        }

        private static string InfoForGetByID()
        {
            clsDatabase.Initialize(clsDataAccessSettings.ConnectionString());

            var columns = clsDatabase.GetTableColumns(_TableName);
            if (columns == null || !columns.Any())
            {
                return string.Empty;
            }

            var resultBuilder = new StringBuilder();

            foreach (var column in columns)
            {
                string formattedName = clsFormat.CapitalizeFirstChars(clsFormat.FormatId(column.Name));
                string csharpType = clsUtil.ConvertDbTypeToCSharpType(column.DataType);

                if (column.IsPrimaryKey)
                {
                    resultBuilder.AppendLine($"{FormattedTNSingleVar.ToLower()}Id,");
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
            clsDatabase.Initialize(clsDataAccessSettings.ConnectionString());

            var columns = clsDatabase.GetTableColumns(_TableName);
            if (columns == null || !columns.Any())
            {
                return string.Empty;
            }

            var resultBuilder = new StringBuilder();

            foreach (var column in columns)
            {
                string formattedName = clsFormat.CapitalizeFirstChars(clsFormat.FormatId(column.Name));
                if (column.IsIdentity)
                {
                    resultBuilder.AppendLine($"{FormattedTNSingleVar.ToLower()}Info.{clsFormat.FormatId(column.Name.ToLower())},");
                }
                else
                {
                    resultBuilder.AppendLine($"{FormattedTNSingleVar.ToLower()}Info.{formattedName},");
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
            clsDatabase.Initialize(clsDataAccessSettings.ConnectionString());

            var columns = clsDatabase.GetTableColumns(_TableName);

            var columnStrings = columns
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
            clsDatabase.Initialize(clsDataAccessSettings.ConnectionString());

            var columns = clsDatabase.GetTableColumns(_TableName);
            if (columns == null || !columns.Any())
            {
                return string.Empty;
            }

            var resultBuilder = new StringBuilder();

            foreach (var column in columns)
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
            clsDatabase.Initialize(clsDataAccessSettings.ConnectionString());

            var columns = clsDatabase.GetTableColumns(_TableName);
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

                string formattedName = clsFormat.CapitalizeFirstChars(clsFormat.FormatId(column.Name));

                if (!firstLine)
                {
                    resultBuilder.AppendLine();
                }
                else
                {
                    firstLine = false;
                }

                resultBuilder.Append($"existing{FormattedTNSingle}.{formattedName} = {formattedName};");
            }

            return resultBuilder.ToString();
        }

        #endregion

        #endregion

        #region Misc
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
    public class cls{FormattedTNSingle}Data
    {{";
        }

        private static string LastSection()
        {
            return $@"}}}}";
        }

        #endregion

        #region Class Func

        private static string GetAll()
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

        private static string GetByID()
        {
            return $@"public static async Task<({tResultsForGetByID()})?> Get{FormattedTNSingle}InfoByIdAsync(int {FormattedTNSingleVar.ToLower()}Id)
{{
    if ({FormattedTNSingleVar.ToLower()}Id <= 0)
    {{
        throw new ArgumentException(""{FormattedTNSingle} ID must be greater than 0."", nameof({FormattedTNSingleVar.ToLower()}Id));
    }}

    try
    {{
        await using var context = new AppDbContext();

        var {FormattedTNSingleVar.ToLower()}Info = await context.{FormattedTNPluralize}
            .Where(x => x.{FormattedTNSingle}Id == {FormattedTNSingleVar.ToLower()}Id)
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

        private static string AddNew()
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

                return new{FormattedTNSingle}.{FormattedTNSingle}Id;
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return -1;
            }}
        }}

";
        }

        private static string Update()
        {
            return $@"public static async Task<bool> Update{FormattedTNSingle}Async(int {FormattedTNSingleVar}Id, int CountryId, int GovernorateId, string StreetName, string BuildingName, int CityId, string District, string NearestLandmark, byte AddressType, int? PersonId, int? CompanyId)
        {{
            if ({FormattedTNSingleVar}Id <= 0)
            {{
                return false;
            }}

            try
            {{
                await using var context = new AppDbContext();
                var existing{FormattedTNSingle} = await context.Addresses.SingleOrDefaultAsync(x => x.{FormattedTNSingle}Id == {FormattedTNSingleVar}Id).ConfigureAwait(false);

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

        private static string Delete()
        {
            return $@"public static async Task<bool> Delete{FormattedTNSingle}Async(int {FormattedTNSingleVar}Id)
        {{
            if ({FormattedTNSingleVar}Id <= 0)
            {{
                return false;
            }}

            try
            {{
                await using var context = new AppDbContext();
                {FormattedTNSingle}? {FormattedTNSingleVar} = await context.{FormattedTNPluralize}.SingleOrDefaultAsync(x => x.{FormattedTNPluralizeVar}Id == {FormattedTNSingleVar}Id).ConfigureAwait(false);

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

        private static string IsExist()
        {
            return $@"public static async Task<bool> Is{FormattedTNSingle}ExistsAsync(int {FormattedTNSingleVar}Id)
        {{
            if ({FormattedTNSingleVar}Id <= 0)
            {{
                return false;
            }}

            const string sqlQuery = ""EXEC SP_Check{FormattedTNSingle}Exists @{FormattedTNSingle}ID = @{FormattedTNSingle}ID"";

            try
            {{
                await using var context = new AppDbContext();
                int result = await context.Database.SqlQueryRaw<int>(sqlQuery, new SqlParameter(""@{FormattedTNSingle}ID"", {FormattedTNSingleVar}Id)).SingleOrDefaultAsync().ConfigureAwait(false);

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

        #endregion

        public static bool GenerateDalCode(string TableName, string? folderPath = null)
        {
            if (TableName == null)
            {
                return false;
            }
            else
            {
                _TableName = TableName;
            }

            if (string.IsNullOrEmpty(folderPath))
            {
                folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Code Generator\\DataAccess\\");
            }

            StringBuilder dalCode = new StringBuilder();

            dalCode.Append(TopUsing());
            dalCode.Append(GetAll());
            dalCode.Append(GetByID());
            dalCode.Append(AddNew());
            dalCode.Append(Update());
            dalCode.Append(Delete());
            dalCode.Append(IsExist());
            dalCode.Append(LastSection());

            string fileName = $"cls{TableName}Data.cs";
            return clsFile.StoreToFile(dalCode.ToString(), folderPath, fileName, true);
        }

    }
}