DECLARE @Name Varchar(50) = 'Initial Schema setup';
DECLARE @Version INT = 1;
DECLARE @CreationDay DATETIME = CONVERT(DATETIME, '2021.09.17', 102)

GO
CREATE TABLE [dbo].[SomeTable](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SomeColumn] [nvarchar](max) NOT NULL)
GO