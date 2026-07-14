CREATE TABLE [dbo].[Book] (
    [Id]          INT            NOT NULL,
    [Title]       NVARCHAR (50)  NULL,
    [Description] NVARCHAR (100) NULL,
    [Author]      NVARCHAR (50)  NULL,
    [IsDeleted]   BIT            CONSTRAINT [DF_Book_IsDeleted] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_Book] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE TABLE [dbo].[ProcessedMessage] (
    [MessageId]      UNIQUEIDENTIFIER NOT NULL,
    [ProcessedOnUtc] DATETIME2        NOT NULL,
    CONSTRAINT [PK_ProcessedMessage] PRIMARY KEY CLUSTERED ([MessageId] ASC)
);
GO
