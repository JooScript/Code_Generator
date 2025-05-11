using Utilities;

namespace CodeGenerator_Logic
{
    public class clsGenrator
    {
        public enum enCodeStyle
        {
            EFStyle = 0,
            AdoStyle = 1
        }

        protected static string _TableName;
        protected static string TableId;
        protected static List<clsDatabase.ColumnInfo> columns;
        protected static List<clsDatabase.ForeignKeyInfo> foreignKeys;
        protected static string AppName;

        #region Properties

        public static string TableName
        {
            set
            {
                _TableName = value;
                clsDatabase.Initialize(clsDataAccessSettings.ConnectionString());
                columns = clsDatabase.GetTableColumns(_TableName);
                TableId = clsGlobal.FormatId(clsDatabase.GetFirstPrimaryKey(_TableName));
                foreignKeys = clsDatabase.GetForeignKeys(_TableName);
                AppName = clsDataAccessSettings.AppName();
            }
        }

        protected static string FormattedTNSingle
        {
            get
            {
                if (string.IsNullOrEmpty(_TableName))
                {
                    return null;
                }

                string singularized = clsFormat.Singularize(_TableName) ?? string.Empty;
                return clsFormat.CapitalizeFirstChars(singularized);
            }
        }

        protected static string FormattedTNPluralize
        {
            get
            {
                if (string.IsNullOrEmpty(_TableName))
                {
                    return null;
                }

                string pluralized = clsFormat.Pluralize(_TableName) ?? string.Empty;
                return clsFormat.CapitalizeFirstChars(pluralized);
            }
        }

        protected static string FormattedTNSingleVar
        {
            get
            {
                if (string.IsNullOrEmpty(_TableName))
                {
                    return null;
                }

                string singularized = clsFormat.Singularize(_TableName) ?? string.Empty;
                string capitalized = clsFormat.CapitalizeFirstChars(singularized);
                return clsFormat.SmalizeFirstChar(capitalized);
            }
        }

        protected static string FormattedTNPluralizeVar
        {
            get
            {
                if (string.IsNullOrEmpty(_TableName))
                {
                    return null;
                }

                string pluralized = clsFormat.Pluralize(_TableName) ?? string.Empty;
                string capitalized = clsFormat.CapitalizeFirstChars(pluralized);
                return clsFormat.SmalizeFirstChar(capitalized);
            }
        }

        #endregion

        protected static string GetClassNameFromColumnName(string columnName)
        {

            if (string.IsNullOrEmpty(columnName))
            {
                return string.Empty;
            }

            foreach (var keyTable in foreignKeys)
            {
                if (columnName.ToLower() == keyTable.ColumnName.ToLower())
                {
                    return clsFormat.Singularize(keyTable.ReferencedTable);
                }
            }

            return string.Empty;
        }

    }
}