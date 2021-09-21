using System.Collections.Generic;

namespace DbMigrationTool
{
    public class DatabaseVersioningStatus
    {
        public DatabaseVersioningStatus()
        {
            ScriptsLogs = new List<DatabaseVersioningScriptLog>();
            Scripts = new List<DatabaseVersioningScript>();
        }

        public bool DatabaseDoesNotExists { get; set; }
        public bool DatabaseNeedsUpdate { get; set; }
        public bool HasWarnings { get; set; }
        public bool StatusLoaded { get; set; }
        public List<DatabaseVersioningScriptLog> ScriptsLogs { get; set; }
        public List<DatabaseVersioningScript> Scripts { get; set; }
    }
}
