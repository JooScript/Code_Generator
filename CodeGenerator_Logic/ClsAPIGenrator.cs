using System.Text;
using Utilities;

namespace CodeGenerator_Logic
{
    public class ClsAPIGenerator : ClsGenerator
    {
        private static int _versionNumber = 1;
        private static readonly object _versionLock = new object();

        /// <summary>
        /// Gets or sets the application version number.
        /// </summary>
        /// <value>The current version number of the application.</value>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when setting a negative version number.</exception>
        public static int VersionNumber
        {
            get
            {
                lock (_versionLock)
                {
                    return _versionNumber;
                }
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Version number cannot be negative.");
                }

                lock (_versionLock)
                {
                    _versionNumber = value;
                }
            }
        }

        #region Support Methods

        private static string ControllerName()
        {
            return $"{FormattedTNPluralize}Controller";
        }

        private static string GetValidationChecks(bool forUpdate)
        {
            StringBuilder sb = new StringBuilder();
            string prefix = forUpdate ? "updated" : "new";

            foreach (var column in columns)
            {
                if (column.IsNullable || column.IsIdentity)
                {
                    continue;
                }

                string propertyName = ClsFormat.CapitalizeFirstChars(ClsGlobal.FormatId(column.Name));
                string csharpType = ClsUtil.ConvertDbTypeToCSharpType(column.DataType);

                switch (csharpType)
                {
                    case "string":
                        sb.Append($" || string.IsNullOrEmpty({prefix}{FormattedTNSingle}.{propertyName})");
                        break;

                    case "int":
                    case "long":
                    case "short":
                    case "byte":
                    case "decimal":
                    case "float":
                    case "double":
                        sb.Append($" || {prefix}{FormattedTNSingle}.{propertyName} <= 0");
                        break;

                    default:
                        break;
                }
            }

            return sb.ToString();
        }

        private static string GeneratePropertyAssignments()
        {
            var sb = new StringBuilder();

            foreach (var column in columns)
            {
                string propertyName = ClsFormat.CapitalizeFirstChars(ClsGlobal.FormatId(column.Name));

                if (column.IsPrimaryKey)
                {
                    continue;
                }

                sb.AppendLine($"            {FormattedTNSingleVar}.{propertyName} = updated{FormattedTNSingle}.{propertyName};");
            }

            return sb.ToString();
        }

        #endregion

        #region Class Structure

        private static string TopUsing()
        {
            return $@"using Microsoft.AspNetCore.Mvc;
using {AppName}_Business.BusinessLogic;
using {AppName}_Data.DTO;

namespace {AppName}_API.Controllers
{{
    [Route(""api/v{_versionNumber}/{FormattedTNPluralize}"")]
    [ApiController]
    public class {ControllerName()} : ControllerBase
    {{";
        }

        private static string GetByIdEndpoint()
        {
            return $@"        [HttpGet(""ById/{{Id:int:min(1)}}"", Name = ""Get{FormattedTNSingle}ById"")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<{FormattedTNSingle}DTO>> Get{FormattedTNSingle}ById(int Id)
        {{
            {LogicClsName} {FormattedTNSingleVar} = await {LogicClsName}.FindAsync(Id);

            if ({FormattedTNSingleVar} == null)
            {{
                return NotFound($""{FormattedTNSingle} with ID {{Id}} not found."");
            }}

            return Ok({FormattedTNSingleVar}.DTO);
        }}

";
        }

        private static string CreateEndpoint()
        {
            return $@"        [HttpPost(Name = ""Add{FormattedTNSingle}"")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<{FormattedTNSingle}DTO>> Add{FormattedTNSingle}({FormattedTNSingle}DTO new{FormattedTNSingle})
        {{
            if (new{FormattedTNSingle} == null{GetValidationChecks(false)})
            {{
                return BadRequest(""Invalid {FormattedTNSingleVar} data."");
            }}

            {LogicClsName} {FormattedTNSingleVar} = new {LogicClsName}(new{FormattedTNSingle});
            await {FormattedTNSingleVar}.SaveAsync();
            new{FormattedTNSingle}.Id = {FormattedTNSingleVar}.{TableId};

            return CreatedAtRoute(""Get{FormattedTNSingle}ById"", new {{ id = new{FormattedTNSingle}.Id }}, new{FormattedTNSingle});
        }}

";
        }

        private static string UpdateEndpoint()
        {
            return $@"        [HttpPut(""{{Id:int:min(1)}}"", Name = ""Update{FormattedTNSingle}"")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<{FormattedTNSingle}DTO>> Update{FormattedTNSingle}(int Id, {FormattedTNSingle}DTO updated{FormattedTNSingle})
        {{
            if (updated{FormattedTNSingle} == null{GetValidationChecks(true)})
            {{
                return BadRequest(""Invalid {FormattedTNSingleVar} data."");
            }}

            {LogicClsName} {FormattedTNSingleVar} = await {LogicClsName}.FindAsync(Id);

            if ({FormattedTNSingleVar} == null)
            {{
                return NotFound($""{FormattedTNSingle} with ID {{Id}} not found."");
            }}

            {GeneratePropertyAssignments()}

            await {FormattedTNSingleVar}.SaveAsync();

            return Ok({FormattedTNSingleVar}.DTO);
        }}

";
        }

        private static string GetAllEndpoint()
        {
            return $@"        [HttpGet(""All"", Name = ""GetAll{FormattedTNPluralize}"")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<{FormattedTNSingle}DTO>>> GetAll{FormattedTNPluralize}(int pageNumber = 1, int pageSize = 50)
        {{
            List<{FormattedTNSingle}DTO> {FormattedTNPluralizeVar}List = await {LogicClsName}.GetAll{FormattedTNPluralize}Async(pageNumber, pageSize);
            if ({FormattedTNPluralizeVar}List.Count == 0)
            {{
                return NotFound(""No {FormattedTNPluralize} Found!"");
            }}
            return Ok({FormattedTNPluralizeVar}List);
        }}

";
        }

        private static string IsExistsEndpoint()
        {
            return $@"        [HttpGet(""Exists/{{Id:int:min(1)}}"", Name = ""Is{FormattedTNSingle}Exists"")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Is{FormattedTNSingle}Exists(int Id)
        {{
            bool exists = await {LogicClsName}.Is{FormattedTNSingle}ExistsAsync(Id);
            if (!exists)
            {{
                return NotFound(new {{ Message = ""{FormattedTNSingle} Not Found!"" }});
            }}
            return Ok(new {{ Message = ""{FormattedTNSingle} Exists"" }});
        }}

";
        }

        private static string GetCountEndpoint()
        {
            return $@"        [HttpGet(""Count"", Name = ""Get{FormattedTNPluralize}Count"")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<int>> Get{FormattedTNPluralize}Count()
        {{
            return Ok(await {LogicClsName}.{FormattedTNPluralize}CountAsync());
        }}

";
        }

        private static string DeleteEndpoint()
        {
            return $@"        [HttpDelete(""{{Id:int:min(1)}}"", Name = ""Delete{FormattedTNSingle}"")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete{FormattedTNSingle}(int Id)
        {{        
            if (await {LogicClsName}.Delete{FormattedTNSingle}Async(Id))
            {{
                return Ok($""{FormattedTNSingle} with ID {{Id}} has been deleted."");
            }}
            else
            {{
                return NotFound($""{FormattedTNSingle} with ID {{Id}} not found. No rows deleted!"");
            }}
        }}

";
        }

        private static string Closing()
        {
            return $@"    }}
}}";
        }

        #endregion

        public static bool GenerateControllerCode(string tableName, string? folderPath = null)
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
                folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"Code Generator\\{ClsDataAccessSettings.AppName()}\\Controllers");
            }

            StringBuilder controllerCode = new StringBuilder();

            controllerCode.Append(TopUsing());
            controllerCode.Append(GetByIdEndpoint());
            controllerCode.Append(CreateEndpoint());
            controllerCode.Append(UpdateEndpoint());
            controllerCode.Append(GetAllEndpoint());
            controllerCode.Append(IsExistsEndpoint());
            controllerCode.Append(GetCountEndpoint());
            controllerCode.Append(DeleteEndpoint());
            controllerCode.Append(Closing());

            string fileName = $"{ControllerName()}.cs";
            return ClsFile.StoreToFile(controllerCode.ToString(), fileName, folderPath, true);
        }

    }
}