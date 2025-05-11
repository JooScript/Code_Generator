using System.Text;
using Utilities;

namespace CodeGenerator_Logic
{
    public class clsIlGenerator : clsGenrator
    {
        #region Support Methods

        private static string GetControllerName()
        {
            return $"{FormattedTNPluralize}Controller";
        }

        private static string GetValidationChecks(bool forUpdate)
        {
            var sb = new StringBuilder();

            foreach (var column in columns)
            {
                if (column.IsNullable || column.IsIdentity)
                {
                    continue;
                }

                string propertyName = clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name));
                string csharpType = clsUtil.ConvertDbTypeToCSharpType(column.DataType);

                if (csharpType == "string")
                {
                    if (forUpdate)
                    {
                        sb.Append($" || string.IsNullOrEmpty(updated{FormattedTNSingle}.{propertyName})");
                    }
                    else
                    {
                        sb.Append($" || string.IsNullOrEmpty(new{FormattedTNSingle}.{propertyName})");
                    }
                }
            }

            return sb.ToString();
        }

        private static string GeneratePropertyAssignments()
        {
            var sb = new StringBuilder();

            foreach (var column in columns)
            {
                string propertyName = clsFormat.CapitalizeFirstChars(clsGlobal.FormatId(column.Name));

                if (column.IsPrimaryKey)
                {
                    sb.AppendLine($"            {FormattedTNSingleVar}.{propertyName} = updated{FormattedTNSingle}.Id;");
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
    [Route(""api/{FormattedTNPluralize}"")]
    [ApiController]
    public class {GetControllerName()} : ControllerBase
    {{";
        }

        private static string GetByIdEndpoint()
        {
            return $@"        [HttpGet(""{{{TableId}}}"", Name = ""Get{FormattedTNSingle}ById"")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<{FormattedTNSingle}DTO> Get{FormattedTNSingle}ById(int {TableId})
        {{
            if ({TableId} < 1)
            {{
                return BadRequest($""Not accepted ID {{{TableId}}}"");
            }}

            cls{FormattedTNSingle} {FormattedTNSingleVar} = cls{FormattedTNSingle}.Find({TableId});

            if ({FormattedTNSingleVar} == null)
            {{
                return NotFound($""{FormattedTNSingle} with ID {{{TableId}}} not found."");
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
        public ActionResult<{FormattedTNSingle}DTO> Add{FormattedTNSingle}({FormattedTNSingle}DTO new{FormattedTNSingle})
        {{
            if (new{FormattedTNSingle} == null{GetValidationChecks(false)})
            {{
                return BadRequest(""Invalid {FormattedTNSingleVar} data."");
            }}

            cls{FormattedTNSingle} {FormattedTNSingleVar} = new cls{FormattedTNSingle}(new{FormattedTNSingle});
            {FormattedTNSingleVar}.Save();
            new{FormattedTNSingle}.Id = {FormattedTNSingleVar}.{TableId};

            return CreatedAtRoute(""Get{FormattedTNSingle}ById"", new {{ id = new{FormattedTNSingle}.Id }}, new{FormattedTNSingle});
        }}

";
        }

        private static string UpdateEndpoint()
        {
            return $@"        [HttpPut(""{{Id}}"", Name = ""Update{FormattedTNSingle}"")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<{FormattedTNSingle}DTO> Update{FormattedTNSingle}(int Id, {FormattedTNSingle}DTO updated{FormattedTNSingle})
        {{
            if (Id < 1 || updated{FormattedTNSingle} == null{GetValidationChecks(true)})
            {{
                return BadRequest(""Invalid {FormattedTNSingleVar} data."");
            }}

            cls{FormattedTNSingle} {FormattedTNSingleVar} = cls{FormattedTNSingle}.Find(Id);

            if ({FormattedTNSingleVar} == null)
            {{
                return NotFound($""{FormattedTNSingle} with ID {{Id}} not found."");
            }}

            {GeneratePropertyAssignments()}

            {FormattedTNSingleVar}.Save();

            return Ok({FormattedTNSingleVar}.DTO);
        }}

";
        }

        private static string GetAllEndpoint()
        {
            return $@"        [HttpGet(""All"", Name = ""GetAll{FormattedTNPluralize}"")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<IEnumerable<{FormattedTNSingle}DTO>> GetAll{FormattedTNPluralize}()
        {{
            List<{FormattedTNSingle}DTO> {FormattedTNPluralizeVar}List = cls{FormattedTNSingle}.GetAll{FormattedTNPluralize}();
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
            return $@"        [HttpGet(""Exists/{{id}}"", Name = ""Is{FormattedTNSingle}Exists"")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Is{FormattedTNSingle}Exists(int id)
        {{
            bool exists = await cls{FormattedTNSingle}.Is{FormattedTNSingle}ExistsAsync(id);
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
        public ActionResult<int> Get{FormattedTNPluralize}Count()
        {{
            return Ok(cls{FormattedTNSingle}.{FormattedTNPluralize}Count());
        }}

";
        }

        private static string DeleteEndpoint()
        {
            return $@"        [HttpDelete(""{{Id}}"", Name = ""Delete{FormattedTNSingle}"")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult Delete{FormattedTNSingle}(int Id)
        {{
            if (Id < 1)
            {{
                return BadRequest($""Not accepted ID {{Id}}"");
            }}

            if (cls{FormattedTNSingle}.Delete{FormattedTNSingle}(Id))
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
                    $"Code Generator\\{clsDataAccessSettings.AppName()}\\Controllers");
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

            string fileName = $"{GetControllerName()}.cs";
            return clsFile.StoreToFile(controllerCode.ToString(), fileName, folderPath, true);
        }

    }
}