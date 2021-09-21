namespace DbMigrationTool
{
    public enum DbEnvironmentEnum
    {
        Default = 0,
        DevLocal = 1,
        DevTest = 2,
        Test = 3,
        Staging = 4,
        PreProd = 5,
        Prod = 6,
        UnitTesting = 20,
        IntegrationTesting = 21
    }
}
