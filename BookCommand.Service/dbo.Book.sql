CREATE TABLE [dbo].[Book] (
    [Id]          INT            IDENTITY (1, 1) NOT NULL,
    [Title]       NVARCHAR (50)  NOT NULL,
    [Description] NVARCHAR (100) NULL,
    [Author]      NVARCHAR (50)  NULL,
    [IsDeleted]   BIT            CONSTRAINT [DF_Book_IsDeleted] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_Book] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE TABLE [dbo].[OutboxMessage] (
    [Id]             UNIQUEIDENTIFIER NOT NULL,
    [Type]           NVARCHAR (100)   NOT NULL,
    [Payload]        NVARCHAR (MAX)   NOT NULL,
    [OccurredOnUtc]  DATETIME2        NOT NULL,
    [ProcessedOnUtc] DATETIME2        NULL,
    [Error]          NVARCHAR (MAX)   NULL,
    [RetryCount]     INT              NOT NULL CONSTRAINT [DF_OutboxMessage_RetryCount] DEFAULT ((0)),
    CONSTRAINT [PK_OutboxMessage] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE INDEX [IX_OutboxMessage_ProcessedOnUtc] ON [dbo].[OutboxMessage] ([ProcessedOnUtc]);
GO
