using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Transactions;

namespace DbMigrationTool
{
    internal abstract class AbstractEntityBrokerBase<TEntity>
        where TEntity : EntityBase, new()
    {
        private const string CUSTOM_MAPPING_KEY__NAME = "name";
        protected const string IN_CLAUSE_VALUES_PLACEHOLDER = ":values:";

        private SqlServerManager m_sqlServerRepository;
        private string m_brokerConnectionString;
        private bool m_loadCompleteEntity = true;
        private string m_selectQuery = string.Empty;
        private string m_selectIDsQuery = string.Empty;

        protected string BrokerConnectionString
        {
            get { return m_brokerConnectionString; }
            set { m_brokerConnectionString = value; }
        }

        protected AbstractEntityBrokerBase(SqlServerManager sqlServerRepository = null)
        {
            if (sqlServerRepository != null)
            {
                SqlServerRepository = sqlServerRepository;
            }
            else
            {
                SqlServerRepository = new SqlServerManager(DbMigrationToolConfig.ConnectionString.ToString());
            }

            BrokerInstanceName = DateTime.Now.Ticks.ToString();
        }

        protected virtual bool SupportsEagerLoading()
        {
            return false;
        }

        protected bool LoadCompleteEntity
        {
            get { return m_loadCompleteEntity; }
            set { m_loadCompleteEntity = value; }
        }

        internal SqlServerManager SqlServerRepository
        {
            get
            {
                if (m_sqlServerRepository == null)
                {
                    if (!string.IsNullOrEmpty(BrokerConnectionString))
                    {
                        m_sqlServerRepository = new SqlServerManager(BrokerConnectionString);
                    }
                }

                return m_sqlServerRepository;
            }

            set
            {
                m_sqlServerRepository = value;
            }
        }

        internal string BrokerInstanceName
        {
            get;
            private set;
        }

        internal virtual TEntity GetById(int id)
        {
            var entityType = GetEntityType();

            if (entityType == null)
            {
                return null;
            }

            string idProperty = entityType.IdProperty;

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add(idProperty, id);

            TEntity entity = GetEntity(GetSelectQuery(new List<string>() { idProperty }), new SqlReaderHandler(ReadAndLoadEntity), parameters: parameters);

            return entity;
        }

        /// <summary>
        /// Returns the alias used for the entity's table when performing sql joins.
        /// </summary>
        /// <returns></returns>
        protected virtual string GetEntityTableQueryAlias()
        {
            return string.Empty;
        }

        /// <summary>
        /// Will set ANSI_NULL OFF (it set true) or ON for the queries.
        /// With ANSI OFF you can use = to look for nulls.
        /// For example: With ANSI_NULL OFF name = null will return every row with name in null, but with ANSI_NULL ON will return nothing.
        /// </summary>
        /// <returns></returns>
        protected virtual bool UseANSI_NULLS_OFF()
        {
            return false; //means it's using the default, that is ON
        }

        internal virtual List<TEntity> GetAll()
        {
            return GetEntities(GetSelectQuery());
        }

        internal virtual List<int> GetAllIDs()
        {
            return GetFromQuery<List<int>>(GetSelectIDsQuery(), new SqlReaderHandler(ReadIDsAsInt));
        }

        internal virtual List<long> GetAllIDsAsLong()
        {
            return GetFromQuery<List<long>>(GetSelectIDsQuery(), new SqlReaderHandler(ReadIDsAsLong));
        }

        /// <summary>
        /// Returns the entities with the given IDs.
        /// <para>Performs automatic handling of large result sets or large number of items in the 'ids' collection.</para>
        /// </summary>
        /// <param name="ids">Entities Id to search</param>
        /// <returns></returns>
        internal List<TEntity> GetByIds(List<int> ids)
        {
            List<TEntity> entities = new List<TEntity>();

            var searchItemsByStep = GetSearchItemsListByStep<int>(ids);

            foreach (var kvp in searchItemsByStep)
            {
                entities.AddRange(_GetByValues(kvp.Value));
            }

            return entities;
        }

        /// <summary>
        /// Returns the entities by a sql IN(...) clause, searching the column specified by the param 'inColumn' matching the values from the param 'values'.
        /// <para>Performs automatic handling of large result sets or large number of items in the 'values' collection.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inColumn">column name for the IN(...) clause</param>
        /// <param name="values"></param>
        /// <param name="otherWhereFilters">other where clauses like date > getdate(), etc</param>
        /// <returns></returns>
        protected List<TEntity> GetByColumnValues<T>(string inColumn, List<T> values, string otherWhereFilters = "")
        {
            List<TEntity> entities = new List<TEntity>();

            var searchItemsByStep = GetSearchItemsListByStep<T>(values);

            foreach (var kvp in searchItemsByStep)
            {
                var partial = _GetByValues(values: kvp.Value, inColumn: inColumn, otherWhereFilters: otherWhereFilters);
                entities.AddRange(partial);
            }

            return entities;
        }

        /// <summary>
        /// Get entities using a custom SQL query with the IN(...) clause, and the values sent for the IN list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query">Complete SQL query with the IN(...) clause.</param>
        /// <param name="values"></param>
        /// <returns></returns>
        protected List<TEntity> GetByColumnValues<T>(string query, List<T> values = null)
        {
            if (!query.Contains(IN_CLAUSE_VALUES_PLACEHOLDER))
            {
                throw new System.Exception("Query must contain the placeholder '" + IN_CLAUSE_VALUES_PLACEHOLDER + "' for IN (...) values replacement.");
            }

            string _query = query.Replace(IN_CLAUSE_VALUES_PLACEHOLDER, GetSearchItemsList<T>(values));
            var entities = GetEntities(_query);

            if (entities != null)
            {
                return entities;
            }

            return new List<TEntity>();
        }

        private List<TEntity> _GetByValues<T>(List<T> values, string inColumn = "", string otherWhereFilters = "")
        {
            var entityType = GetEntityType();
            if (entityType == null)
            {
                return null;
            }

            string _inColumn = entityType.IdProperty;
            if (!string.IsNullOrEmpty(inColumn))
            {
                _inColumn = inColumn;
            }

            if (!string.IsNullOrEmpty(GetEntityTableQueryAlias()))
            {
                _inColumn = GetEntityTableQueryAlias() + "." + _inColumn;
            }

            string inClauseValues = GetSearchItemsList<T>(values);
            string query = GetSelectQuery() + " WHERE " + _inColumn + " IN (" + inClauseValues + ") " + otherWhereFilters;
            var entities = GetEntities(query);

            if (entities != null)
            {
                return entities;
            }

            return new List<TEntity>();
        }

        /// <summary>
        /// Returns a list of parameters to inyect to a IN(...) clause in a query. 
        /// <para>Key: step number, Values: list of values</para>
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        protected Dictionary<int, List<T>> GetSearchItemsListByStep<T>(List<T> list, int stepMaxItems = 400)
        {
            Dictionary<int, List<T>> searchItemsByStep = new Dictionary<int, List<T>>();

            if (list == null)
            {
                return searchItemsByStep;
            }

            int maxItems = stepMaxItems;
            int steps = (list.Count / maxItems);
            int stepStart = 0;

            for (int i = 0; i <= steps; i++)
            {
                stepStart = maxItems * i;
                int stepEnd = ((i == steps) ? list.Count - stepStart : maxItems);
                List<T> _list = list.GetRange(stepStart, stepEnd) as List<T>;

                if (_list.Count == 0)
                {
                    break;
                }

                if (typeof(T).IsAssignableFrom(typeof(String)))
                {
                    List<T> stringValues = new List<T>();
                    foreach (T value in _list.Distinct())
                    {
                        object _value = "'" + value + "'";
                        stringValues.Add((T)_value);
                    }

                    searchItemsByStep.Add(i, stringValues);
                }
                else
                {
                    searchItemsByStep.Add(i, _list.Distinct().ToList());
                }
            }

            return searchItemsByStep;
        }

        protected T GetFromQuery<T>(string commandText, SqlReaderHandler readerHandler, Dictionary<string, object> parameters = null, bool applySecurity = true) where T : new()
        {
            if (UseANSI_NULLS_OFF())
            {
                //If using ansi null off, I will set it at the beggining of the command, and put it back at the end.
                commandText = " SET ANSI_NULLS OFF " + commandText + " SET ANSI_NULLS ON ";
            }
            return SqlServerRepository.ExecuteDatabaseCommand<T>(commandText, readerHandler, parameters);
        }

        protected TEntity GetEntity(string commandText, SqlReaderHandler readerHandler = null, Dictionary<string, object> parameters = null, bool applySecurity = true)
        {
            if (readerHandler == null)
            {
                readerHandler = LoadEntityAttributes;
            }

            if (UseANSI_NULLS_OFF())
            {
                //If using ansi null off, I will set it at the beggining of the command, and put it back at the end.
                commandText = " SET ANSI_NULLS OFF " + commandText + " SET ANSI_NULLS ON ";
            }

            return SqlServerRepository.ExecuteDatabaseCommand<TEntity>(commandText, readerHandler, parameters: parameters);
        }

        protected List<TEntity> GetEntities(string commandText, SqlReaderHandler readerHandler = null, Dictionary<string, object> parameters = null, bool applySecurity = true)
        {
            if (readerHandler == null)
            {
                readerHandler = LoadEntities;
            }

            if (UseANSI_NULLS_OFF())
            {
                //If using ansi null off, I will set it at the beggining of the command, and put it back at the end.
                commandText = " SET ANSI_NULLS OFF " + commandText + " SET ANSI_NULLS ON ";
            }

            return SqlServerRepository.ExecuteDatabaseCommand<List<TEntity>>(commandText, readerHandler, parameters: parameters);
        }

        /// <summary>
        /// Reads an entity from the database, moving the DataReader to the next record (executes dataReader.Read() ).
        /// </summary>
        /// <param name="dataReader"></param>
        /// <returns></returns>
        protected TEntity ReadAndLoadEntity(SqlDataReader dataReader)
        {
            if (!dataReader.Read())
            {
                return null;
            }

            TEntity entity = LoadEntityAttributes(dataReader);

            OnLoadEntityCompleted(entity);
            return entity;
        }

        /// <summary>
        /// Reads an entity from the database, using the DataReader's current position (does not execute dataReader.Read() ).
        /// </summary>
        /// <param name="dataReader"></param>
        /// <returns></returns>
        internal abstract TEntity LoadEntity(SqlDataReader dataReader);

        /// <summary>
        /// Reads and loads the entity from the database and then set it's attributes.
        /// </summary>
        /// <param name="dataReader"></param>
        /// <returns></returns>
        internal virtual TEntity LoadEntityAttributes(SqlDataReader dataReader)
        {
            TEntity entity = LoadEntity(dataReader);
            return entity;
        }

        private List<long> ReadIDsAsLong(SqlDataReader dataReader)
        {
            List<long> ids = new List<long>();

            while (dataReader.Read())
            {
                ids.Add(dataReader.GetInt64(0));
            }

            return ids;
        }

        protected List<int> ReadIDsAsInt(SqlDataReader dataReader)
        {
            List<int> ids = new List<int>();

            while (dataReader.Read())
            {
                ids.Add(dataReader.GetInt32(0));
            }

            return ids;
        }

        internal virtual List<TEntity> LoadEntities(SqlDataReader dataReader)
        {
            var entityType = GetEntityType();
            List<TEntity> entities = new List<TEntity>(100);
            TEntity entity = null;

            do
            {
                bool hasData = dataReader.Read();
                if (!hasData)
                {
                    break;
                }

                int objectId = dataReader.GetInt32(dataReader.GetOrdinal(entityType.IdProperty));
                // int rowUpdateId = dataReader.GetInt32(dataReader.GetOrdinal(entityType.RowUpdateIdProperty));
                entity = LoadEntityAttributes(dataReader);
                if (entity != null)
                {
                    entities.Add(entity);
                }

            } while (entity != null);

            return entities;
        }

        protected virtual void OnLoadEntitiesCompleted(List<TEntity> entities)
        {
            // override to feed custom caches etc.
        }

        /// <summary>
        /// Called when a single entity is loaded from the DB (not part of LoadEntities).
        /// </summary>
        /// <param name="entity"></param>
        protected virtual void OnLoadEntityCompleted(TEntity entity)
        {
            // override to feed custom caches etc.
        }

        internal virtual OperationResult Save(EntityBase entity)
        {
            using (TransactionScope scope = GetTransactionScope())
            {
                CompleteEntityBeforeInsert(entity);

                if (IfCheckDuplicityEntities())
                {
                    CheckDuplicityEntities(entity);
                }

                return PerformSaveOperation(scope, entity);
            }
        }

        internal virtual OperationResult Delete(EntityBase entity)
        {
            CheckUsage(entity);

            using (TransactionScope scope = GetTransactionScope())
            {
                try
                {
                    Type entityType = entity.GetType();

                    if (entityType.GetCustomAttribute(typeof(PersistibleClass)) == null)
                    {
                        throw new System.Exception(entityType.Name + " is not a PersistibleClass");
                    }

                    // children objects graph persistence is not implemented in this stripper down repo version.
                    entity.DeleteInstance();
                    return PerformSaveOperation(scope, entity);
                }
                catch (System.Exception ex)
                {
                    OperationResult opFailed = new OperationFailed("Error deleting", detailedInfo: ex.Message);
                    return opFailed;
                }
            }
        }

        internal virtual OperationResult Update(EntityBase entity, EntityBase oldEntity)
        {
            CheckUsage(entity);

            if (IfCheckDuplicityEntities())
            {
                CheckDuplicityEntities(entity);
            }

            using (TransactionScope scope = GetTransactionScope())
            {
                Type entityType = entity.GetType();

                if (entityType.GetCustomAttribute(typeof(PersistibleClass)) == null)
                {
                    throw new System.Exception(entityType.Name + " is not a PersistibleClass");
                }

                return PerformSaveOperation(scope, entity);
            }
        }

        private OperationResult PerformSaveOperation(TransactionScope scope, EntityBase entity)
        {
            OperationResult rslt = SqlServerRepository.Save(entity);
            if (!rslt.Success)
            {
                string callerMethod = new StackFrame(1).GetMethod().Name;
                string details = string.Format("Save operation '{0}' for entity type '{1}' id '{2}' was executed but affected 0 records!", callerMethod, entity.GetType().Name, entity.Id);
                Debug.WriteLine(rslt.Info + " - Details: " + details);
            }

            scope.Complete();

            return rslt;
        }

        #region SQL Server Methods

        internal object ExecuteDatabaseCommand(string commandText, DatabaseCommandHandler handler, Dictionary<string, object> parameters = null, string connectionString = "", CommandType type = CommandType.StoredProcedure)
        {
            return SqlServerRepository.ExecuteDatabaseCommand(commandText, handler, parameters: parameters, connectionString: connectionString, type: type);
        }

        internal void ExecuteQuery(string commandText, List<SqlParameter> parameters = null)
        {
            SqlServerRepository.ExecuteCommand(commandText: commandText, parameters: parameters);
        }

        internal object ExecuteNonQuery(SqlCommand cmd, Dictionary<string, object> parameters)
        {
            return SqlServerRepository.ExecuteNonQuery(cmd, parameters);
        }

        internal OperationResult ExecuteScript(Script script, bool executeWithReader)
        {
            return SqlServerRepository.ExecuteScript(script, executeAsReader: executeWithReader);
        }

        #endregion

        #region Check Usage
        internal virtual void CheckUsage(EntityBase entity)
        {
        }

        protected virtual int? GetParentEntityTypeId()
        {
            return null;
        }

        protected virtual int? GetParentId(EntityBase entity)
        {
            return null;
        }
        #endregion

        #region CompleteEntityBeforeInsert
        /// <summary>
        /// Build the required data structure before saving.
        /// </summary>
        /// <param name="entity"></param>
        protected virtual void CompleteEntityBeforeInsert(EntityBase entity)
        {

        }
        #endregion

        #region CheckDuplicity
        protected virtual bool IfCheckDuplicityEntities()
        {
            return false;
        }
        protected virtual void CheckDuplicityEntities(EntityBase entity)
        {

        }
        #endregion

        #region Utils
        protected string GetSearchItemsList<T>(List<T> values)
        {
            if (values == null || values.Count == 0)
            {
                return string.Empty;
            }

            if (typeof(T).IsAssignableFrom(typeof(string)))
            {
                List<string> stringValues = new List<string>();
                foreach (T value in values.Distinct())
                {
                    stringValues.Add(value.ToString());
                }

                return GetSearchStringItemsList(stringValues);
            }

            StringBuilder searchIdsBuilder = new StringBuilder();
            foreach (T value in values.Distinct())
            {
                searchIdsBuilder.Append(value.ToString() + ",");
            }

            string searchIds = searchIdsBuilder.ToString();
            searchIds = searchIds.Remove(searchIds.Length - 1, 1);
            return searchIds;
        }

        /// <summary>
        /// Concatena los string con comas
        /// </summary>
        /// <param name="stringList"></param>
        /// <param name="putSingleQuote">si es verdadero agrega la comilla simple en cada elemento</param>
        /// <returns></returns>
        protected string GetSearchStringItemsList(List<string> stringList, bool putSingleQuote = false)
        {
            if (stringList == null || stringList.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder searchIdsBuilder = new StringBuilder();
            foreach (string id in stringList)
            {
                StringBuilder str = new StringBuilder(id);
                if (putSingleQuote)
                {
                    str.Insert(0, "'");
                    str.Append("'");
                }
                str.Append(",");
                searchIdsBuilder.Append(str);
            }

            string searchIds = searchIdsBuilder.ToString();
            searchIds = searchIds.Remove(searchIds.Length - 1, 1);
            return searchIds;
        }

        protected virtual string GetSelectIDsQuery()
        {
            if (string.IsNullOrEmpty(m_selectIDsQuery))
            {
                var entityType = GetEntityType();

                if (entityType == null)
                {
                    return null;
                }

                string idProperty = entityType.IdProperty;
                m_selectIDsQuery = "SELECT [" + idProperty + "] FROM [dbo].[" + GetEntityType().TableName + "]";
            }

            return m_selectIDsQuery;
        }

        /// <summary>
        /// Retursn the select query for current EntityBase with attributes constraints
        /// </summary>
        /// <param name="whereFilterColumns"></param>
        /// <returns></returns>
        protected virtual string GetSelectIDsQuery(List<string> whereFilterColumns)
        {
            return GetWhereQuery(whereFilterColumns, GetSelectIDsQuery());
        }

        private string GetWhereQuery(List<string> whereFilterColumns, string selectQuery)
        {
            string finalQuery = selectQuery + " WHERE ";
            string alias = GetEntityTableQueryAlias();
            int count = whereFilterColumns.Count;
            int current = 0;

            foreach (string attr in whereFilterColumns)
            {
                if (!string.IsNullOrEmpty(alias))
                {
                    finalQuery += alias + "." + attr + " = @p" + attr;
                }
                else
                {
                    finalQuery += attr + " = @p" + attr;
                }

                if (++current < count)
                {
                    finalQuery += " AND ";
                }
            }

            return finalQuery;
        }

        /// <summary>
        /// Returns the select query for current EntityBase
        /// </summary>
        /// <returns></returns>
        protected virtual string GetSelectQuery()
        {
            if (string.IsNullOrEmpty(m_selectQuery))
            {
                if (!string.IsNullOrEmpty(GetEntityTableQueryAlias()))
                {
                    m_selectQuery = string.Format("SELECT {0}.* FROM [dbo].[" + GetEntityType().TableName + "] {0}", GetEntityTableQueryAlias());
                }
                else
                {
                    m_selectQuery = "SELECT * FROM [dbo].[" + GetEntityType().TableName + "]";
                }
            }

            return m_selectQuery;
        }

        /// <summary>
        /// Retursn the select query for current EntityBase with attributes constraints
        /// </summary>
        /// <param name="whereFilterColumns"></param>
        /// <returns></returns>
        protected virtual string GetSelectQuery(List<string> whereFilterColumns)
        {
            return GetWhereQuery(whereFilterColumns, GetSelectQuery());
        }

        internal PersistibleClass GetEntityType()
        {
            return typeof(TEntity).GetCustomAttribute(typeof(PersistibleClass)) as PersistibleClass;
        }

        protected object LoadCount(SqlDataReader dataReader)
        {
            if (!dataReader.Read())
            {
                return null;
            }

            return dataReader.GetInt32(0);
        }

        protected string GetSqlDate(DateTime date)
        {
            // 2014-01-20 06:00
            // 2014-03-03 05:59
            // 2008-06-15 21:15:07Z
            return date.ToString("u").Remove(16, 4);
        }

        protected TransactionScope GetTransactionScope()
        {
            return new TransactionScope(TransactionScopeOption.Required);
        }
        #endregion
    }
}
