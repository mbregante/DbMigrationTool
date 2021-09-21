using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DbMigrationTool
{
    public class Script
    {
        private string m_stringDesc = string.Empty;

        public Script()
        {
            Messages = new List<string>();
            Warnings = new List<string>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public int Version { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ImpactedDate { get; set; }
        public string FileName { get; set; }
        public bool IsDataType { get { return DbMigrationToolConfig.DataResourceRegex.IsMatch(FileName); } }
        public bool IsSchemaType { get { return DbMigrationToolConfig.SchemaResourceRegex.IsMatch(FileName); } }
        public bool IsIntegrityType { get { return DbMigrationToolConfig.IntegrityResourceRegex.IsMatch(FileName); } }
        public bool HasMessages { get { return Messages.Count > 0; } }
        public bool HasWarnings { get { return Warnings.Count > 0; } }
        public string SqlCode { get; set; }
        public List<string> Messages { get; set; }
        public List<string> Warnings { get; set; }        
        public bool ExecutionRequiredForThisDatabase { get; set; }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(m_stringDesc))
            {
                if (!string.IsNullOrEmpty(Name))
                {
                    m_stringDesc += Name;
                }
                else
                {
                    m_stringDesc += FileName;
                }

                if (CreationDate > DateTime.MinValue)
                {
                    m_stringDesc += " | " + CreationDate.ToShortDateString();
                }
            }

            return m_stringDesc;
        }
    }
}
