DECLARE @Name Varchar(50) = 'Initial Schema setup';
DECLARE @Version INT = 1;
DECLARE @CreationDay DATETIME = CONVERT(DATETIME, '2021.09.17', 102)

GO
CREATE TABLE [dbo].[LogActivity](
	[LogActivityID] [int] IDENTITY(1,1) NOT NULL,
	[Message] [nvarchar](max) NOT NULL,
	[Details] [text] NULL,
	[NotificationType] [nvarchar](255) NOT NULL,
	[NotificationSource] [nvarchar](255) NOT NULL,
	[ApplicationVersion] [nvarchar](255) NULL,
	[NotificationTypeID] [int] NULL,
	[NotificationSourceID] [int] NULL,
	[CreatedDate] [datetime] NOT NULL,
	[CreatedBy] [nvarchar](128) NOT NULL,
	[UpdatedDate] [datetime] NOT NULL,
	[UpdatedBy] [nvarchar](128) NOT NULL,
 CONSTRAINT [PK_Log_Activity] PRIMARY KEY CLUSTERED 
(
	[LogActivityID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
CREATE TABLE [dbo].[Project](
	[ProjectID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
	[ReferenceData] [nvarchar](max) NOT NULL,
	[StatusID] [int] NOT NULL,
	[IsArchived] [bit] NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[CreatedBy] [nvarchar](128) NOT NULL,
	[UpdatedDate] [datetime] NOT NULL,
	[UpdatedBy] [nvarchar](128) NOT NULL,
	[ProjectResult] [nvarchar](max) NULL,
	[SettingsData] [nvarchar](max) NULL,
	[Comment] [nvarchar](max) NULL,
	[Description] [nvarchar](max) NULL,
	[MasterAgencyDivisionID] [int] NOT NULL,
	[MasterBusinessUnitID] [int] NOT NULL,
	[MasterCountryID] [int] NOT NULL,
	[MasterRegionID] [int] NOT NULL,
 CONSTRAINT [PK_PROJECT] PRIMARY KEY CLUSTERED 
(
	[ProjectID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO