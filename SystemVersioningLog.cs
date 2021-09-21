using System;

namespace DbMigrationTool
{
    [PersistibleClass(tableName: "SYSTEM_VERSIONING_LOG", idProperty: "Id")]
    public class SystemVersioningLog : EntityBase
    {
        [PersistibleProperty(isIdentity: true, columnName: "Id")]
        public override int Id
        {
            get { return base.Id; }
            set { base.Id = value; }
        }

        [PersistibleProperty()]
        public string Message
        {
            get;
            set;
        }

        [PersistibleProperty()]
        public string DetailedMessage
        {
            get;
            set;
        }

        [PersistibleProperty()]
        public int? RelatedScriptId
        {
            get;
            set;
        }

        [PersistibleProperty()]
        public DateTime Date
        {
            get;
            set;
        }

        [PersistibleProperty()]
        public int LogType
        {
            get;
            set;
        }
    }
}
