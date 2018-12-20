using System;
using System.Data.SqlClient;

namespace Stef.CleanHtml.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var sqlConnection = new SqlConnection("Server=TIPDEVSQL01;Database=CERP_BECHTER;");
            sqlConnection.Open();

            CleanDynEintrag(sqlConnection);
            CleanBrief(sqlConnection);
        }

        private static void CleanDynEintrag(SqlConnection sqlConnection)
        {
            var sqlOptions = new CleanHtmlSqlOptions(
                sqlConnection,
                "ERP_DYN_EINTRAG",
                "ID",
                "WERT_TEXT")
            {
                BackupDirectory = @"c:\temp\clean-html",
                Where = "ID_DYN_FELD in (select ID from ERP_DYN_FELD where TYP = 4)"
            };

            CleanHtmlManager.Current.Clean(sqlOptions);
        }
        private static void CleanBrief(SqlConnection sqlConnection)
        {
            var sqlOptions = new CleanHtmlSqlOptions(
                sqlConnection,
                "ERP_BRIEF",
                "ID",
                "TEXT_HTML")
            {
                BackupDirectory = @"c:\temp\clean-html"
            };

            CleanHtmlManager.Current.Clean(sqlOptions);
        }
    }
}
