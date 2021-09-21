using System.Collections.Generic;

namespace DbMigrationTool
{
    public class DatabaseServerInfo
    {
        public string Name { get; set; }
        public List<string> Databases { get; set; }
        public long MemoryUsageInKB { get; set; }
        public int ProcessorUsage { get; set; }
        public string Version { get; set; }
    }
}
