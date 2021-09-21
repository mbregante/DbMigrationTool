namespace DbMigrationTool
{
    public class ScriptLog
    {
        public string Message { get; set; }
        public Script Script { get; set; }
        public bool WarningScript { get; set; }
    }
}
