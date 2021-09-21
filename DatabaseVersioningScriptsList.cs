using System.Collections.Generic;

namespace DbMigrationTool
{
    public class DatabaseVersioningScriptsList
    {
        public DatabaseVersioningScriptsList()
        {
            Scripts = new List<DatabaseVersioningScript>();
        }

        public List<DatabaseVersioningScript> Scripts { get; set; }
    }
}
