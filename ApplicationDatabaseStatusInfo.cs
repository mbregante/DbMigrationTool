namespace DbMigrationTool
{
    public class ApplicationDatabaseStatusInfo
    {
        static ApplicationDatabaseStatusInfo s_instance;

        static ApplicationDatabaseStatusInfo()
        {
        }

        public string ApplicationServer { get; set; }
        public DatabaseInfo DatabaseInfo { get; set; }
        public DatabaseServerInfo DatabaseServerInfo { get; set; }
        public DatabaseVersioningStatus DatabaseVersioningStatus { get; set; }

        public static bool GenerateDbScripts = false;

        public static bool ApplicationInfoReady
        {
            get;
            private set;
        }

        public static DbEnvironmentEnum Environment
        {
            get { return DbMigrationToolConfig.Environment; }
        }
        
        public static ApplicationDatabaseStatusInfo Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = new ApplicationDatabaseStatusInfo();
                }
                return s_instance;
            }
        }
    }
}
