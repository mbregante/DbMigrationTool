using Microsoft.Data.SqlClient;

namespace DbMigrationTool
{
    internal class SystemVersioningBroker : AbstractEntityBrokerBase<SystemVersioning>
    {
        internal SystemVersioningBroker(SqlServerManager sqlServerRepository = null)
            : base(sqlServerRepository: sqlServerRepository)
        {
        }

        internal override SystemVersioning LoadEntity(SqlDataReader dataReader)
        {
            SystemVersioning e = new SystemVersioning();
            e.Id = dataReader.GetInt32(0);
            e.Name = dataReader.GetString(1);
            e.Version = dataReader.GetInt32(2);
            e.CreationDate = dataReader.GetDateTime(3);
            e.ImpactedDate = dataReader.GetDateTime(4);

            return e;
        }

        internal bool SystemVersioningTableExists()
        {
            string query = "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'System_Versioning'";
            object rslt = SqlServerRepository.ExecuteScalar(query);
            int a = rslt != null ? (int)rslt : 0;
            return a == 1;
        }

        internal void GetDatabaseInfo(out DatabaseInfo databaseInfo, out DatabaseServerInfo serverInfo, bool generateScript = false)
        {
            SqlServerRepository.GetDatabaseInfo(out databaseInfo, out serverInfo, generateScript);
        }
    }
}
