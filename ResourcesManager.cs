using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DbMigrationTool
{
    public static class ResourcesManager
    {
        internal static void LoadScriptResources()
        {
            List<string> resourcesName = DbMigrationToolConfig.ResourcesAssembly.GetManifestResourceNames().ToList();

            List<string> allSchemaScripts = resourcesName.Where(x => DbMigrationToolConfig.SchemaResourceRegex.IsMatch(x)).ToList();
            SchemaScripts = FilterByEnvironment(allSchemaScripts);

            List<string> allDataScripts = resourcesName.Where(x => DbMigrationToolConfig.DataResourceRegex.IsMatch(x)).ToList();
            DataScripts = FilterByEnvironment(allDataScripts);

            List<string> allIntegrityScripts = resourcesName.Where(x => DbMigrationToolConfig.IntegrityResourceRegex.IsMatch(x)).ToList();
            IntegrityScripts = FilterByEnvironment(allIntegrityScripts);

            SchemaScripts.Sort();
            DataScripts.Sort();
        }        

        public static List<string> SchemaScripts
        {
            get;
            private set;
        }

        public static List<string> DataScripts
        {
            get;
            private set;
        }

        public static List<string> IntegrityScripts
        {
            get;
            private set;
        }

        internal static Stream ReadResource(string name)
        {
            return DbMigrationToolConfig.ResourcesAssembly.GetManifestResourceStream("Resources." + name);
        }

        internal static Stream ReadExecutingAssemblyResource(string name)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("DbMigrationTool.Resources." + name);
        }

        /*
        internal static List<string> GetOtherEnvironmentsTag(string thisEnvironment)
        {
            List<string> otherEnvsTags = new List<string>();
            IEnumerable<string> otherEnvs = Enum.GetNames(typeof(DbEnvironmentEnum)).Except(new string[] { thisEnvironment });
            List<string> filteredFixScripts = new List<string>();

            foreach (string envName in otherEnvs)
            {
                string otherEnvTag = envName;
                if (envName.StartsWith("Dev"))
                {
                    otherEnvTag = string.Format(".{0}.", "Dev");
                }
                else if (envName.Contains("Testing"))
                {
                    otherEnvTag = string.Format(".{0}.", "Testing");
                }
                else
                {
                    otherEnvTag = string.Format(".{0}.", envName);
                }

                if (!otherEnvsTags.Contains(otherEnvTag))
                {
                    otherEnvsTags.Add(otherEnvTag);
                }
            }

            return otherEnvsTags;
        }*/

        internal static List<string> FilterByEnvironment(List<string> scriptNames)
        {
            
            List<string> filteredList = new List<string>();
            string keyTagTemplate = ".{0}.";

            List<string> defaultScripts = new List<string>();
            foreach (string scriptName in scriptNames)
            {
                foreach(string tag in DbMigrationToolConfig.ScriptsTags)
                {
                    string keyTag = string.Format(keyTagTemplate, tag);
                    if (scriptName.ToLower().Contains(keyTag))
                    {
                        filteredList.Add(scriptName);
                    }
                }
            }

            return filteredList;
        }
    }    
}
