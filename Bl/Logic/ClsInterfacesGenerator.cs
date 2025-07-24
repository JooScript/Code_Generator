using System.Net;
using System.Text;
using Utilities;

namespace CodeGenerator.Bl
{
    public class ClsInterfacesGenerator : ClsGenerator
    {
        public ClsInterfacesGenerator(string tableName) : base(tableName)
        {

        }

        #region Private Methods

        private static string GenerateRepositoryMethodDeclarations(enInterfaceType interfaceType)
        {
            StringBuilder repoCode = new StringBuilder();
            repoCode.AppendLine(RepoMethods[MethodNames.GetById]);
            repoCode.AppendLine(RepoMethods[MethodNames.GetAll]);
            repoCode.AppendLine(RepoMethods[MethodNames.Add]);
            repoCode.AppendLine(RepoMethods[MethodNames.Update]);

            if (interfaceType == enInterfaceType.Bl)
            {
                repoCode.AppendLine(RepoMethods[MethodNames.Save]);
            }

            repoCode.AppendLine(RepoMethods[MethodNames.IsExists]);
            repoCode.AppendLine(RepoMethods[MethodNames.Count]);
            repoCode.AppendLine(RepoMethods[MethodNames.Delete]);

            return repoCode.ToString();
        }

        private static string GenerateInterfaceDefinition(enInterfaceType interfaceType)
        {
            string interfaceName = interfaceType == enInterfaceType.Bl ? LogicInterfaceName : DataInterfaceName;
            return $@"    public interface {interfaceName}
    {{
{GenerateRepositoryMethodDeclarations(interfaceType)}
    }}";
        }

        private enum enInterfaceType
        {
            Bl,
            Da
        }

        private bool _GenerateInterfaceFile(enInterfaceType interfaceType, out string filePath)
        {
            filePath = null;



            string folderName = interfaceType == enInterfaceType.Bl ? "BlInterfaces" : "DaInterfaces";
            string folderPath = Path.Combine(StoringPath, folderName);

            StringBuilder fileContent = new StringBuilder();
            fileContent.AppendLine($"using {AppName}.Models;");
            fileContent.Append(GenerateInterfaceDefinition(interfaceType));

            string interfaceName = interfaceType == enInterfaceType.Bl ? LogicInterfaceName : DataInterfaceName;
            string fileName = $"{interfaceName}.cs";
            bool success = FileHelper.StoreToFile(fileContent.ToString(), fileName, folderPath, true);

            if (success)
            {
                filePath = Path.Combine(folderPath, fileName);
            }

            return success;
        }

        #endregion

        #region Public Methods

        public bool GenerateBlInterfaceCode(out string filePath)
        {
            return _GenerateInterfaceFile(enInterfaceType.Bl, out filePath);
        }

        public bool GenerateDaInterfaceCode(out string filePath)
        {
            return _GenerateInterfaceFile(enInterfaceType.Da, out filePath);
        }

        #endregion
    }
}