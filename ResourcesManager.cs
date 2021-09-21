using System;
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
            SchemaScripts = FilterByEnvironment(allSchemaScripts, includeCommon: true);

            List<string> allDataScripts = resourcesName.Where(x => DbMigrationToolConfig.DataResourceRegex.IsMatch(x)).ToList();
            DataScripts = FilterByEnvironment(allDataScripts, includeCommon: false);

            List<string> allIntegrityScripts = resourcesName.Where(x => DbMigrationToolConfig.IntegrityResourceRegex.IsMatch(x)).ToList();
            IntegrityScripts = FilterByEnvironment(allIntegrityScripts, includeCommon: false);

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
        }

        internal static List<string> FilterByEnvironment(List<string> scriptNames, bool includeCommon = false)
        {
            string environmentName = DbMigrationToolConfig.Environment.ToString();
            List<string> filteredList = new List<string>();
            string thisKeyTag = string.Format(".{0}.", environmentName);
            string defaultKeyTag = string.Format(".{0}.", DbEnvironmentEnum.Default);
            List<string> otherKeysTag = GetOtherEnvironmentsTag(environmentName);

            List<string> defaultScripts = new List<string>();
            foreach (string scriptName in scriptNames)
            {
                if (DbMigrationToolConfig.Environment == DbEnvironmentEnum.Default)
                {
                    // if not a specific environment, only take scripts that AREN'T environment specific.

                    if ( !otherKeysTag.Any( o => scriptName.Contains(o)))
                    {
                        filteredList.Add(scriptName);
                    }
                }
                else
                {
                    if (includeCommon && scriptName.Contains(defaultKeyTag))
                    {
                        defaultScripts.Add(scriptName);
                    }

                    if (scriptName.Contains(thisKeyTag))
                    {
                        filteredList.Add(scriptName);
                    }
                }
            }

            if (filteredList.Count == 0)
            {
                filteredList.AddRange(defaultScripts);
            }

            return filteredList;
        }
    }    
}
