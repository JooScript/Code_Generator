using Utilities;

namespace CodeGenerator.Bl
{
    public class ClsSPGenerator : ClsGenerator
    {
        public ClsSPGenerator(string tableName) : base(tableName)
        {

        }

        #region SP Structure

        public bool IsExistsSP()
        {
            string procedureName = $"SP_Is{FormattedTNSingle}Exists";

            string procedureBody = $@"
    @{TableId} INT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS(SELECT 1 FROM [{TableName}] WHERE [{TableId}] = @{TableId})
        RETURN 1;
    ELSE
        RETURN 0;
END";

            return DatabaseHelper.CreateStoredProcedure(procedureName, procedureBody);
        }

        public bool CountSP()
        {
            string procedureName = $"SP_{FormattedTNPluralize}Count";

            string procedureBody = $@"
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT COUNT(*) AS TotalCount FROM [{TableName}];
END";

            return DatabaseHelper.CreateStoredProcedure(procedureName, procedureBody);
        }

        public bool GetAllSP()
        {
            string procedureName = $"SP_GetAll{FormattedTNPluralize}";

            string procedureBody = $@"
    @PageNumber INT,
    @PageSize INT
AS
BEGIN
    
    SELECT {GetColumnList()}
    FROM [{TableName}]
    ORDER BY [{TableId}]
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END";

            return DatabaseHelper.CreateStoredProcedure(procedureName, procedureBody);
        }

        public bool GetByIdSP()
        {
            string procedureName = $"SP_Get{FormattedTNSingle}ById";

            string procedureBody = $@"
    @{TableId} INT
AS
BEGIN
    
    SELECT {GetColumnList()}
    FROM [{TableName}]
    WHERE [{TableId}] = @{TableId};
END";

            return DatabaseHelper.CreateStoredProcedure(procedureName, procedureBody);
        }

        public bool AddNewSP()
        {
            string procedureName = $"SP_Add{FormattedTNSingle}";

            string procedureBody = $@"
{GetSPParameters(true)}
AS
BEGIN
    
    INSERT INTO [{TableName}] (
        {GetInsertColumnList()}
    )
    VALUES (
        {GetInsertValuesList()}
    );
    
    SET @New{TableId} = SCOPE_IDENTITY();
END";

            return DatabaseHelper.CreateStoredProcedure(procedureName, procedureBody);
        }

        public bool UpdateSP()
        {
            string procedureName = $"SP_Update{FormattedTNSingle}";

            string procedureBody = $@"
{GetSPParameters(false)}
AS
BEGIN
    
    UPDATE [{TableName}]
    SET
{GetUpdateSetClause()}
    WHERE [{TableId}] = @{TableId};
END";

            return DatabaseHelper.CreateStoredProcedure(procedureName, procedureBody);
        }

        public bool DeleteSP()
        {
            string procedureName = $"SP_Delete{FormattedTNSingle}";

            string procedureBody = $@"
    @{TableId} INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DELETE FROM [{TableName}]
    WHERE [{TableId}] = @{TableId};
    
    SELECT @@ROWCOUNT;
END";

            return DatabaseHelper.CreateStoredProcedure(procedureName, procedureBody);
        }

        public bool FindByCountryNameSP()
        {
            string procedureName = $"SP_Find{FormattedTNSingle}ByCountryName";

            string procedureBody = $@"
    @CountryName NVARCHAR(MAX)
AS
BEGIN  
    SELECT {GetColumnList()}
    FROM [{TableName}]
    WHERE Name = @CountryName;
END";

            return DatabaseHelper.CreateStoredProcedure(procedureName, procedureBody);
        }

        public bool FindByPersonIdSP()
        {
            string procedureName = $"SP_Find{FormattedTNSingle}ByPersonId";

            string procedureBody = $@"
    @PersonId INT
AS
BEGIN
    SELECT {GetColumnList()}
    FROM [{TableName}]
    WHERE PersonId = @PersonId;
END";

            return DatabaseHelper.CreateStoredProcedure(procedureName, procedureBody);
        }

        public bool FindByUsernameAndPasswordSP()
        {
            string procedureName = $"SP_Find{FormattedTNSingle}ByUsernameAndPassword";

            string procedureBody = $@"
    @Username NVARCHAR(50),
    @Password NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT {GetColumnList()}
    FROM [{TableName}]
    WHERE Username = @Username AND Password = @Password;
END";

            return DatabaseHelper.CreateStoredProcedure(procedureName, procedureBody);
        }

        public bool IsExistsByUsernameSP()
        {
            string procedureName = $"SP_Is{FormattedTNSingle}ExistsByUsername";

            string procedureBody = $@"
    @Username NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS(SELECT 1 FROM [{TableName}] WHERE Username = @Username)
        SELECT 1 AS Result;
    ELSE
        SELECT 0 AS Result;
END";

            return DatabaseHelper.CreateStoredProcedure(procedureName, procedureBody);
        }

        public bool IsExistsByPersonIdSP()
        {
            string procedureName = $"SP_Is{FormattedTNSingle}ExistsByPersonId";

            string procedureBody = $@"
    @PersonId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS(SELECT 1 FROM [{TableName}] WHERE PersonId = @PersonId)
        SELECT 1 AS Result;
    ELSE
        SELECT 0 AS Result;
END";

            return DatabaseHelper.CreateStoredProcedure(procedureName, procedureBody);
        }

        #endregion

        #region Support Methods

        private static string GetColumnList()
        {
            return string.Join(", ", Columns.Select(c => $"[{c.Name}]"));
        }

        private static string GetInsertColumnList()
        {
            var columns = ClsGenerator.Columns.Where(c => !c.IsPrimaryKey);
            return string.Join(",\n            ", columns.Select(c => $"[{c.Name}]"));
        }

        private static string GetInsertValuesList()
        {
            var columns = ClsGenerator.Columns.Where(c => !c.IsPrimaryKey);
            return string.Join(",\n            ", columns.Select(c => $"@{c.Name}"));
        }

        private static string GetUpdateSetClause()
        {
            var columns = ClsGenerator.Columns.Where(c => !c.IsPrimaryKey);
            return string.Join(",\n            ", columns.Select(c => $"[{c.Name}] = @{c.Name}"));
        }

        private static string GetSPParameters(bool idAsOutput)
        {
            var parameters = new List<string>();

            if (idAsOutput)
            {
                parameters.Add($"        @New{TableId} INT OUTPUT");
            }
            else
            {
                parameters.Add($"        @{TableId} INT");
            }

            foreach (var column in Columns)
            {
                if (column.IsPrimaryKey)
                {
                    continue;
                }

                string sqlType = GetSqlType(column.DataType);
                string nullability = column.IsNullable ? " = NULL" : "";
                parameters.Add($"        @{column.Name} {sqlType}{nullability}");
            }

            return string.Join(",\n", parameters);
        }

        private static string GetSqlType(string dataType)
        {
            switch (dataType.ToLower())
            {
                case "int":
                case "smallint":
                case "tinyint":
                    return "INT";
                case "varchar":
                case "nvarchar":
                    return $"{dataType.ToUpper()}(MAX)";
                case "datetime":
                    return "DATETIME";
                case "bit":
                    return "BIT";
                case "decimal":
                case "numeric":
                    return "DECIMAL(18, 2)";
                case "float":
                    return "FLOAT";
                case "uniqueidentifier":
                    return "UNIQUEIDENTIFIER";
                case "binary":
                case "varbinary":
                    return "VARBINARY(MAX)";
                default:
                    return dataType.ToUpper();
            }
        }

        #endregion

        public bool GenerateAllSPs()
        {
            bool allSuccess = true;

            allSuccess &= GetAllSP();
            allSuccess &= GetByIdSP();
            allSuccess &= AddNewSP();
            allSuccess &= UpdateSP();
            allSuccess &= DeleteSP();
            allSuccess &= IsExistsSP();
            allSuccess &= CountSP();

            if (FormatHelper.Singularize(TableName.ToLower()) == "user")
            {
                allSuccess &= FindByPersonIdSP();
                allSuccess &= FindByUsernameAndPasswordSP();
                allSuccess &= IsExistsByUsernameSP();
                allSuccess &= IsExistsByPersonIdSP();
            }

            if (FormatHelper.Singularize(TableName.ToLower()) == "country")
            {
                allSuccess &= FindByCountryNameSP();
            }

            return allSuccess;
        }

    }
}