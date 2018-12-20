using System;
using System.Data.SqlClient;
using System.Linq;

namespace Stef.CleanHtml
{
    public class CleanHtmlSqlOptions
    {
        public CleanHtmlSqlOptions(SqlConnection connection, string tableName, string idColumnName, string htmlColumnName)
        {
            Connection = connection;
            TableName = tableName;
            IdColumnName = idColumnName;
            HtmlColumnName = htmlColumnName;
        }

        public SqlConnection Connection { get; private set; }
        public string TableName { get; private set; }
        public string IdColumnName { get; private set; }
        public string HtmlColumnName { get; private set; }
        public string Where { get; set; }
        public string BackupDirectory { get; set; }
    }
}
