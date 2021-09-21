using System;

namespace DbMigrationTool
{
    [Serializable]
    public abstract class EntityBase : IComparable
    {
        private EntityStatus m_entityStatus = EntityStatus.Unknown;

        public virtual int Id { get; set; }

        public EntityStatus EntityStatus
        {
            get
            {
                return m_entityStatus;
            }
            set
            {
                m_entityStatus = value;
            }
        }

        public override bool Equals(object obj)
        {
            EntityBase other = obj as EntityBase;
            if (other == null)
            {
                return false;
            }

            if (this.Id == other.Id)
            {
                bool sameTypeAndId = other.GetType().Equals(this.GetType());
                return sameTypeAndId;
            }

            return false;
        }

        public void DeleteInstance()
        {
            m_entityStatus = EntityStatus.DeletePending;
        }

        public EntityStatus GetEntityStatus()
        {
            if (m_entityStatus != EntityStatus.Unknown)
            {
                return m_entityStatus;
            }

            if (Id == 0)
            {
                return EntityStatus.InsertPending;
            }

            if (m_entityStatus == EntityStatus.DeletePending)
            {
                return EntityStatus.DeletePending;
            }

            return EntityStatus.Unknown;
        }

        public override int GetHashCode()
        {
            return (int)Id;
        }

        public int CompareTo(object obj)
        {
            EntityBase other = obj as EntityBase;
            if (other == null)
            {
                return 1;
            }

            return this.Id.CompareTo(other.Id);
        }
    }

    public enum EntityStatus
    {
        Unknown,
        DeletePending,
        InsertPending,
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class PersistibleList : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class PersistibleProperty : Attribute
    {
        private string m_columnName;
        private bool m_isIdentity = false;
        private bool m_isForeignKey = false;
        private Type m_parentType;
        private string m_foreignKeyName = "";
        private bool m_isCompressed = false;

        public PersistibleProperty()
        {
        }

        public PersistibleProperty(string columnName, bool isIdentity, bool isCompressed = false)
        {
            m_columnName = columnName;
            m_isIdentity = isIdentity;
            m_isCompressed = isCompressed;
        }

        public PersistibleProperty(string columnName)
        {
            m_columnName = columnName;
        }

        public PersistibleProperty(bool isIdentity)
        {
            m_isIdentity = isIdentity;
        }

        public PersistibleProperty(Type parentType, string propName = "", bool isCompressed = false)
        {
            m_isForeignKey = true;
            m_parentType = parentType;
            m_foreignKeyName = propName;
            m_isCompressed = isCompressed;
        }

        public PersistibleProperty(bool isIdentity = true, bool isCompressed = false)
        {
            m_isIdentity = isIdentity;
            m_isCompressed = isCompressed;
            m_isCompressed = isCompressed;
        }



        public string ColumnName
        {
            get
            {
                return m_columnName;
            }
        }

        public bool IsIdentity
        {
            get
            {
                return m_isIdentity;
            }
        }

        public bool IsForeignKey
        {
            get
            {
                return m_isForeignKey;
            }
        }

        public Type ParentType
        {
            get
            {
                return m_parentType;
            }
        }

        public string ForeignKeyName
        {
            get
            {
                return m_foreignKeyName;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class PersistibleClass : Attribute
    {
        string m_tableName;
        string m_idProperty;
        public PersistibleClass()
        {
        }

        public PersistibleClass(string tableName, string idProperty)
        {
            m_tableName = tableName;
            m_idProperty = idProperty;
        }

        public string IdProperty
        {
            get
            {
                return m_idProperty;
            }
        }

        public string RowUpdateIdProperty
        {
            get
            {
                return "row_update_id";
            }
        }

        public string TableName
        {
            get
            {
                return m_tableName;
            }
        }
    }
}
