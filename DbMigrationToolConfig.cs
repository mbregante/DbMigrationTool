using System;
using System.Reflection;
using System.Text.RegularExpressions;


namespace DbMigrationTool
{
    public static class DbMigrationToolConfig
    {
        public static DbConnectionString ConnectionString
        {
            get; set;
        }

        public static DbEnvironmentEnum Environment
        {
            get; set;
        }

        public static Assembly ResourcesAssembly
        {
            get;
            set;
        }

        public static string ResourcesNamespace
        {
            get;
            set;
        }

        public static string ResourcesFilePreffix
        {
            get
            {
                return ResourcesNamespace + ".Resources.";
            }
        }
        public static Regex SchemaResourceRegex
        {
            get
            {
                string pattern = ResourcesFilePreffix + @"DB.\d{4}.\w*.Schema.\w*.sql";
                Regex r = new Regex(pattern);
                return r;
            }
        }

        public static Regex DataResourceRegex
        {
            get
            {
                string pattern = ResourcesFilePreffix + @"DB.\d{4}.\w*.Data.\w*.sql";
                Regex r = new Regex(pattern);
                return r;
            }
        }

        public static Regex IntegrityResourceRegex
        {
            get
            {
                string pattern = ResourcesFilePreffix + @"DB.\w*.Integrity.\w*.sql";
                Regex r = new Regex(pattern);
                return r;
            }
        }

        public static void Init()
        {
            string connectionString = System.Environment.GetEnvironmentVariable("LOCAL_CONNECTION_STRING");
            string dbDeployEnv = System.Environment.GetEnvironmentVariable("DB_DEPLOY_ENV");
            object dbEnv = null;
            if (Enum.TryParse(enumType: typeof(DbEnvironmentEnum), value: dbDeployEnv, out dbEnv))
            {
                DbMigrationToolConfig.Environment = (DbEnvironmentEnum)dbEnv;
            }
            else
            {
                DbMigrationToolConfig.Environment = DbEnvironmentEnum.Default;
            }

            DbConnectionString dbcs = new DbConnectionString(connectionString);
            DbMigrationToolConfig.ConnectionString = dbcs;

            DbMigrationToolConfig.ResourcesAssembly = Assembly.GetExecutingAssembly();
            DbMigrationToolConfig.ResourcesNamespace = Assembly.GetExecutingAssembly().GetName().Name;
        }
    }
}
