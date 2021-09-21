namespace DbMigrationTool
{
    public class DatabaseVersioningScriptLog
    {
        public string Message { get; set; }
        public DatabaseVersioningScript Script { get; set; }
        public bool WarningScript { get; set; }
    }
}
