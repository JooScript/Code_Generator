using System.Text;
using Utilities;

namespace CodeGenerator.Bl
{
    public class ClsInterfacesGenerator : ClsGenerator
    {
        #region Interfaces

        private static string Interface()
        {
            return $@"    public interface {LogicInterfaceName}
    {{
    public  Task<{ModelName}> FindAsync(int id);
    public  Task<List<{ModelName}>> GetAllAsync(int pageNumber = 1, int pageSize = 50);
    public Task<int> AddAsync({ModelName} {FormattedTNSingleVar});
    public Task<bool> UpdateAsync({ModelName} {FormattedTNSingleVar});
    public  Task<bool> SaveAsync({ModelName} {FormattedTNSingleVar});
    public  Task<bool> IsExistsAsync(int id);
    public  Task<int> CountAsync();
    public  Task<bool> DeleteAsync(int id);
    }}";
        }

        public static bool GenerateInterfaceCode(string tableName)
        {
            if (tableName == null)
            {
                return false;
            }
            else
            {
                TableName = tableName;
            }

            string folderPath = Path.Combine(StoringPath, "Interfaces");

            StringBuilder blCode = new StringBuilder();
            blCode.AppendLine(@$"using {AppName}.Models;");

            blCode.Append(Interface());

            return FileHelper.StoreToFile(blCode.ToString(), $"{LogicInterfaceName}.cs", folderPath, true);
        }

        #endregion
    }
}