using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DbMigrationTool
{
    public class DBStatusController
    {
        private static object s_lock = new object();

        private const string SCRIPT_ID = "DECLARE @ScriptId INT = {0};";
        private const string SCRIPT_REGISTRATION_INSERT = "INSERT INTO SYSTEM_VERSIONING(ScriptId,Name,Version,CreationDay,ImpactedDay) VALUES (@ScriptId,@Name,@Version,@CreationDay,GETDATE())";
        private const string SCRIPT_TAG = "@SQL::";
        private const string MESSAGE_TAG = "@MESSAGE::";
        private const string WARNING_TAG = "@WARNING::";

        private List<Script> m_allScripts = new List<Script>();
        private List<Script> m_allScriptsToExecute = new List<Script>();

        private SystemVersioningBroker m_broker;
        private DatabaseSetupService m_setupService;
        private string m_dbName;

        public bool DatabaseDoesNotExists { get; private set; }
        public bool DatabaseNeedsUpdate { get; private set; }
        public bool StatusLoaded { get; private set; }
        public bool HasWarnings { get; private set; }
        public List<Script> SchemaScripts { get; private set; }
        public List<Script> DataScripts { get; private set; }
        public List<Script> IntegrityScripts { get; private set; }
        public List<ScriptLog> ScriptsLog { get { return Logger.ScriptsLog; } }

        internal DBStatusController()
        {
            string versioningConnString = ConnectionStringUtils.BuildForVersioning();
            SqlServerManager repo = new SqlServerManager(versioningConnString);
            m_setupService = new DatabaseSetupService(); 
           
            m_broker = new SystemVersioningBroker(repo);
            m_dbName = DbMigrationToolConfig.ConnectionString.Database;
            SetUp();
        }

        internal DBStatusController(SqlServerManager manager, string dbName)
        {
            m_setupService = new DatabaseSetupService(manager);
            m_broker = new SystemVersioningBroker(manager);
            m_dbName = dbName;
            SetUp();
        }

        private OperationResult SetUp()
        {
            if (!m_setupService.DatabaseExists(m_dbName))
            {
                DatabaseDoesNotExists = true;
                return new OperationFailed(info: string.Format("Database '{0}' does not exists.", m_dbName));
            }

            if (!m_broker.SystemVersioningTableExists())
            {
                Script setupScript = new Script();
                setupScript.Name = "System Versioning schema setup";
                setupScript.SqlCode = new StreamReader(ResourcesManager.ReadExecutingAssemblyResource("DB.SystemVersioningSetUp.sql")).ReadToEnd();
                return m_broker.ExecuteScript(setupScript, executeWithReader: false);
            }

            return new OperationOk(info: "No need to run system versioning table setup.");
        }

        private List<Script> GetScriptsFromResourceList(List<string> scriptsNames, bool loadCustomFields = true)
        {
            List<Script> scripts = new List<Script>();
            foreach (string scriptName in scriptsNames)
            {
                Script script = new Script();
                scripts.Add(script);

                using (var stream = DbMigrationToolConfig.ResourcesAssembly.GetManifestResourceStream(scriptName))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        script.FileName = scriptName;
                        var allText = reader.ReadToEnd();
                        allText = allText.Replace('\r', ' ');
                        var sqlLines = allText.Split('\n').ToList();

                        for (int i = 0; i < sqlLines.Count(); i++)
                        {
                            if (sqlLines[i].Trim() == string.Empty)
                            {
                                sqlLines.RemoveAt(i);
                                i--;
                                continue;
                            }

                            sqlLines[i] = sqlLines[i].TrimStart(' ');
                            sqlLines[i] = sqlLines[i].TrimEnd(' ');
                        }

                        if (sqlLines.Count == 0)
                        {
                            Logger.Log(script, LogScriptMessageEnum.MissingDataHeader);
                        }
                        else if (loadCustomFields)
                        {
                            ReadScriptFields(sqlLines, script);
                        }

                        if (!Logger.HasErrors(script.FileName))
                        {
                            m_allScripts.Add(script);
                            script.SqlCode = string.Join(Environment.NewLine, sqlLines);
                        }
                    }
                }
            }

            return scripts;
        }

        private List<string> GetTokenValues(string[] lines, string token)
        {
            List<string> values = new List<string>();
            foreach (string line in lines)
            {
                if (line.Contains(token))
                {
                    int idxStart = line.IndexOf(token) + token.Length;
                    int len = line.Length - idxStart;
                    string value = line.Substring(idxStart, len);
                    values.Add(value);
                }
            }

            return values;
        }

        private void UpdateScriptsData()
        {
            List<Script> scripts = new List<Script>();
            scripts.AddRange(SchemaScripts);
            scripts.AddRange(DataScripts);
            scripts.Sort(new ScriptComparer());

            foreach (Script s in scripts)
            {
                SystemVersioning versioningData = m_broker.GetById(s.Id);
                if (versioningData == null)
                {
                    s.ExecutionRequiredForThisDatabase = true;
                }
                else
                {
                    s.ExecutionRequiredForThisDatabase = false;
                    s.ImpactedDate = versioningData.ImpactedDate;
                }
            }

            m_allScripts.Sort(new ScriptComparer());
            m_allScriptsToExecute = m_allScripts.Where(s => s.ExecutionRequiredForThisDatabase).ToList();
            m_allScriptsToExecute.Sort(new ScriptComparer());
        }

        private OperationResult ExecuteScripts(List<Script> scripts)
        {
            if (scripts == null)
            {
                return new OperationOk();
            }

            foreach (Script script in scripts)
            {
                OperationResult rslt = script.IsIntegrityType ? m_broker.ExecuteScript(script, executeWithReader: true) : m_broker.ExecuteScript(script, executeWithReader: false);
                if (rslt.Success)
                {
                    List<string> results = rslt.Data as List<string>;
                    if (results.Count > 0)
                    {
                        script.Messages = GetTokenValues(results.ToArray(), MESSAGE_TAG);
                        script.Warnings = GetTokenValues(results.ToArray(), WARNING_TAG);
                    }
                }
            }

            UpdateDatabaseServerInfo();

            return new OperationOk();
        }

        private void ReadScriptFields(List<string> sqlLines, Script script)
        {
            if (script.IsSchemaType || script.IsDataType)
            {
                ValidateName(sqlLines, script, '=');
                ValidateVersion(sqlLines, script, useDeclarePrefix: true);
                ValidateDate(sqlLines, script, useDeclarePrefix: true, tagName: "CreationDay");

                AddScriptId(sqlLines, script);
                AddScriptRegistration(sqlLines, script);
            }
            else if (script.IsIntegrityType)
            {
                script.Name = script.FileName.Replace(DbMigrationToolConfig.ResourcesFilePreffix, string.Empty);
            }

            if (sqlLines.Any(x => x.Trim().StartsWith("USE")))
            {
                Logger.Log(script, LogScriptMessageEnum.DBDeclaration);
            }
        }

        private void AddScriptId(List<string> sqlLines, Script script)
        {
            Regex idRegex = new Regex(@"\d{4}");
            int id;
            if (int.TryParse(idRegex.Match(script.FileName).Value, out id))
            {
                script.Id = id;
                string scriptId = string.Format(SCRIPT_ID, id.ToString());
                if (!sqlLines.Contains(scriptId)) 
                {
                    sqlLines.Insert(0, scriptId);
                    script.Id = id;
                }
            }
            else
            {
                throw new Exception("Cannot read script id from file name!");
            }
        }

        private void AddScriptRegistration(List<string> sqlLines, Script script)
        {
            if (!sqlLines.Contains(SCRIPT_REGISTRATION_INSERT))
            {
                sqlLines.Insert(4, SCRIPT_REGISTRATION_INSERT);
            }            
        }

        private void ValidateDate(List<string> sqlLines, Script script, bool useDeclarePrefix, string tagName = "Date")
        {
            string tag = (useDeclarePrefix) ? ("DECLARE @" + tagName) : tagName;
            int n = 2;

            if (sqlLines[n].StartsWith(tag))
            {
                DateTime creationDate;
                string separator = useDeclarePrefix ? " DATETIME = " : ":";
                string[] dateLineSplit = sqlLines[n].Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries);
                Regex regex = new Regex(@"'\d{4}[.]\d{2}[.]\d{2}'"); // matches '2019.05.14'
                string dateString = string.Empty;
                Match match = regex.Match(sqlLines[n]);
                if (match.Success)
                {
                    dateString = match.Value.Replace("'", string.Empty); 
                }

                if (DateTime.TryParse(dateString, out creationDate))
                {
                    script.CreationDate = creationDate;
                }
                else
                {
                    Logger.Log(script, LogScriptMessageEnum.DateValueInvalid);
                }
            }
            else
            {
                Logger.Log(script, LogScriptMessageEnum.DateMissing);
            }
        }

        private void ValidateVersion(List<string> sqlLines, Script script, bool useDeclarePrefix)
        {
            string _tag = "Version";
            int n = 1;
            string tag = (useDeclarePrefix) ? ("DECLARE @" + _tag) : _tag;
            string separator = useDeclarePrefix ? " INT =" : ":";
            if (sqlLines[n].StartsWith(tag))
            {
                int version;
                string[] versionLineSplit = sqlLines[n].Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries);
                if (int.TryParse(versionLineSplit[1].Replace(";", string.Empty).Trim(), out version))
                {
                    script.Version = version;
                }
                else
                {
                    Logger.Log(script, LogScriptMessageEnum.VersionValueInvalid);
                }
            }
            else
            {
                Logger.Log(script, LogScriptMessageEnum.VersionMissing);
            }
        }

        private void ValidateName(List<string> sqlLines, Script script, char delimitator, bool useDeclarePrefix = false)
        {
            string _tag = "Name";
            int n = 0;
            string tag = useDeclarePrefix ? "DECLARE @" + _tag : _tag;
            if (sqlLines[n].Contains(tag))
            {
                var name = sqlLines[n].Split(delimitator)[1].Trim();
                if (name.Length > 5)
                {
                    script.Name = name.Replace("'", string.Empty).Replace(";", string.Empty).Trim();
                }
                else
                {
                    Logger.Log(script, LogScriptMessageEnum.NameValueInvalid);
                }
            }
            else
            {
                Logger.Log(script, LogScriptMessageEnum.NameMissing);
            }
        }

        public void LoadStatus()
        {
            lock (s_lock)
            {
                DatabaseDoesNotExists = !m_setupService.DatabaseExists(m_dbName);
                DatabaseNeedsUpdate = true;
                StatusLoaded = false;
                m_allScripts = new List<Script>();

                ResourcesManager.LoadScriptResources();

                SchemaScripts = GetScriptsFromResourceList(ResourcesManager.SchemaScripts);
                DataScripts = GetScriptsFromResourceList(ResourcesManager.DataScripts);
                IntegrityScripts = GetScriptsFromResourceList(ResourcesManager.IntegrityScripts);

                if (DatabaseDoesNotExists)
                {
                    SchemaScripts.ForEach(s => s.ExecutionRequiredForThisDatabase = true);
                }
                else
                {                    
                    UpdateScriptsData();
                    DatabaseNeedsUpdate = m_allScripts.Any(s => s.ExecutionRequiredForThisDatabase);
                    
                    if (!DatabaseNeedsUpdate)
                    {
                        ExecuteIntegrityScripts();
                    }
                    
                    HasWarnings = m_allScripts.Any(s => s.Warnings.Count > 0);

                    UpdateDatabaseServerInfo();
                }

                
                StatusLoaded = true;
            }
        }

        public OperationResult ExecuteSchemaUpdates()
        {
            lock (s_lock)
            {
                List<Script> scripts = SchemaScripts.Where(s => s.ExecutionRequiredForThisDatabase).ToList();
                return ExecuteScripts(scripts);
            }
        }

        public OperationResult ExecuteDataScripts()
        {
            lock (s_lock)
            {
                List<Script> scripts = DataScripts.Where(s => s.ExecutionRequiredForThisDatabase).ToList();
                return ExecuteScripts(scripts);
            }
        }

        public OperationResult ExecuteIntegrityScripts()
        {
            lock (s_lock)
            {
                return ExecuteScripts(IntegrityScripts);
            }
        }

        public OperationResult ExecuteAllPendingScripts()
        {
            lock (s_lock)
            {
                return ExecuteScripts(m_allScriptsToExecute);
            }
        }

        public OperationResult ExecuteScript(DatabaseVersioningScriptExec request)
        {
            lock (s_lock)
            {
                foreach (Script s in m_allScripts)
                {
                    if (s.FileName.Equals(request.FileName))
                    {
                        return ExecuteScripts(new List<Script>() { s });
                    }
                }

                return new OperationFailed(info: "Cannot find script with file name: " + request.FileName);
            }
        }

        public List<Script> GetScripts()
        {
            var list = new List<Script>();
            if (SchemaScripts != null)
            {
                list.AddRange(SchemaScripts);
            }

            if (DataScripts != null)
            {
                list.AddRange(DataScripts);
            }

            list.Sort(new ScriptComparer());

            if ( IntegrityScripts != null)
            {
                list.AddRange(IntegrityScripts);
            }
            
            return list;
        }

        private class ScriptComparer : IComparer<Script>
        {
            public int Compare(Script x, Script y)
            {
                return x.Id.CompareTo(y.Id);
            }
        }

        public void UpdateDatabaseServerInfo()
        {
            DatabaseInfo di = null;
            DatabaseServerInfo si = null;
            var appInfo = ApplicationDatabaseStatusInfo.Instance;
            m_broker.SqlServerRepository.GetDatabaseInfo(out di, out si, generateScript: ApplicationDatabaseStatusInfo.GenerateDbScripts);

            if (appInfo.DatabaseInfo != null && !string.IsNullOrEmpty(appInfo.DatabaseInfo.SchemaScript))
            {
                di.SchemaScript = appInfo.DatabaseInfo.SchemaScript;
            }

            appInfo.DatabaseInfo = di;
            appInfo.DatabaseServerInfo = si;
        }
    }
}
