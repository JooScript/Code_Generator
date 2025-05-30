using Utilities;

namespace CodeGenerator_Logic
{
    public class Genrator
    {
        public enum enCodeStyle
        {
            EFStyle = 0,
            AdoStyle = 1
        }

        protected static string _TableName;
        protected static string TableId;
        protected static List<DatabaseUtil.ColumnInfo> columns;
        protected static List<DatabaseUtil.ForeignKeyInfo> foreignKeys;
        protected static string AppName;

        #region Properties

        public static string TableName
        {
            get
            {
                return _TableName;
            }
            set
            {
                _TableName = value;
                DatabaseUtil.Initialize(DataAccessSettings.ConnectionString());
                columns = DatabaseUtil.GetTableColumns(_TableName);
                TableId = Global.FormatId(DatabaseUtil.GetFirstPrimaryKey(_TableName));
                foreignKeys = DatabaseUtil.GetForeignKeys(_TableName);
                AppName = DataAccessSettings.AppName();
            }
        }

        protected static string LogicClsName
        {
            get
            {
                if (string.IsNullOrEmpty(_TableName))
                {
                    return null;
                }

                return $"{FormattedTNSingle}Logic";
            }
        }

        protected static string DataClsName
        {
            get
            {
                if (string.IsNullOrEmpty(_TableName))
                {
                    return null;
                }

                return $"{FormattedTNSingle}Data";
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

                string singularized = FormatUtil.Singularize(_TableName) ?? string.Empty;
                return FormatUtil.CapitalizeFirstChars(singularized);
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

                string pluralized = FormatUtil.Pluralize(_TableName) ?? string.Empty;
                return FormatUtil.CapitalizeFirstChars(pluralized);
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

                string singularized = FormatUtil.Singularize(_TableName) ?? string.Empty;
                string capitalized = FormatUtil.CapitalizeFirstChars(singularized);
                return FormatUtil.SmalizeFirstChar(capitalized);
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

                string pluralized = FormatUtil.Pluralize(_TableName) ?? string.Empty;
                string capitalized = FormatUtil.CapitalizeFirstChars(pluralized);
                return FormatUtil.SmalizeFirstChar(capitalized);
            }
        }

        #endregion



    }
}