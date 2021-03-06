USE [alexitech]
GO
/****** Object:  Table [dbo].[RequestLog]    Script Date: 4/28/2016 9:39:13 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RequestLog](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NULL,
	[IntentName] [nvarchar](1000) NULL,
	[RequestBody] [ntext] NOT NULL,
	[RequestDate] [datetime] NOT NULL,
 CONSTRAINT [PK_RequestLog] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[User]    Script Date: 4/28/2016 9:39:13 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[User](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[HarmonyUsername] [nvarchar](100) NOT NULL,
	[HarmonyPassword] [nvarchar](100) NOT NULL,
	[HarmonyToken] [nvarchar](100) NOT NULL,
	[AlexaUserID] [nvarchar](500) NOT NULL,
	[AlexaToken] [nvarchar](100) NOT NULL,
	[Hostname] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
ALTER TABLE [dbo].[RequestLog]  WITH CHECK ADD  CONSTRAINT [FK_RequestLog_User] FOREIGN KEY([UserID])
REFERENCES [dbo].[User] ([ID])
GO
ALTER TABLE [dbo].[RequestLog] CHECK CONSTRAINT [FK_RequestLog_User]
GO
