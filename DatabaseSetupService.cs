using System;

namespace DbMigrationTool
{
    public class DatabaseSetupService
    {
        private SqlServerManager m_manager = null;
        public DatabaseSetupService()
        {
            string databaseSetupConnString = ConnectionStringUtils.BuildForSetup();
            m_manager = new SqlServerManager(databaseSetupConnString);
        }

        internal DatabaseSetupService(SqlServerManager manager)
        {
            m_manager = manager;
        }

        public OperationResult CreateDataPackageScript(string sourceDatabaseName, string targetDatabaseName)
        {
            m_manager.CreateDataPackageScript(sourceDatabaseName, targetDatabaseName);
            return new OperationOk();
        }

        public OperationResult CreateDatabase(string databaseName, string dataPackageScriptPath)
        {
            m_manager.CreateDatabase(databaseName, dataPackageScriptPath);
            return new OperationOk();
        }

        public bool CanCreateDatabase(string databaseName)
        {
            return m_manager.CanCreateDatabase(databaseName);
        }

        public bool DatabaseExists(string databaseName)
        {
            return m_manager.DatabaseExists(databaseName);
        }

        /// <summary>
        /// Executes a T-SQL script as a reader (read data only) or read-write (schema and content fix).
        /// </summary>
        /// <param name="scriptCode"></param>
        /// <param name="executeAsReader">True if script is read only, or False if script is Schema or Data</param>
        /// <returns></returns>
        internal OperationResult ExecuteScript(Script script, bool executeAsReader = false)
        {
            return m_manager.ExecuteScript(script, executeAsReader);
        }
    }
}
