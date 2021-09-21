using System;

namespace DbMigrationTool
{
    [PersistibleClass(tableName: "SYSTEM_VERSIONING", idProperty: "ScriptId")]
    public class SystemVersioning : EntityBase
    {
        [PersistibleProperty(isIdentity: true, columnName: "ScriptId")]
        public override int Id
        {
            get { return base.Id; }
            set { base.Id = value; }
        }

        [PersistibleProperty()]
        public string Name
        {
            get;
            set;
        }

        [PersistibleProperty()]
        public int Version
        {
            get;
            set;
        }

        [PersistibleProperty(columnName: "CreationDay")]
        public DateTime CreationDate
        {
            get;
            set;
        }

        [PersistibleProperty(columnName: "ImpactedDay")]
        public DateTime ImpactedDate
        {
            get;
            set;
        }
    }
}
