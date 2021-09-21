using System;
using System.Collections.Generic;

namespace DbMigrationTool
{
    public class DatabaseVersioningScript
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Version { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ImpactedDate { get; set; }
        public string FileName { get; set; }
        public string SqlCode { get; set; }
        public bool IsSchemaScript { get; set; }
        public bool IsDataScript { get; set; }
        public bool IsIntegrityScript { get; set; }
        public bool HasMessages { get; set; }
        public bool HasWarnings { get; set; }
        public List<string> Messages { get; set; }
        public List<string> Warnings { get; set; }
        public bool ExecutionRequiredForThisDatabase { get; set; }

        public override string ToString()
        {
            return string.Format("Id: {0} | Creation date: {1} | Name: {2}", Id, CreationDate, Name);
        }
    }
}
