BEGIN TRANSACTION;
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TimeEntries]') AND [c].[name] = N'Project');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [TimeEntries] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [TimeEntries] DROP COLUMN [Project];
GO

ALTER TABLE [TimeEntries] ADD [ProjectId] int NULL;
GO

CREATE TABLE [Projects] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [DateCreated] datetime2 NOT NULL,
    [DateUpdated] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [DateDeleted] datetime2 NULL,
    CONSTRAINT [PK_Projects] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_TimeEntries_ProjectId] ON [TimeEntries] ([ProjectId]);
GO

ALTER TABLE [TimeEntries] ADD CONSTRAINT [FK_TimeEntries_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20230422032113_Projects', N'7.0.4');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [ProjectDetails] (
    [Id] int NOT NULL IDENTITY,
    [Description] nvarchar(max) NULL,
    [StartDate] datetime2 NULL,
    [EndDate] datetime2 NULL,
    [ProjectId] int NOT NULL,
    CONSTRAINT [PK_ProjectDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProjectDetails_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_ProjectDetails_ProjectId] ON [ProjectDetails] ([ProjectId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20230422042616_ProjectDetails', N'7.0.4');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ProjectUser] (
    [ProjectsId] int NOT NULL,
    [UsersId] int NOT NULL,
    CONSTRAINT [PK_ProjectUser] PRIMARY KEY ([ProjectsId], [UsersId]),
    CONSTRAINT [FK_ProjectUser_Projects_ProjectsId] FOREIGN KEY ([ProjectsId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProjectUser_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_ProjectUser_UsersId] ON [ProjectUser] ([UsersId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20230422043432_Users', N'7.0.4');
GO

COMMIT;
GO

