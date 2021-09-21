using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DbMigrationTool
{
    public static class SqlCommandUtils
    {
        public static SqlCommandHelper GetSqlCommandHelper(Type type, object value)
        {
            SqlCommandHelper SqlCommandHelper = new SqlCommandHelper();

            if (type == typeof(byte[]))
            {
                SqlCommandHelper.SqlDbType = SqlDbType.Image;
                if (value == null || (value != null && (value as byte[]).Length == 0))
                {
                    SqlCommandHelper.Value = DBNull.Value;
                }
                else
                {
                    SqlCommandHelper.Value = value;
                }
            }
            else
            {
                if (value == null)
                {
                    SqlCommandHelper.Value = DBNull.Value;
                }
                else
                {
                    SqlCommandHelper.SqlDbType = (new SqlParameter("dummy", value)).SqlDbType;
                    SqlCommandHelper.Value = value;
                }
            }

            return SqlCommandHelper;
        }
    }

    public class SqlCommandHelper
    {
        public object Value;
        public SqlDbType SqlDbType;

        public SqlCommandHelper()
        {
        }
    }
}
