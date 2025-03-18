using System;
using System.IO;
using System.Text;

namespace CodeGenerator
{
    public class BlGenerator
    {
        public static void GenerateBlCode(string TableName)
        {
            string blCode = $@"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Inventory_DataAccess.DataAccess;
using Inventory_DataAccess.Entities;
using Utilities;

namespace Inventory_Business.BusinessLogic
{{
    public class cls{TableName} : {TableName}
    {{
        public enum enMode
        {{
            AddNew = 0,
            Update = 1
        }};

        public enMode Mode = enMode.AddNew;

        public cls{TableName}()
        {{
            this.{TableName}Id = -1;
            Mode = enMode.AddNew;
        }}

        private cls{TableName}({TableName} {TableName.ToLower()})
        {{
            this.{TableName}Id = {TableName.ToLower()}.{TableName}Id;
            Mode = enMode.Update;
        }}

        private async Task<bool> _AddNew{TableName}Async()
        {{
            try
            {{
                this.{TableName}Id = await cls{TableName}Data.AddNew{TableName}Async(this);
                return this.{TableName}Id != -1;
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return false;
            }}
        }}

        private async Task<bool> _Update{TableName}Async()
        {{
            try
            {{
                return await cls{TableName}Data.Update{TableName}Async(this);
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                return false;
            }}
        }}

        public static async Task<cls{TableName}?> FindAsync(int {TableName.ToLower()}Id)
        {{
            {TableName}? {TableName.ToLower()} = await cls{TableName}Data.Get{TableName}InfoByIdAsync({TableName.ToLower()}Id);

            if ({TableName.ToLower()} != null)
            {{
                return new cls{TableName}({TableName.ToLower()});
            }}
            else
            {{
                return null;
            }}
        }}

        public static cls{TableName}? Find(int {TableName.ToLower()}Id)
        {{
            return FindAsync({TableName.ToLower()}Id).GetAwaiter().GetResult();
        }}

        public async Task<bool> SaveAsync()
        {{
            switch (Mode)
            {{
                case enMode.AddNew:
                    if (await _AddNew{TableName}Async())
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
                        return await _Update{TableName}Async();
                    }}
            }}
            return false;
        }}

        public bool Save()
        {{
            return SaveAsync().GetAwaiter().GetResult();
        }}

        public static async Task<List<cls{TableName}>> GetAll{TableName}sAsync()
        {{
            try
            {{
                List<{TableName}> {TableName.ToLower()}s = await cls{TableName}Data.GetAll{TableName}sAsync();

                List<cls{TableName}> cls{TableName}s = {TableName.ToLower()}s.Select({TableName.ToLower()} => new cls{TableName}({TableName.ToLower()})).ToList();

                return cls{TableName}s;
            }}
            catch (Exception ex)
            {{
                clsUtil.ErrorLogger(ex);
                throw;
            }}
        }}

        public static List<cls{TableName}> GetAll{TableName}s()
        {{
            return GetAll{TableName}sAsync().GetAwaiter().GetResult();
        }}

        public static async Task<bool> Delete{TableName}Async(int {TableName.ToLower()}Id)
        {{
            return await cls{TableName}Data.Delete{TableName}Async({TableName.ToLower()}Id);
        }}

        public static bool Delete{TableName}(int {TableName.ToLower()}Id)
        {{
            return Delete{TableName}Async({TableName.ToLower()}Id).GetAwaiter().GetResult();
        }}

        public static async Task<bool> Is{TableName}ExistAsync(int {TableName.ToLower()}Id)
        {{
            return await cls{TableName}Data.Is{TableName}ExistsAsync({TableName.ToLower()}Id);
        }}

        public static bool Is{TableName}Exist(int {TableName.ToLower()}Id)
        {{
            return Is{TableName}ExistAsync({TableName.ToLower()}Id).GetAwaiter().GetResult();
        }}
    }}
}}";

            string filePath = Path.Combine("C:\\Users\\yousef\\Desktop\\Code Generator\\", $"cls{TableName}.cs");
            File.WriteAllText(filePath, blCode);
        }
    }
}