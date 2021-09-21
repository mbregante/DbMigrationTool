using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Linq;

namespace DbMigrationTool
{
    internal delegate object SqlReaderHandler(SqlDataReader dataReader);
    internal delegate object DatabaseCommandHandler(SqlCommand sqlCommand, Dictionary<string, object> parameters);

    internal static class DataReaderExtensions
    {
        internal static bool HasColumn(this IDataReader reader, string fieldName)
        {
            reader.GetSchemaTable().DefaultView.RowFilter = string.Format("ColumnName= '{0}'", fieldName);
            return (reader.GetSchemaTable().DefaultView.Count > 0);
        }

        internal static bool IsNotNull(this IDataReader reader, string fieldName)
        {
            return !reader.IsDBNull(reader.GetOrdinal(fieldName));
        }

        /// <summary>
        /// Gets the value or a string empty if null.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        internal static string GetStringValue(this IDataReader reader, string fieldName)
        {
            return reader.IsNotNull(fieldName) ? reader.GetString(reader.GetOrdinal(fieldName)) : string.Empty;
        }
    }

    internal class SqlServerManager
    {
        private string[] m_reservedWords = new string[] { "order", "group" };
        private string m_connStr = "";
        private SqlConnection m_currentConnection;

        internal SqlServerManager(string connectionString)
        {
            SetConnectionString(connectionString: connectionString);
        }

        private void SetConnectionString(string connectionString)
        {
            m_connStr = connectionString;
        }

        internal string GetConnectionString()
        {
            return m_connStr;
        }

        internal SqlConnection CreateConnection(bool createAndOpen = true, string connectionString = "", bool scoped = false)
        {
            SqlConnection conn = new SqlConnection(connectionString);
            try
            {
                if (createAndOpen)
                {
                    conn.Open();
                }

                m_currentConnection = conn;

                return conn;
            }
            catch (SqlException ex)
            {
                throw new Exception("Could not connect to the database.", ex);
            }
        }

        internal bool NewConectionIsRequired()
        {
            if (m_currentConnection == null || m_currentConnection.State != ConnectionState.Open)
            {
                //if my connection is null or unopened I will have to create a new one
                return true;
            }

            //If my connection is open and with a connection string I do not need a new one.
            return false;
        }

        internal object ExecuteDatabaseCommand(string commandText, DatabaseCommandHandler handler, Dictionary<string, object> parameters = null, string connectionString = "", CommandType type = CommandType.StoredProcedure)
        {
            if (NewConectionIsRequired())
            {
                using (SqlConnection conn = CreateConnection(connectionString: m_connStr))
                {
                    using (SqlCommand cmd = CreateDatabaseCommand(commandText, conn, parameters, type))
                    {
                        return handler(cmd, parameters);
                    }
                }
            }
            else
            {
                using (SqlCommand cmd = CreateDatabaseCommand(commandText, m_currentConnection, parameters, type))
                {
                    return handler(cmd, parameters);
                }
            }
        }

        internal T ExecuteDatabaseCommand<T>(string commandText, SqlReaderHandler readerHandler, Dictionary<string, object> parameters = null) where T : new()
        {
            object rslt = ExecuteDatabaseCommand(commandText, readerHandler, parameters);
            if (rslt != null)
            {
                return (T)rslt;
            }

            bool isCollection = typeof(T).GetInterfaces()[0].Name.StartsWith("IList");
            if (isCollection)
            {
                return new T(); // return empty collection to avoid possible iterations over null ref.
            }

            return default(T); // if not a collection, return null.
        }

        internal object ExecuteDatabaseCommand(string commandText, SqlReaderHandler readerHandler, Dictionary<string, object> parameters = null)
        {
            if (NewConectionIsRequired())
            {
                using (SqlConnection newConnection = CreateConnection(connectionString: m_connStr))
                {
                    if (newConnection == null)
                    {
                        throw new System.Exception("No se puede abrir la conexión a la base de datos [connstr::empty]", null);
                    }

                    return ExecuteReader(commandText: commandText, connection: newConnection, readerHandler: readerHandler, parameters: parameters, type: CommandType.Text);
                }
            }
            else
            {
                return ExecuteReader(commandText: commandText, connection: m_currentConnection, readerHandler: readerHandler, parameters: parameters, type: CommandType.Text);
            }
        }


        private object ExecuteReader(string commandText, SqlConnection connection, SqlReaderHandler readerHandler, Dictionary<string, object> parameters, CommandType type)
        {
            string minifiedCommandText = commandText.Replace("\n", string.Empty);

            using (SqlCommand cmd = CreateDatabaseCommand(commandText: minifiedCommandText, conn: connection, parameters: parameters, type: CommandType.Text))
            {
                return ExecuteReader(cmd, readerHandler);
            }
        }

        private object ExecuteReader(SqlCommand cmd, SqlReaderHandler readerHandler)
        {
            SqlDataReader reader = cmd.ExecuteReader();
            object result = null;
            if (reader.HasRows)
            {
                result = readerHandler(reader);
            }

            reader.Close();
            return result;
        }

        internal void ExecuteCommand(string commandText, List<SqlParameter> parameters = null)
        {
            if (NewConectionIsRequired())
            {
                using (SqlConnection conn = CreateConnection(connectionString: m_connStr))
                {
                    if (conn == null)
                    {
                        throw new System.Exception("No se puede abrir la conexión a la base de datos [connstr::empty]", null);
                    }

                    using (SqlCommand cmd = CreateDatabaseCommand(commandText, conn))
                    {
                        if (parameters != null && parameters.Count > 0)
                        {
                            foreach (var p in parameters)
                            {
                                cmd.Parameters.Add(p);
                            }
                        }

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                using (SqlCommand cmd = CreateDatabaseCommand(commandText, m_currentConnection))
                {
                    if (parameters != null && parameters.Count > 0)
                    {
                        foreach (var p in parameters)
                        {
                            cmd.Parameters.Add(p);
                        }
                    }

                    cmd.ExecuteNonQuery();
                }
            }
        }

        internal object ExecuteScalar(string commandText, List<SqlParameter> parameters = null)
        {
            if (NewConectionIsRequired())
            {
                using (SqlConnection conn = CreateConnection(connectionString: m_connStr))
                {
                    if (conn == null)
                    {
                        throw new System.Exception("No se puede abrir la conexión a la base de datos [connstr::empty]", null);
                    }

                    using (SqlCommand cmd = CreateDatabaseCommand(commandText, conn))
                    {
                        if (parameters != null && parameters.Count > 0)
                        {
                            foreach (var p in parameters)
                            {
                                cmd.Parameters.Add(p);
                            }
                        }

                        return cmd.ExecuteScalar();
                    }
                }
            }
            else
            {
                using (SqlCommand cmd = CreateDatabaseCommand(commandText, m_currentConnection))
                {
                    if (parameters != null && parameters.Count > 0)
                    {
                        foreach (var p in parameters)
                        {
                            cmd.Parameters.Add(p);
                        }
                    }

                    return cmd.ExecuteScalar();
                }
            }
        }

        /// <summary>
        /// Executes a T-SQL script as a reader (read data only) or read-write (schema and content fix).
        /// </summary>
        /// <param name="scriptCode"></param>
        /// <param name="executeAsReader">True if script is read only, or False if script is Schema or Data</param>
        /// <returns></returns>
        internal OperationResult ExecuteScript(Script script, bool executeAsReader = false)
        {
            string connString = executeAsReader ? m_connStr : m_connStr.ToLower().Replace("multiple active result sets=true", string.Empty);

            using (SqlConnection connection = new SqlConnection(connString))
            {                
                Server server = new Server(new ServerConnection(connection));
                List<string> results = new List<string>();                
                if (executeAsReader)
                {
                    SqlDataReader dr = server.ConnectionContext.ExecuteReader(script.SqlCode);
                    using (dr)
                    {
                        do
                        {
                            while (dr.Read())
                            {
                                results.Add(dr.GetString(0));
                            }

                        } while (dr.NextResult());
                    }
                }
                else
                {
                    server.ConnectionContext.BeginTransaction();
                    int rslt = server.ConnectionContext.ExecuteNonQuery(script.SqlCode);
                    results.Add("ExecuteNonQuery int result: " + rslt);

                    string message = string.Format("Versioning Script '{0}' has been executed. See 'Details' column for script code.", script.FileName);
                    ScriptErrorLogger.AddMessage(message: message);
                    Debug.WriteLine(message);
                    server.ConnectionContext.CommitTransaction();
                }

                
                return new OperationOk() { Data = results };
            }
        }

        /// <summary>
        /// Checks if there's not an existing database with the name/app version or a new one can be created.
        /// </summary>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        internal bool CanCreateDatabase(string databaseName)
        {
            try
            {               
                if (DbMigrationToolConfig.ResourcesAssembly is null)
                {
                    throw new Exception("There's no assembly with Resources to read from.");
                }

                Version appVersion = DbMigrationToolConfig.ResourcesAssembly.GetName().Version;
                string appVersionPropertyName = "AppVersion";

                //string connStringServer = ConnectionStringUtils.ExtractForVersioning(this.m_connStr);
                using (SqlConnection connection = new SqlConnection(m_connStr))
                {
                    Server server = new Server(new ServerConnection(connection));
                    // ... check if the current 
                    Database db = server.Databases[databaseName];
                    if (db == null)
                    {
                        return true;
                    }

                    if (db.ExtendedProperties.Contains(appVersionPropertyName))
                    {
                        string dbAppVersion = db.ExtendedProperties[appVersionPropertyName].Value.ToString();
                        return !dbAppVersion.Equals(appVersion.ToString());
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        internal bool DatabaseExists(string databaseName)
        {
            string query = string.Format("SELECT 1 FROM MASTER.SYS.DATABASES WHERE NAME = N'{0}'", databaseName);
            object rslt = ExecuteScalar(query);
            int a = rslt != null ? (int)rslt : 0;
            return a == 1;
        }

        internal void CreateDatabase(string databaseName, string dataPackageScriptPath)
        {
            try
            {
                if (CanCreateDatabase(databaseName))
                {
                    _CreateDatabase(databaseName, dataPackageScriptPath);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void _CreateDatabase(string databaseName, string dataPackageScriptPath)
        {
            /*
             * DATABASE CREATION PROCESS DESCRIPTION 
             * 1. New Database creation: will be named databaseName_[AppVersion] so the logical files are unique and descriptive.
             * 1.2 The AppVersion is also stored as an ExtendedProperty of the database.
             * 2. Database Setup Process: schema and data scripts are applied to the new database.             
             * 3. DataPackageScript creation: if there's a database matching the name [databaseName] (the soon to be replaced "old" database), a DataPackageScript is generated with the new data that may have been created in the old database.
             *      For example, test case data that needs to be carried over, and more importantly, incorporated as part of the 'Database Setup Process' (2). database data is compared against the new database, and:
             * 3.1 The DataPackageScript will be applied to the new database (effectively carrying over data from the old database that is not present in the "Database Setup Process".
             * 3.2 The DataPackageScript will saved as an .sql script file so it can be retrieved at any moment. Data from the script can then be identified as Test Case Data and included into the source 
             *      code so the next "Database Setup Process" can include this data. This effectively creates an incremental data pool for new built databases.
             * 4. The "old" database will be renamed to databaseName_[AppVersionFromExtendedProperty]. This allows to keep history of databases and directly match a DB with a built AppVersion.
             *      Remember that as all databases are built from scratch using this very same process, logical files are already named after the AppVersion because of (1). After dabase renaming, both files and "user accesible name" will match.
             *      At this stage, no database is available with the naming [databaseName] , so there's no DB available for the app to use.
             * 4.1  Backup the old database: backup for databaseName_[AppVersionFromExtendedProperty] is executed
             * 4.2  Delete databases: all databases named databaseName_[SomeVersion] other than the recently replaced one are deleted. This allows to access the replaced DB without having to restore a backup. Backups of deleted databases are not deleted, 
             *      and ony databases with a valid backup file are deleted.
             * 5. Rename the new database: name of the recently created database is set to [databaseName] and is now ready to be accessed by the App.
             * END OF PROCESS
             */

            /************************************************************************/
            /* FIXME: ON TESTING ENVIRONMENT, IF A DATABASE ALREADY EXISTS, DROP IT */
            /************************************************************************/

            ScriptErrorLogger.AddMessage(message: string.Format("Starting database bootstrapping process for database '{0}'.", databaseName));
            Version appVersion = DbMigrationToolConfig.ResourcesAssembly.GetName().Version;
            string appVersionPropertyName = "AppVersion";
            string newDatabaseName = databaseName + "_" + appVersion;
            ScriptErrorLogger.AddMessage(message: string.Format("Creating database '{0}', final name will be '{1}'", newDatabaseName, databaseName));

            using (SqlConnection connection = new SqlConnection(m_connStr))
            {
                Server server = new Server(new ServerConnection(connection));

                // 1. New Database creation.

                Database newDb = new Database(server, newDatabaseName)
                {
                    // FIXME It's 2019, should use unicode.
                    Collation = "SQL_Latin1_General_CP1_CI_AS"
                };

                if (server.Databases.Contains(newDatabaseName))
                {
                    server.Databases[newDatabaseName].Drop();
                }

                newDb.Create();
                // 1.2 set the app version as a property on the new database, additional
                ExtendedProperty ep = new ExtendedProperty(newDb, appVersionPropertyName, appVersion.ToString());
                ep.Create();

                ScriptErrorLogger.AddMessage(message: string.Format("Database '{0}' created.", newDb.Name));

                string newDbConnString = server.ConnectionContext.ConnectionString + ";initial catalog=" + newDb.Name;
                // 2. Database Setup Process.
                SqlServerManager newDatabaseManager = new SqlServerManager(newDbConnString);
                DBStatusController dbsc = new DBStatusController(newDatabaseManager, newDb.Name);
                dbsc.LoadStatus();
                OperationResult rslt = dbsc.ExecuteAllPendingScripts();
                if (!rslt.Success)
                {
                    throw new Exception(rslt.Info);
                }

                string oldDatabaseName = string.Empty;
                if (server.Databases.Contains(databaseName))
                {
                    ScriptErrorLogger.AddMessage(message: string.Format("Old database found with name '{0}', proceeding with Db replacement...", databaseName));
                    /* DataPackageScript creation ommitted for performance reasons.
                    // 3. DataPackageScript creation.                
                    Script dataPackageScript = CreateDataPackageScript(sourceDatabaseName: databaseName, targetDatabaseName: newDatabaseName);

                    // 3.1 apply script to new database
                    //For now it will not be impacted automatically because it creates problems with the updates.
                    //newDatabaseManager.ExecuteScript(dataPackageScript);

                    // 3.2 save script to file.
                    if (!dataPackageScriptPath.EndsWith("\\"))
                    {
                        dataPackageScriptPath += "\\";
                    }

                    string dataPackageScriptFullPath = dataPackageScriptPath + dataPackageScript.FileName;

                    if (!Directory.Exists(dataPackageScriptPath))
                    {
                        Directory.CreateDirectory(dataPackageScriptPath);
                    }

                    File.WriteAllText(dataPackageScriptFullPath, dataPackageScript.SqlCode);

                    Logger.AddMessage(message: "DataPackageScript " + dataPackageScript.FileName + " saved to " + dataPackageScriptPath);
                    */

                    // 4. old database rename                    
                    Database oldDatabase = server.Databases[databaseName];
                    if (oldDatabase.ExtendedProperties.Contains(appVersionPropertyName))
                    {
                        string oldDbAppVersion = oldDatabase.ExtendedProperties[appVersionPropertyName].Value.ToString();
                        oldDatabaseName = oldDatabase.Name + "_" + oldDbAppVersion;
                        RenameDatabase(server, oldDatabase, oldDatabaseName);
                    }
                    else
                    {
                        string errMsg = string.Format("Could not find ExtendedProperty '{0}' for database '{1}'.", appVersionPropertyName, databaseName);
                        ScriptErrorLogger.AddMessage(message: errMsg, logType: LogTypeEnum.Error);
                        throw new Exception(errMsg);
                    }

                    // 4.1 old database backup
                    BackupDatabase(databaseName: oldDatabase.Name, server: server);
                }
                else
                {
                    ScriptErrorLogger.AddMessage(message: string.Format("No database to replace found with name '{0}'.", databaseName));
                }

                List<Database> databasesToDrop = new List<Database>();
                //4.2 delete databases, only if a valid backup exists.
                ScriptErrorLogger.AddMessage(message: "Deleting other databases (only if there's a valid backup file)...");
                foreach (Database db in server.Databases)
                {
                    bool deleteDatabase = db.Name.StartsWith(databaseName + "_") && !db.Name.Equals(oldDatabaseName) && !db.Name.Equals(newDb.Name);
                    if (deleteDatabase)
                    {
                        string backupFileName = server.BackupDirectory + "\\" + db.Name;
                        OperationResult backupVerifyOperation = VerifyDatabaseBackup(backupFileName: backupFileName, databaseName: db.Name, server: server);
                        if (backupVerifyOperation.Success)
                        {
                            databasesToDrop.Add(db);
                        }
                        else
                        {
                            string errMsg = "Error: " + backupVerifyOperation.Info;
                            //Logger.SaveLog(message: errMsg, logType: LogTypeEnum.Error);
                            Console.WriteLine(errMsg);
                        }
                    }
                }

                databasesToDrop.ForEach(db =>
                {
                    ScriptErrorLogger.AddMessage(message: string.Format("Dropping database '{0}'", db.Name));
                    db.Drop();
                });

                // 5. rename new database to it's operational name.
                RenameDatabase(server, newDb, databaseName);
                ScriptErrorLogger.AddMessage(message: string.Format("Database bootstraping process completed for database '{0}'", databaseName));
            }
        }

        private OperationResult RenameDatabase(Server server, Database database, string newDbName)
        {
            ScriptErrorLogger.AddMessage(message: string.Format("Renaming database '{0}' to '{1}'", database, newDbName));
            server.KillAllProcesses(database.Name);
            database.DatabaseOptions.UserAccess = DatabaseUserAccess.Single;
            database.Alter(TerminationClause.RollbackTransactionsImmediately);
            database.Rename(newDbName);
            database.Alter();
            database.DatabaseOptions.UserAccess = DatabaseUserAccess.Multiple;
            database.Alter(TerminationClause.FailOnOpenTransactions);
            ScriptErrorLogger.AddMessage(message: string.Format("Rename completed."));
            return new OperationOk();
        }

        public OperationResult BackupDatabase(string databaseName, Server server)
        {
            ScriptErrorLogger.AddMessage(message: string.Format("Backing up database '{0}' ", databaseName));
            Backup backup = new Backup();
            backup.Action = BackupActionType.Database;
            backup.Database = databaseName;
            string destinationPath = System.IO.Path.Combine(server.BackupDirectory, databaseName + ".bak");
            backup.Devices.Add(new BackupDeviceItem(destinationPath, DeviceType.File));
            backup.Initialize = true;
            backup.Checksum = true;
            backup.ContinueAfterError = true;
            backup.Incremental = false;
            backup.LogTruncation = BackupTruncateLogType.Truncate;
            backup.PercentComplete += new PercentCompleteEventHandler(backup_PercentComplete);
            backup.Complete += new Microsoft.SqlServer.Management.Common.ServerMessageEventHandler(backup_Complete);
            // Perform backup.
            backup.SqlBackup(server);
            ScriptErrorLogger.AddMessage(message: string.Format("Backup of database '{0}' completed.", databaseName));
            return new OperationOk();
        }

        public OperationResult VerifyDatabaseBackup(string backupFileName, string databaseName, Server server)
        {
            ScriptErrorLogger.AddMessage(message: string.Format("Verifying backup file '{0}' for database '{1}' ", backupFileName, databaseName));
            Restore restore = new Restore();
            restore.Devices.AddDevice(backupFileName + ".bak", DeviceType.File);
            restore.Database = databaseName;
            bool rslt = restore.SqlVerify(server);

            if (!rslt)
            {
                string msg = string.Format("Backup file '{0}' for database '{1}' could not be verified or it's corrupted.", backupFileName, databaseName);
                ScriptErrorLogger.AddMessage(message: msg, logType: LogTypeEnum.Error);
                return new OperationFailed(msg);
            }

            ScriptErrorLogger.AddMessage(message: "Backup file OK.");

            return new OperationOk();
        }

        public Script CreateDataPackageScript(string sourceDatabaseName, string targetDatabaseName)
        {
            ScriptErrorLogger.AddMessage(message: "Creating DataPackageScript...");
            StringBuilder dataPackageBuilder = new StringBuilder();
            List<string> ignoreTables = new List<string>() { "SYSTEM_VERSIONING", "SYSTEM_VERSIONING_LOG" };

            SqlConnection conn = new SqlConnection(m_connStr);
            try
            {
                Server server = new Server(new ServerConnection(conn));
                Database sourceDatabase = server.Databases[sourceDatabaseName];
                Database targetDatabase = server.Databases[targetDatabaseName];

                ScriptingOptions options = new ScriptingOptions();
                options.ScriptData = true;
                options.ScriptDrops = false;
                options.ScriptSchema = false;
                options.EnforceScriptingOptions = false;
                options.Indexes = false;
                options.IncludeHeaders = false;
                options.WithDependencies = false;

                TableCollection tables = sourceDatabase.Tables;

                var sourceRowsByTable = new Dictionary<string, string>();
                var targetRowsByTable = new Dictionary<string, string>();
                List<string> tablesDiff = new List<string>();
                foreach (Table table in tables)
                {
                    if (ignoreTables.Contains(table.Name))
                    {
                        continue;
                    }

                    tablesDiff.Add(table.Name);
                    StringBuilder sbSource = new StringBuilder();
                    StringBuilder sbTarget = new StringBuilder();
                    sourceRowsByTable.Add(table.Name, string.Empty);

                    foreach (string line in sourceDatabase.Tables[table.Name].EnumScript(options))
                    {
                        sbSource.Append(line + "\r\n");
                    }

                    sourceRowsByTable[table.Name] = sbSource.ToString();

                    if (targetDatabase.Tables.Contains(table.Name))
                    {
                        targetRowsByTable.Add(table.Name, string.Empty);

                        foreach (string line in targetDatabase.Tables[table.Name].EnumScript(options))
                        {
                            sbTarget.Append(line + "\r\n");
                        }

                        targetRowsByTable[table.Name] = sbTarget.ToString();
                    }
                }

                string sqlTemplate =
                    @"IF exists( select 1 from sys.columns c where c.object_id in (
                        select object_id from sys.objects o where type_desc = 'USER_TABLE'
                        and o.name = '{0}')
                        and c.is_identity = 1)
                        BEGIN
                            SET IDENTITY_INSERT {0} ON;
                            {1}
                            SET IDENTITY_INSERT {0} OFF;
                        END
                    ELSE
                        BEGIN
                            {1}
                        END
                    ";


                foreach (string table in tablesDiff)
                {
                    InlineDiffBuilder diffBuilder = new InlineDiffBuilder(new Differ());
                    var diff = diffBuilder.BuildDiffModel(targetRowsByTable[table], sourceRowsByTable[table]);

                    StringBuilder tableDiffs = new StringBuilder();
                    foreach (DiffPiece diffLine in diff.Lines)
                    {
                        if (diffLine.Type == ChangeType.Inserted || diffLine.Type == ChangeType.Modified)
                        {
                            tableDiffs.AppendLine(diffLine.Text);
                        }
                    }

                    if (tableDiffs.Length > 0)
                    {
                        string sqlDiffTable = string.Format(sqlTemplate, table, tableDiffs.ToString());
                        dataPackageBuilder.Append(sqlDiffTable);
                    }
                }

                string dataPackageString = dataPackageBuilder.ToString();
                string sourceDbAppVersion = sourceDatabase.ExtendedProperties["AppVersion"].Value.ToString();
                string scriptName = sourceDatabaseName + " (" + sourceDbAppVersion + ") to " + targetDatabaseName + "_DataPackage";
                string fileName = scriptName + ".sql";
                Script script = new Script()
                {
                    Id = -1,
                    SqlCode = dataPackageString,
                    CreationDate = DateTime.Now,
                    Name = scriptName,
                    FileName = fileName,
                    ExecutionRequiredForThisDatabase = true,
                    Version = 1
                };

                ScriptErrorLogger.AddMessage(message: "DataPackageScript created: " + script.FileName);

                return script;
            }
            catch (Exception e)
            {
                ScriptErrorLogger.AddMessage(message: "Error creating DataPackageScript: " + e.Message, logType: LogTypeEnum.Error);
                Debug.WriteLine("CreateDataPackageScript FAILED: " + e.Message);
                throw e;
            }
            finally
            {
                conn.Close();
            }
        }

        static void backup_Complete(object sender, Microsoft.SqlServer.Management.Common.ServerMessageEventArgs e)
        {
            Debug.WriteLine(e.ToString() + "% Complete");
        }
        static void backup_PercentComplete(object sender, PercentCompleteEventArgs e)
        {
            Debug.WriteLine(e.Percent.ToString() + "% Complete");
        }

        internal void GetDatabaseInfo(out DatabaseInfo databaseInfo, out DatabaseServerInfo serverInfo, bool generateScript = false)
        {
            databaseInfo = new DatabaseInfo();
            serverInfo = new DatabaseServerInfo();
            databaseInfo.Tables = new List<string>();
            serverInfo.Databases = new List<string>();

            //string connStringServer = ConnectionStringUtils.ExtractForVersioning(this.m_connStr);
            using (SqlConnection connection = new SqlConnection(m_connStr))
            {
                Server server = new Server(new ServerConnection(connection));
                SqlConnectionStringBuilder connStringBuilder = new SqlConnectionStringBuilder(m_connStr);
                List<string> results = new List<string>();
                Database thisDatabase = server.Databases[connStringBuilder.InitialCatalog];
                serverInfo.Name = server.Name;

                foreach (Database db in server.Databases)
                {
                    serverInfo.Databases.Add(db.Name);
                }

                serverInfo.MemoryUsageInKB = server.PhysicalMemoryUsageInKB;
                // serverInfo.ProcessorUsage = server.ProcessorUsage;
                serverInfo.Version = server.Version.ToString();

                databaseInfo.Name = thisDatabase.Name;
                databaseInfo.Collation = thisDatabase.Collation;
                databaseInfo.ActiveConnections = thisDatabase.ActiveConnections;
                databaseInfo.CreateDate = thisDatabase.CreateDate;
                databaseInfo.LastBackupDate = thisDatabase.LastBackupDate;
                databaseInfo.SizeMB = (int)thisDatabase.Size;

                TableCollection tables = thisDatabase.Tables;
                var schemaScript = thisDatabase.Schemas["dbo"].Script();

                string dbScript = string.Empty;

                foreach (Table table in tables)
                {
                    databaseInfo.Tables.Add(table.Name);
                    if (!generateScript)
                    {
                        continue;
                    }

                    var tableScripts = table.Script();
                    foreach (string script in tableScripts)
                    {
                        dbScript += script;
                    }

                    foreach (Microsoft.SqlServer.Management.Smo.Index index in table.Indexes)
                    {
                        var indexScripts = index.Script();
                        foreach (string script in indexScripts)
                        {
                            dbScript += script;
                        }
                    }

                    foreach (ForeignKey fk in table.ForeignKeys)
                    {
                        var fkScripts = fk.Script();
                        foreach (string script in fkScripts)
                        {
                            dbScript += script;
                        }
                    }
                }

                databaseInfo.SchemaScript = dbScript;
            }

            databaseInfo.VersioningHistory = new SystemVersioningBroker(this).GetAll();
        }

        internal SqlCommand CreateDatabaseCommand(string commandText, SqlConnection conn)
        {
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandTimeout = 30;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = commandText;
            return cmd;
        }

        private SqlCommand CreateDatabaseCommand(string commandText, SqlConnection conn, Dictionary<string, object> parameters, CommandType type)
        {
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandTimeout = 30;
            cmd.CommandType = type;
            cmd.CommandText = commandText;

            string paramPrefix = "@p";
            if (parameters != null)
            {
                foreach (KeyValuePair<string, object> kvp in parameters)
                {
                    string paramName = kvp.Key.ToLower();
                    object value = kvp.Value;

                    if (value == null || value.ToString() == "null")
                    {
                        value = DBNull.Value;
                    }

                    if (value is EntityBase)
                    {
                        value = ((EntityBase)value).Id;
                        paramName = (kvp.Value.GetType().Name).ToLower() + "Id";
                    }

                    SqlCommandHelper sqlCommandHelper = value as SqlCommandHelper;
                    SqlParameter parameter;
                    if (sqlCommandHelper != null)
                    {
                        parameter = new SqlParameter(paramPrefix + paramName, sqlCommandHelper.SqlDbType);
                        parameter.Value = sqlCommandHelper.Value;
                    }
                    else
                    {
                        parameter = new SqlParameter(paramPrefix + paramName, value);
                    }

                    bool paramInCmd = commandText.ToLower().Contains(parameter.ParameterName.ToLower());

                    if (paramInCmd)
                    {
                        cmd.Parameters.Add(parameter);
                    }
                }
            }

            return cmd;
        }

        internal object ExecuteNonQuery(SqlCommand cmd, Dictionary<string, object> parameters)
        {
            return cmd.CommandText.ToLower().EndsWith("select scope_identity();") ? cmd.ExecuteScalar() : cmd.ExecuteNonQuery();
        }

        internal OperationResult Save(EntityBase entity)
        {
            return SaveEntity(entity);
        }

        private OperationResult SaveEntity(EntityBase entityToSave)
        {
            Type parentEntityType = entityToSave.GetType();
            if (parentEntityType.GetCustomAttribute(typeof(PersistibleClass)) == null)
            {
                throw new System.Exception(parentEntityType.Name + " is not a PersistibleClass");
            }

            PersistibleClass persistibleClass = parentEntityType.GetCustomAttribute(typeof(PersistibleClass)) as PersistibleClass;

            bool isInsert = entityToSave.GetEntityStatus() == EntityStatus.InsertPending;
            List<PropertyInfo> persistibles = new List<PropertyInfo>();
            Dictionary<EntityBase, PropertyInfo> parentPropertyByChild = new Dictionary<EntityBase, PropertyInfo>();
            List<EntityBase> childs = new List<EntityBase>();
            foreach (var pp in parentEntityType.GetProperties())
            {
                PersistibleProperty _pp = pp.GetCustomAttribute(typeof(PersistibleProperty)) as PersistibleProperty;
                if (_pp != null)
                {
                    if (isInsert && _pp.IsIdentity)
                    {

                    }
                    else
                    {
                        persistibles.Add(pp);
                    }
                }

                if (pp.IsDefined(typeof(PersistibleList), true))
                {
                    System.Collections.IList _a = pp.GetValue(entityToSave, null) as System.Collections.IList;
                    List<EntityBase> childsToDelete = new List<EntityBase>();
                    foreach (var __a in _a)
                    {
                        EntityBase _e = __a as EntityBase;
                        if (_e.GetEntityStatus() == EntityStatus.DeletePending)
                        {
                            childsToDelete.Add(_e);
                            continue;
                        }

                        childs.Add(_e);
                    }


                    childsToDelete.ForEach(delegate (EntityBase __e) { childs.Insert(0, __e); });
                }
            }

            string updateColumns = "";
            string insertColumns1 = "(";
            string insertColumns2 = "(";
            int count = persistibles.Count;
            int current = 0;
            Dictionary<string, object> parameters = new Dictionary<string, object>();

            foreach (var p in persistibles)
            {
                PersistibleProperty pp = (PersistibleProperty)p.GetCustomAttribute(typeof(PersistibleProperty));

                string pName = pp.ColumnName != null ? pp.ColumnName : p.Name;
                object pValue = null;
                // take value from another entity.
                if (typeof(EntityBase).IsAssignableFrom(p.PropertyType))
                {
                    object oo = p.GetValue(entityToSave, null);
                    pValue = p.DeclaringType.GetProperty("Id").GetValue(oo, null);
                    if ((int)pValue == -1)
                    {
                        pValue = null;
                    }
                }
                else
                {
                    pValue = p.GetValue(entityToSave, null);
                }

                if (pValue == null)
                {
                    pValue = DBNull.Value;
                }

                parameters.Add(pName, pValue);
                string colName = pName;
                if (m_reservedWords.Contains(colName.ToLower()))
                {
                    colName = "[" + colName + "]";
                }

                if (!pp.IsIdentity)
                {
                    updateColumns += colName + " = @p" + pName + ", ";
                }

                insertColumns1 += colName;
                insertColumns2 += "@p" + pName;

                if (++current < count)
                {
                    insertColumns1 += ", ";
                    insertColumns2 += ", ";
                }
            }

            updateColumns = updateColumns.Substring(0, updateColumns.Length - 2);

            insertColumns1 += ") ";
            insertColumns2 += ") ";

            string typeName = parentEntityType.Name;
            string table = persistibleClass.TableName;
            string idColumn = persistibleClass.IdProperty;

            string updateQuery = "update {0} set {1} where {2} = " + entityToSave.Id;
            updateQuery = string.Format(updateQuery, table, updateColumns, idColumn);

            string insertQuery = "insert into {0} {1} OUTPUT INSERTED.{3} values {2};SELECT SCOPE_IDENTITY();";
            insertQuery = string.Format(insertQuery, table, insertColumns1, insertColumns2, idColumn);

            string deleteQuery = "delete from {0} where {1} = " + entityToSave.Id;
            deleteQuery = string.Format(deleteQuery, table, idColumn);

            string query = insertQuery;
            EntityStatus entityStatus = entityToSave.GetEntityStatus();
            switch (entityStatus)
            {
                case EntityStatus.InsertPending:
                    query = insertQuery;
                    break;
                case EntityStatus.DeletePending:
                    parameters.Clear();
                    query = deleteQuery;
                    break;
                default:
                    query = updateQuery;
                    break;
            }

            Debug.WriteLine("Executing:");
            Debug.WriteLine(query);
            string parString = string.Empty;
            foreach (var kvp in parameters)
            {
                parString += kvp.Key + ": " + kvp.Value + Environment.NewLine;
            }
            Debug.WriteLine("Parameters:" + Environment.NewLine + parString);
            Debug.WriteLine("#############");
            object rslt = ExecuteDatabaseCommand(query, ExecuteNonQuery, parameters, type: CommandType.Text);

            if (rslt != null && ((int)rslt) > 0)
            {
                if (entityToSave.Id == 0)
                {
                    int thisId = (int)rslt;
                    entityToSave.Id = thisId;
                }

                if (entityToSave.GetEntityStatus() != EntityStatus.DeletePending)
                {
                    foreach (var c in childs)
                    {
                        // check for the parent property which should have the Id of the recently saved entity.
                        Type childType = c.GetType();
                        foreach (var pp in childType.GetProperties())
                        {
                            PersistibleProperty _pp = pp.GetCustomAttribute(typeof(PersistibleProperty)) as PersistibleProperty;
                            if (_pp != null && _pp.IsForeignKey && _pp.ParentType.Equals(parentEntityType))
                            {
                                pp.SetValue(c, entityToSave.Id);
                            }
                        }
                        
                        SaveEntity(c);
                    }
                }

                return new OperationOk();
            }

            return new OperationFailed(info: "Save failed for type:" + typeName);
        }
    }
}
