namespace DbMigrationTool
{
    public abstract class OperationResult
    {
        string m_info;
        string m_detailedInfo;
        bool m_success = false;
        object m_data = null;

        public OperationResult(string info)
        {
            this.m_info = info;
        }

        public string Info
        {
            get { return m_info; }
            set { m_info = value; }
        }

        public string DetailedInfo
        {
            get { return m_detailedInfo; }
            set { m_detailedInfo = value; }
        }

        public bool Success
        {
            get
            {
                return m_success;
            }
            set
            {
                m_success = value;
            }
        }

        public object Data
        {
            get
            {
                return m_data;
            }
            set
            {
                m_data = value;
            }
        }
    }

    public class OperationOk : OperationResult
    {
        public OperationOk() : base(string.Empty) { Success = true; }
        public OperationOk(string info) : base(info) { Success = true; }
    }

    public class OperationFailed : OperationResult
    {
        public OperationFailed(string info, string detailedInfo = "", string topExceptionMessage = "", string lastInnerExceptionMessage = "", string stackTrace = "")
            : base(info)
        {
            Success = false;
            DetailedInfo = detailedInfo;
            TopExceptionMessage = topExceptionMessage;
            LastInnerExceptionMessage = lastInnerExceptionMessage;
            StackTrace = stackTrace;
            IsException = !string.IsNullOrEmpty(topExceptionMessage) || !string.IsNullOrEmpty(lastInnerExceptionMessage);
        }

        public string TopExceptionMessage { get; set; }
        public string LastInnerExceptionMessage { get; set; }
        public string StackTrace { get; set; }
        public bool IsException { get; set; }
    }
}
