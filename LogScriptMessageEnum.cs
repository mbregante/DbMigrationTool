namespace DbMigrationTool
{
    internal enum LogScriptMessageEnum
    {
        ScriptToRun,
        InsertStatementMissing,
        StartSymbolMissing,
        EndSymbolMissing,
        IdMissing,
        VersionMissing,
        NameMissing,
        DateMissing,
        IdValueInvalid,
        NameValueInvalid,
        VersionValueInvalid,
        DateValueInvalid,
        MissingDataHeader,
        DBDeclaration,
        ConnectionStrKeyInvalid
    }
}
