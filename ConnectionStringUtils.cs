using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DbMigrationTool
{
    public static class ConnectionStringUtils
    {
        /// <summary>
        /// Takes data source, initial catalog, user and password from the connstr.
        /// </summary>
        /// <param name="fullConnectionString"></param>
        /// <returns></returns>
        public static string BuildForVersioning()
        {
            return DbMigrationToolConfig.ConnectionString.ToString();
        }

        /// <summary>
        /// Takes data source, user and password from the connstr.
        /// </summary>
        /// <param name="fullConnectionString"></param>
        /// <returns></returns>
        public static string BuildForSetup()
        {
            var csb = new System.Data.Common.DbConnectionStringBuilder();
            csb.ConnectionString = DbMigrationToolConfig.ConnectionString.ToString();
            csb.Remove("database");
            csb.Remove("initial catalog");
            csb.Remove("multiple active result sets");
            return csb.ToString();
        }

        
        public static string GetDatasource(string connectionString)
        {
            Regex rx = new Regex(@"(?<=data source=).+?(?=;)");
            Match mt = rx.Match(connectionString);
            return mt.Value;
        }
       
        private static string GetParts(string connectionString, List<string> parts)
        {
            Regex rx = new Regex(@"(?'datasource'(?>data source=).+).+(?'catalog'(?>initial catalog=).+?(?=;)).+(?'user'(?>user id=).+).+(?'password'(?>password=).+?(?=;))");
            Match mt = rx.Match(connectionString.ToLower());

            string allParts = string.Empty;
            foreach (string group in parts)
            {
                if (mt.Groups[group].Success)
                {
                    string value = mt.Groups[group].Value;
                    allParts += value +";";
                }
            }

            return allParts;
        }
    }
}
