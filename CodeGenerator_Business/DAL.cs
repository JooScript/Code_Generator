using System;
using System.IO;
using System.Text;

namespace CodeGenerator_Business
{
    public class DalGenerator
    {
        public static void GenerateDalCode(string TableName)
        {
            string dalCode = $@"
using Inventory_DataAccess.Data;
using Inventory_DataAccess.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Utilities;

namespace Inventory_DataAccess.DataAccess
{{
    public class cls{TableName}Data
    {{
        public static async Task<List<{TableName}>> GetAll{TableName}sAsync()
        {{
            try
            {{
                await using var context = new AppDbContext();
                return await context.{TableName}s.AsNoTracking().ToListAsync().ConfigureAwait(false);
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                throw;
            }}
        }}

        public static async Task<{TableName}?> Get{TableName}InfoByIdAsync(int {TableName.ToLower()}Id)
        {{
            if ({TableName.ToLower()}Id <= 0)
            {{
                return null;
            }}

            try
            {{
                await using var context = new AppDbContext();
                return await context.{TableName}s.AsNoTracking().SingleOrDefaultAsync(e => e.{TableName}Id == {TableName.ToLower()}Id).ConfigureAwait(false);
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return null;
            }}
        }}

        public static async Task<int> AddNew{TableName}Async({TableName} {TableName.ToLower()})
        {{
            if ({TableName.ToLower()} == null)
            {{
                return -1;
            }}

            try
            {{
                await using var context = new AppDbContext();
                await context.{TableName}s.AddAsync({TableName.ToLower()}).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);

                return {TableName.ToLower()}.{TableName}Id;
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return -1;
            }}
        }}

        public static async Task<bool> Update{TableName}Async({TableName} {TableName.ToLower()})
        {{
            if ({TableName.ToLower()} == null)
            {{
                return false;
            }}

            try
            {{
                await using var context = new AppDbContext();
                var existing{TableName} = await context.{TableName}s.SingleOrDefaultAsync(e => e.{TableName}Id == {TableName.ToLower()}.{TableName}Id).ConfigureAwait(false);

                if (existing{TableName} == null)
                {{
                    return false;
                }}

                context.Entry(existing{TableName}).CurrentValues.SetValues({TableName.ToLower()});
                await context.SaveChangesAsync().ConfigureAwait(false);
                return true;
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return false;
            }}
        }}

        public static async Task<bool> Delete{TableName}Async(int {TableName.ToLower()}Id)
        {{
            if ({TableName.ToLower()}Id <= 0)
            {{
                return false;
            }}

            try
            {{
                await using var context = new AppDbContext();
                var {TableName.ToLower()} = await context.{TableName}s.SingleOrDefaultAsync(e => e.{TableName}Id == {TableName.ToLower()}Id).ConfigureAwait(false);

                if ({TableName.ToLower()} == null)
                {{
                    return false;
                }}

                context.{TableName}s.Remove({TableName.ToLower()});
                await context.SaveChangesAsync().ConfigureAwait(false);
                return true;
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return false;
            }}
        }}

        public static async Task<bool> Is{TableName}ExistsAsync(int {TableName.ToLower()}Id)
        {{
            if ({TableName.ToLower()}Id <= 0)
            {{
                return false;
            }}

            const string sqlQuery = ""EXEC SP_Check{TableName}Exists @{TableName}ID = @{TableName}ID"";

            try
            {{
                await using var context = new AppDbContext();
                int result = await context.Database.SqlQueryRaw<int>(sqlQuery, new SqlParameter(""@{TableName}ID"", {TableName.ToLower()}Id)).SingleOrDefaultAsync().ConfigureAwait(false);

                return result == 1;
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return false;
            }}
        }}
    }}
}}";

            string filePath = Path.Combine("C:\\Users\\yousef\\Desktop\\Code Generator\\", $"cls{TableName}Data.cs");

            File.WriteAllText(filePath, dalCode);
        }
    }
}