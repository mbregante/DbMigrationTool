using Microsoft.Data.SqlClient;

namespace DbMigrationTool
{
    internal class SystemVersioningLogBroker : AbstractEntityBrokerBase<SystemVersioningLog>
    {
        internal SystemVersioningLogBroker(SqlServerManager sqlServerRepository = null)
            : base(sqlServerRepository: sqlServerRepository)
        {
        }

        internal override SystemVersioningLog LoadEntity(SqlDataReader dr)
        {
            SystemVersioningLog e = new SystemVersioningLog();
            e.Id = dr.GetInt32(dr.GetOrdinal("id"));
            e.Message = dr.GetString(dr.GetOrdinal("Message"));
            e.DetailedMessage = dr.GetString(dr.GetOrdinal("DetailedMessage"));
            e.Date = dr.GetDateTime(dr.GetOrdinal("Date"));
            e.LogType = dr.GetInt32(dr.GetOrdinal("LogType"));
            e.RelatedScriptId = dr.GetInt32(dr.GetOrdinal("RelatedScriptId"));

            return e;
        }

        internal bool SystemVersioningLogTableExists()
        {
            string query = "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'System_Versioning_Log'";
            object rslt = SqlServerRepository.ExecuteScalar(query);
            int a = rslt != null ? (int)rslt : 0;
            return a == 1;
        }
    }
}