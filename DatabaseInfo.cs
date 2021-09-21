using System;
using System.Collections.Generic;

namespace DbMigrationTool
{
    public class DatabaseInfo
    {
        public string Name { get; set; }
        public string SchemaScript { get; set; }
        public int ActiveConnections { get; set; }
        public string Collation { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime LastBackupDate { get; set; }
        public int SizeMB { get; set; }
        public List<string> Tables { get; set; }
        public List<SystemVersioning> VersioningHistory { get; set; }
    }
}
