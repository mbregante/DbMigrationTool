using System;
using System.Collections.Generic;
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

        public static List<string> ScriptsTags
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

        public static void Init(string connectionString, List<string> scriptsTags, Assembly resourcesAssembly)
        {
            DbMigrationToolConfig.ScriptsTags = new List<string>(scriptsTags);
            
            DbConnectionString dbcs = new DbConnectionString(connectionString);
            DbMigrationToolConfig.ConnectionString = dbcs;

            DbMigrationToolConfig.ResourcesAssembly = resourcesAssembly; // Assembly.GetEntryAssembly();
            DbMigrationToolConfig.ResourcesNamespace = resourcesAssembly.GetName().Name;
        }
    }
}
