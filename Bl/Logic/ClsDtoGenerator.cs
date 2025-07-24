using Microsoft.IdentityModel.Tokens;
using System.Text;
using Utilities;

namespace CodeGenerator.Bl
{
    public class ClsDtoGenerator : ClsGenerator
    {
        public ClsDtoGenerator(string tableName) : base(tableName)
        {

        }

        private static string ConstructorAssignments()
        {
            var sb = new StringBuilder();

            foreach (var column in Columns)
            {
                string propertyName = FormatHelper.CapitalizeFirstChars(FormatId(column.Name));

                if (column.IsPrimaryKey)
                {
                    sb.AppendLine($"            this.Id = Id;");
                    continue;
                }

                sb.AppendLine($"            this.{propertyName} = {propertyName};");
            }

            return sb.ToString();
        }

        private static string ConstructorParameters()
        {
            var parameters = new List<string>();
            parameters.Add($"int Id");

            foreach (var column in Columns)
            {
                if (column.IsPrimaryKey)
                {
                    continue;
                }
                string csharpType = Helper.GetCSharpType(column.DataType);
                string nullableSymbol = column.IsNullable ? "?" : "";
                string parameterName = FormatHelper.CapitalizeFirstChars(FormatId(column.Name));

                parameters.Add($"{csharpType}{nullableSymbol} {parameterName}");
            }

            return string.Join(", ", parameters);
        }

        private static string ParameterizedConstructor()
        {
            return $@"        public {DtoClsName}({ConstructorParameters()})
        {{
{ConstructorAssignments()}}}

";
        }

        private static string Properties()
        {
            var sb = new StringBuilder();

            foreach (var column in Columns)
            {
                string csharpType = Helper.GetCSharpType(column.DataType);
                string nullableSymbol = column.IsNullable ? "?" : "";
                string propertyName = FormatHelper.CapitalizeFirstChars(FormatId(column.Name));

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

        public bool GenerateDTO(enCodeStyle codeStyle, out string filePath)
        {
            filePath = null;




            string TopUsing = $@"using {AppName}.Da;

namespace {AppName}.DTO
{{
    public class {DtoClsName}
    {{";

            var dto = new StringBuilder();
            dto.AppendLine(TopUsing);

            if (codeStyle == enCodeStyle.Ado)
            {
                dto.AppendLine(ParameterizedConstructor());
            }

            dto.AppendLine(Properties());
            dto.Append($@"}}}}");

            string folderPath = Path.Combine(StoringPath, "DTO");
            string fileName = $"{DtoClsName}.cs";

            bool success = FileHelper.StoreToFile(dto.ToString(), fileName, folderPath, true);

            if (success)
            {
                filePath = Path.Combine(folderPath, fileName);
            }

            return success;
        }

    }
}