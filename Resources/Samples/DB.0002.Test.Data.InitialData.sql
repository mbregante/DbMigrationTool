﻿DECLARE @Name Varchar(50) = 'Initial Test Data 2';
DECLARE @Version INT = 1;
DECLARE @CreationDay DATETIME = CONVERT(DATETIME, '2021.09.17', 102)

GO
SET IDENTITY_INSERT [dbo].[SomeTable] ON 
GO
INSERT ...
GO
SET IDENTITY_INSERT [dbo].[SomeTable] OFF
GO
