using System;
using System.Collections.Generic;

namespace DbMigrationTool
{
    public class DatabaseVersioningService
    {
        private DBStatusController m_statusController;

        public DatabaseVersioningService()
        {
            m_statusController = new DBStatusController();            
        }

        public void LoadStatus()
        {
            if (!m_statusController.DatabaseDoesNotExists)
            {
                m_statusController.LoadStatus();
            }
        }

        public OperationResult CheckVersioningStatus()
        {
            LoadStatus();

            DatabaseVersioningStatus statusData = GetVersioningStatus();
            ApplicationDatabaseStatusInfo.Instance.DatabaseVersioningStatus = statusData;

            string message = string.Empty;
            if (statusData.DatabaseDoesNotExists)
            {
                message = "Database does not exists";
            }
            else if (statusData.DatabaseNeedsUpdate)
            {
                message = "Database needs to be updated.";
            }
            else
            {
                message = "Database is up to date.";
            }

            return new OperationOk(info: message) { Data = statusData };
        }

        public bool HasWarnings
        {
            get { return m_statusController.HasWarnings; }
        }

        public bool StatusLoaded
        {
            get { return m_statusController.StatusLoaded; }
        }

        public List<ScriptLog> GetScriptsLog()
        {
            return m_statusController.ScriptsLog;
        }

        public DatabaseVersioningStatus GetVersioningStatus()
        {
            return new DatabaseVersioningStatus()
            {
                DatabaseDoesNotExists = m_statusController.DatabaseDoesNotExists,
                DatabaseNeedsUpdate = m_statusController.DatabaseNeedsUpdate,
                HasWarnings = this.HasWarnings,
                StatusLoaded = this.StatusLoaded,
                ScriptsLogs = this.GetScriptsLogsDTOs(),
                Scripts = this.GetScriptsList().Scripts
            };
        }

        public OperationResult ExecuteSchemaUpdates()
        {
            OperationResult rslt = m_statusController.ExecuteSchemaUpdates();
            CheckVersioningStatus();
            return rslt;
        }
        
        public OperationResult ExecuteScript(DatabaseVersioningScriptExec scriptExecRequest)
        {
            OperationResult rslt = m_statusController.ExecuteScript(scriptExecRequest);
            if (!rslt.Success)
            {
                return rslt;
            }

            CheckVersioningStatus();
            return rslt;
        }

        private List<DatabaseVersioningScriptLog> GetScriptsLogsDTOs()
        {
            List<DatabaseVersioningScriptLog> dtos = new List<DatabaseVersioningScriptLog>();
            foreach (ScriptLog log in this.GetScriptsLog())
            {
                DatabaseVersioningScriptLog dto = new DatabaseVersioningScriptLog()
                {
                    Message = log.Message,
                    Script = GetScriptDTO(log.Script),
                    WarningScript = log.WarningScript
                };

                dtos.Add(dto);
            }

            return dtos;
        }

        private DatabaseVersioningScriptsList GetScriptsList()
        {
            DatabaseVersioningScriptsList dto = new DatabaseVersioningScriptsList();
            foreach (Script script in m_statusController.GetScripts())
            {
                dto.Scripts.Add(GetScriptDTO(script));
            }

            return dto;
        }

        private DatabaseVersioningScript GetScriptDTO(Script script)
        {
            DatabaseVersioningScript dto = new DatabaseVersioningScript()
            {
                Id = script.Id,
                Name = script.Name,
                CreationDate = script.CreationDate,
                ImpactedDate = script.ImpactedDate,
                IsSchemaScript = script.IsSchemaType,
                IsDataScript = script.IsDataType,
                IsIntegrityScript = script.IsIntegrityType,
                Version = script.Version,
                FileName = script.FileName,
                SqlCode = script.SqlCode,
                ExecutionRequiredForThisDatabase = script.ExecutionRequiredForThisDatabase,
                HasMessages = script.HasMessages,
                HasWarnings = script.HasWarnings,
                Messages = script.Messages,
                Warnings = script.Warnings
            };

            return dto;
        }
    }
}
