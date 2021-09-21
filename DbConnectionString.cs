using Microsoft.Data.SqlClient;

namespace DbMigrationTool
{
    public class DbConnectionString
    {
        private SqlConnectionStringBuilder m_builder;

        public DbConnectionString(string connString)
        {
            m_builder = new SqlConnectionStringBuilder(connString);
            Database = m_builder.InitialCatalog;
            Username = m_builder.UserID;
            Password = m_builder.Password;
            Server = m_builder.DataSource;
        }

        public override string ToString()
        {
            return m_builder.ToString();
        }

        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Server { get; set; }
    }
}
