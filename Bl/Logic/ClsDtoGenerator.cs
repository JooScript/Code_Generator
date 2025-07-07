using System.Text;
using Utilities;

namespace CodeGenerator.Bl
{
    public class ClsDtoGenerator : ClsGenerator
    {
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

        public static bool GenerateDTO(string tableName, enCodeStyle codeStyle)
        {
            if (tableName == null)
            {
                return false;
            }
            else
            {
                TableName = tableName;
            }

            string folderPath = Path.Combine(StoringPath, "DTO");

            string TopUsing = $@"using {AppName}.Da;

namespace {AppName}.DTO
{{
    public class {DtoClsName}
    {{";

            var dto = new StringBuilder();
            dto.AppendLine(TopUsing);

            if (codeStyle == enCodeStyle.AdoStyle)
            {
                dto.AppendLine(ParameterizedConstructor());
            }

            dto.AppendLine(Properties());
            dto.Append($@"}}}}");


            return FileHelper.StoreToFile(dto.ToString(), $"{DtoClsName}.cs", folderPath, true);
        }

    }
}


