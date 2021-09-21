﻿SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[SYSTEM_VERSIONING](
 [ScriptId] [int] NOT NULL,
 [Name] [varchar](50) NOT NULL,
 [Version] [int] NOT NULL,
 [CreationDay] [datetime] NOT NULL,
 [ImpactedDay] [datetime] NOT NULL,
 CONSTRAINT [PK_System_Versioning] PRIMARY KEY CLUSTERED 
(
 [ScriptId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

CREATE TABLE [dbo].[SYSTEM_VERSIONING_LOG](
 [Id] [int] IDENTITY(1,1) NOT NULL,
 [Message] [nvarchar](1000) NOT NULL,
 [DetailedMessage] [nvarchar](max) NULL,
 [LogType] [int] NOT NULL,
 [RelatedScriptId] [int] NULL,
 [Date] [datetime] NOT NULL default(getdate())
 CONSTRAINT [PK_System_Versioning_Log] PRIMARY KEY CLUSTERED 
(
 [Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO