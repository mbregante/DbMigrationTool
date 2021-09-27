using System.Collections.Generic;
using System.Linq;

namespace DbMigrationTool
{
    public static class Logger
    {
        private static List<ScriptLog> s_scriptsLog;
        private static SystemVersioningLogBroker s_broker;
        private static List<SystemVersioningLog> s_logs;

        static Logger()
        {
            s_scriptsLog = new List<ScriptLog>();
            s_broker = new SystemVersioningLogBroker();
            s_logs = new List<SystemVersioningLog>();
        }

        internal static void Log(Script script, LogScriptMessageEnum LogType)
        {
            ScriptLog scriptLog = new ScriptLog();
            scriptLog.Script = script;

            switch (LogType)
            {
                case LogScriptMessageEnum.ScriptToRun:
                    scriptLog.Message = "The Script has to be run.";
                    break;
                case LogScriptMessageEnum.InsertStatementMissing:
                    scriptLog.Message = "The insert statement is missing.";
                    break;
                case LogScriptMessageEnum.StartSymbolMissing:
                    scriptLog.Message = "The script must start with a /* symbol.";
                    break;
                case LogScriptMessageEnum.EndSymbolMissing:
                    scriptLog.Message = "The script must end with a */ symbol.";
                    break;
                case LogScriptMessageEnum.IdMissing:
                    scriptLog.Message = "The Id parameter is missing.";
                    break;
                case LogScriptMessageEnum.VersionMissing:
                    scriptLog.Message = "The Version parameter is missing.";
                    break;
                case LogScriptMessageEnum.NameMissing:
                    scriptLog.Message = "The Name parameter is missing.";
                    break;
                case LogScriptMessageEnum.DateMissing:
                    scriptLog.Message = "The Date parameter is missing.";
                    break;
                case LogScriptMessageEnum.IdValueInvalid:
                    scriptLog.Message = "The Id value must be a number";
                    break;
                case LogScriptMessageEnum.NameValueInvalid:
                    scriptLog.Message = "The Name must be longer than 5 characters.";
                    break;
                case LogScriptMessageEnum.VersionValueInvalid:
                    scriptLog.Message = "The Version value must be a number.";
                    break;
                case LogScriptMessageEnum.DateValueInvalid:
                    scriptLog.Message = "The Date must have the following format MM/DD/AAAA";
                    break;
                case LogScriptMessageEnum.MissingDataHeader:
                    scriptLog.Message = "It's missing the header data of the script.";
                    break;
                case LogScriptMessageEnum.DBDeclaration:
                    scriptLog.Message = "The script should not specify which database is used.";
                    break;
                case LogScriptMessageEnum.ConnectionStrKeyInvalid:
                    scriptLog.Message = "The connection string key value is invalid.";
                    break;
                default:
                    break;
            }

            s_scriptsLog.Add(scriptLog);

            SystemVersioningLog svl = new SystemVersioningLog()
            {
                Message = scriptLog.Message,
                DetailedMessage = string.Empty,
                LogType = (int)LogTypeEnum.Info,
                RelatedScriptId = script.Id,
                Date = System.DateTime.Now
            };

            s_broker.Save(svl);
        }

        internal static void AddMessage(string message, string detailedMessage = "", LogTypeEnum logType = LogTypeEnum.Info, int? relatedScriptId = null)
        {
            SystemVersioningLog svl = new SystemVersioningLog()
            {
                Message = message,
                DetailedMessage = string.Empty,
                LogType = (int)logType,
                RelatedScriptId = relatedScriptId,
                Date = System.DateTime.Now
            };

            s_logs.Add(svl);
        }

        internal static List<ScriptLog> ScriptsLog { get { return s_scriptsLog; } }

        internal static bool HasErrors(string file)
        {
            return s_scriptsLog.Any(x => x.Script.FileName == file);
        }

        internal static void AddError(string errorMessage, Script script, bool isWarningScript)
        {
            ScriptLog scriptLog = new ScriptLog()
            {
                Message = errorMessage.Replace("\r","<br />"),
                Script = script,
                WarningScript = isWarningScript
            };
            s_scriptsLog.Add(scriptLog);

            SystemVersioningLog svl = new SystemVersioningLog()
            {
                Message = scriptLog.Message,
                DetailedMessage = string.Empty,
                LogType = (int)LogTypeEnum.Error,
                RelatedScriptId = script.Id,
                Date = System.DateTime.Now
            };
            s_logs.Add(svl);
        }

        /// <summary>
        /// Save can be executed only when the database is ready etc, so ... This should have a flush feature etc etc.
        /// </summary>
        internal static void SaveLogs()
        {
            foreach(SystemVersioningLog log in s_logs)
            {
                if ( log.Id == 0)
                {
                    s_broker.Save(log);
                }
            }
        }

        public static List<SystemVersioningLog> GetLogs()
        {
            return new List<SystemVersioningLog>(s_logs);
        }
    }
}
