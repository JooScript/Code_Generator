using System.Security.AccessControl;
using System.Text;
using Utilities;

namespace CodeGenerator.Bl
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

        private static string ControllerName
        {
            get
            {
                return $"{FormattedTNPluralize}Controller";
            }
        }

        private static string GetValidationChecks(bool forUpdate)
        {
            StringBuilder sb = new StringBuilder();
            string prefix = forUpdate ? "updated" : "new";

            foreach (var column in Columns)
            {
                if (column.IsNullable || column.IsIdentity)
                {
                    continue;
                }

                string propertyName = FormatHelper.CapitalizeFirstChars(FormatId(column.Name));
                string csharpType = Helper.GetCSharpType(column.DataType);

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

            foreach (var column in Columns)
            {
                string propertyName = FormatHelper.CapitalizeFirstChars(FormatId(column.Name));

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

        private static string TopUsing(enCodeStyle codeStyle)
        {
            string models = codeStyle == enCodeStyle.AdoStyle ? "" : $"using {AppName}.Models;{Environment.NewLine}using AutoMapper;{Environment.NewLine}";
            return $@"using Microsoft.AspNetCore.Mvc;
using {AppName}.Bl;
using {AppName}.DTO;
{models}
namespace {AppName}.Controllers
{{
    [Route(""api/v{_versionNumber}/{FormattedTNPluralize}"")]
    [ApiController]
    public class {ControllerName} : ControllerBase
    {{";
        }

        private static string EFConstructor()
        {
            return @$"        {LogicInterfaceName} {LogicObjName};
        private readonly IMapper _mapper;
        public {ControllerName}({LogicInterfaceName} {FormattedTNSingleVar}, IMapper mapper)
        {{
            {LogicObjName} = {FormattedTNSingleVar};
            _mapper = mapper;
        }}";
        }

        private static string GetByIdEndpoint(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.EFStyle:
                    {

                        return @$"        [HttpGet(""ById/{{Id:int:min(1)}}"", Name = ""Get{FormattedTNSingle}ById"")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<{DtoClsName}>> Get{FormattedTNSingle}ById(int Id)
        {{
            {ModelName} {FormattedTNSingleVar} = await {LogicObjName}.FindAsync(Id);

            if ({FormattedTNSingleVar} == null)
            {{
                return NotFound($""{FormattedTNSingle} with ID {{Id}} not found."");
            }}

            return Ok(_mapper.Map<{DtoClsName}>({FormattedTNSingleVar}));
        }}";

                    }
                case enCodeStyle.AdoStyle:
                    { return $@"        [HttpGet(""ById/{{Id:int:min(1)}}"", Name = ""Get{FormattedTNSingle}ById"")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<{DtoClsName}>> Get{FormattedTNSingle}ById(int Id)
        {{
            {LogicClsName} {FormattedTNSingleVar} = await {LogicClsName}.FindAsync(Id);

            if ({FormattedTNSingleVar} == null)
            {{
                return NotFound($""{FormattedTNSingle} with ID {{Id}} not found."");
            }}

            return Ok({FormattedTNSingleVar}.DTO);
        }}

"; }
                default:
                    throw new NotSupportedException($"The code style '{codeStyle}' is not supported.");
            }
        }

        private static string CreateEndpoint(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.AdoStyle:
                    { return $@"        [HttpPost(Name = ""Add{FormattedTNSingle}"")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<{DtoClsName}>> Add{FormattedTNSingle}({DtoClsName} new{FormattedTNSingle})
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

"; }
                case enCodeStyle.EFStyle:
                    { return @$"        [HttpPost(Name = ""Add{FormattedTNSingle}"")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<{DtoClsName}>> Add{FormattedTNSingle}({DtoClsName} new{FormattedTNSingle})
        {{
            if (!ModelState.IsValid)
            {{
                return BadRequest(""Invalid {FormattedTNSingleVar} data."");
            }}

            int {FormattedTNSingleVar}Id = await {LogicObjName}.AddAsync(_mapper.Map<{ModelName}>(new{FormattedTNSingle}));

            if ({FormattedTNSingleVar}Id <= 0)
            {{
                return BadRequest(""Failed to add {FormattedTNSingleVar}."");
            }}

            new{FormattedTNSingle}.Id = {FormattedTNSingleVar}Id;

            return CreatedAtRoute(""Get{FormattedTNSingle}ById"", new {{ id = {FormattedTNSingleVar}Id }}, new{FormattedTNSingle});
        }}"; }
                default:
                    {
                        throw new NotSupportedException($"The code style '{codeStyle}' is not supported.");
                    }
            }
        }

        private static string UpdateEndpoint(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.AdoStyle:
                    {
                        return $@"        [HttpPut(""{{Id:int:min(1)}}"", Name = ""Update{FormattedTNSingle}"")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<{DtoClsName}>> Update{FormattedTNSingle}(int Id, {DtoClsName} updated{FormattedTNSingle})
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
                case enCodeStyle.EFStyle:
                    {
                        return @$"        [HttpPut(""{{Id:int:min(1)}}"", Name = ""Update{FormattedTNSingle}"")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<{DtoClsName}>> Update{FormattedTNSingle}(int Id, {DtoClsName} updated{FormattedTNSingle})
        {{
            if (!ModelState.IsValid)
            {{
                return BadRequest(""Invalid {FormattedTNSingleVar} data."");
            }}

            if (await {LogicObjName}.IsExistsAsync(Id))
            {{
                return NotFound($""{FormattedTNSingle} with ID {{Id}} not found."");
            }}

            updated{FormattedTNSingle}.Id = Id;

            if (!await {LogicObjName}.UpdateAsync(_mapper.Map<{ModelName}>(updated{FormattedTNSingle})))
            {{
                return BadRequest(""Failed to update {FormattedTNSingleVar}."");
            }}

            return Ok(updated{FormattedTNSingle});
        }}";
                    }
                default:
                    throw new NotSupportedException($"The code style '{codeStyle}' is not supported.");
            }
        }

        private static string GetAllEndpoint(enCodeStyle codeStyle)
        {
            switch (codeStyle)
            {
                case enCodeStyle.AdoStyle:
                    {
                        return $@"        [HttpGet(""All"", Name = ""GetAll{FormattedTNPluralize}"")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<{DtoClsName}>>> GetAll{FormattedTNPluralize}(int pageNumber = 1, int pageSize = 50)
        {{
            List<{DtoClsName}> {FormattedTNPluralizeVar}List = await {LogicClsName}.GetAll{FormattedTNPluralize}Async(pageNumber, pageSize);
            if ({FormattedTNPluralizeVar}List.Count == 0)
            {{
                return NotFound(""No {FormattedTNPluralize} Found!"");
            }}
            return Ok({FormattedTNPluralizeVar}List);
        }}

";
                    }
                case enCodeStyle:
                    {
                        return @$"        [HttpGet(""All"", Name = ""GetAll{FormattedTNPluralize}"")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<{DtoClsName}>>> GetAll{FormattedTNPluralize}(int pageNumber = 1, int pageSize = 50)
        {{
            var {FormattedTNPluralizeVar}List = await {LogicObjName}.GetAllAsync(pageNumber, pageSize);
            if ({FormattedTNPluralizeVar}List.Count == 0)
            {{
                return NotFound(""No {FormattedTNPluralize} Found!"");
            }}
            return Ok(_mapper.Map<IEnumerable<{DtoClsName}>>({FormattedTNPluralizeVar}List));
        }}";
                    }
                default: throw new NotSupportedException($"The code style '{codeStyle}' is not supported.");
            }
        }

        private static string IsExistsEndpoint(enCodeStyle codeStyle)
        {
            var objectName = codeStyle == enCodeStyle.EFStyle ? LogicObjName : LogicClsName;

            return $@"        [HttpGet(""IsExists/{{Id:int:min(1)}}"", Name = ""Is{FormattedTNSingle}Exists"")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Is{FormattedTNSingle}Exists(int Id)
        {{
            bool exists = await {objectName}.IsExistsAsync(Id);
            if (!exists)
            {{
                return NotFound(new {{ Message = ""{FormattedTNSingle} Not Found!"" }});
            }}
            return Ok(new {{ Message = ""{FormattedTNSingle} Exists"" }});
        }}

";
        }

        private static string GetCountEndpoint(enCodeStyle codeStyle)
        {
            var objectName = codeStyle == enCodeStyle.EFStyle ? LogicObjName : LogicClsName;

            return $@"        [HttpGet(""Count"", Name = ""Get{FormattedTNPluralize}Count"")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<int>> Get{FormattedTNPluralize}Count()
        {{
            return Ok(await {objectName}.CountAsync());
        }}

";
        }

        private static string DeleteEndpoint(enCodeStyle codeStyle)
        {
            var objectName = codeStyle == enCodeStyle.EFStyle ? LogicObjName : LogicClsName;
            return $@"        [HttpDelete(""{{Id:int:min(1)}}"", Name = ""Delete{FormattedTNSingle}"")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete{FormattedTNSingle}(int Id)
        {{        
            if (await {objectName}.DeleteAsync(Id))
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

        private static string Closing(enCodeStyle codeStyle)
        {
            return $@"    }}
}}";
        }

        #endregion

        #region Mapping & DI

        public static bool GenerateMappingCode(string tableName)
        {
            if (tableName == null)
            {
                return false;
            }
            else
            {
                TableName = tableName;
            }
            StringBuilder mappingCode = new StringBuilder();

            mappingCode.Append(@$"CreateMap<{ModelName}, {DtoClsName}>().ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.{TableId})).ReverseMap();{Environment.NewLine}");
            string folderPath = Path.Combine(StoringPath);
            string fileName = $"Mapping.txt";
            return FileHelper.StoreToFile(mappingCode.ToString(), fileName, folderPath, false);
        }

        public static bool GenerateDIode(string tableName)
        {
            if (tableName == null)
            {
                return false;
            }
            else
            {
                TableName = tableName;
            }
            StringBuilder DiCode = new StringBuilder();

            DiCode.Append(@$"builder.Services.AddScoped<{LogicInterfaceName}, {LogicClsName}>();{Environment.NewLine}");
            string folderPath = Path.Combine(StoringPath);
            string fileName = $"Dependency_Injection.txt";
            return FileHelper.StoreToFile(DiCode.ToString(), fileName, folderPath, false);
        }

        #endregion

        public static bool GenerateControllerCode(string tableName, enCodeStyle codeStyle)
        {
            if (tableName == null)
            {
                return false;
            }
            else
            {
                TableName = tableName;
            }

            StringBuilder controllerCode = new StringBuilder();

            controllerCode.Append(TopUsing(codeStyle));

            if (codeStyle == enCodeStyle.EFStyle)
            {
                if (!GenerateDIode(tableName) || !GenerateMappingCode(tableName))
                {
                    Helper.ErrorLogger(new Exception("Failed to generate DI or Mapping code."));
                    return false;
                }
                controllerCode.Append(EFConstructor());
            }

            controllerCode.Append(GetByIdEndpoint(codeStyle));
            controllerCode.Append(CreateEndpoint(codeStyle));
            controllerCode.Append(UpdateEndpoint(codeStyle));
            controllerCode.Append(GetAllEndpoint(codeStyle));
            controllerCode.Append(IsExistsEndpoint(codeStyle));
            controllerCode.Append(GetCountEndpoint(codeStyle));
            controllerCode.Append(DeleteEndpoint(codeStyle));
            controllerCode.Append(Closing(codeStyle));

            string folderPath = Path.Combine(StoringPath, "Controllers");
            string fileName = $"{ControllerName}.cs";
            return FileHelper.StoreToFile(controllerCode.ToString(), fileName, folderPath, true);
        }

    }
}