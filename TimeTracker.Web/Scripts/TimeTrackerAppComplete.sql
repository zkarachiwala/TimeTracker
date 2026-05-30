IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230701075147_Initial')
BEGIN
    IF SCHEMA_ID(N'app') IS NULL EXEC(N'CREATE SCHEMA [app];');
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230701075147_Initial')
BEGIN
    CREATE TABLE [app].[Projects] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [DateCreated] datetime2 NOT NULL,
        [DateUpdated] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DateDeleted] datetime2 NULL,
        CONSTRAINT [PK_Projects] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230701075147_Initial')
BEGIN
    CREATE TABLE [app].[ProjectDetails] (
        [Id] int NOT NULL IDENTITY,
        [Description] nvarchar(max) NULL,
        [StartDate] datetime2 NULL,
        [EndDate] datetime2 NULL,
        [ProjectId] int NOT NULL,
        CONSTRAINT [PK_ProjectDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectDetails_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [app].[Projects] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230701075147_Initial')
BEGIN
    CREATE TABLE [app].[TimeEntries] (
        [Id] int NOT NULL IDENTITY,
        [ProjectId] int NULL,
        [Start] datetime2 NOT NULL,
        [End] datetime2 NULL,
        [UserId] nvarchar(max) NOT NULL,
        [DateCreated] datetime2 NOT NULL,
        [DateUpdated] datetime2 NULL,
        CONSTRAINT [PK_TimeEntries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TimeEntries_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [app].[Projects] ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230701075147_Initial')
BEGIN
    CREATE TABLE [app].[UserId] (
        [Id] nvarchar(450) NOT NULL,
        [ProjectId] int NULL,
        CONSTRAINT [PK_UserId] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserId_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [app].[Projects] ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230701075147_Initial')
BEGIN
    CREATE UNIQUE INDEX [IX_ProjectDetails_ProjectId] ON [app].[ProjectDetails] ([ProjectId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230701075147_Initial')
BEGIN
    CREATE INDEX [IX_TimeEntries_ProjectId] ON [app].[TimeEntries] ([ProjectId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230701075147_Initial')
BEGIN
    CREATE INDEX [IX_UserId_ProjectId] ON [app].[UserId] ([ProjectId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230701075147_Initial')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230701075147_Initial', N'7.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230709013755_AppUser')
BEGIN
    DROP TABLE [app].[UserId];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230709013755_AppUser')
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[TimeEntries]') AND [c].[name] = N'UserId');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [app].[TimeEntries] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [app].[TimeEntries] DROP COLUMN [UserId];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230709013755_AppUser')
BEGIN
    ALTER TABLE [app].[TimeEntries] ADD [AppUserId] nvarchar(450) NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230709013755_AppUser')
BEGIN
    CREATE TABLE [app].[AppUsers] (
        [Id] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AppUsers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230709013755_AppUser')
BEGIN
    CREATE TABLE [app].[AppUserProject] (
        [AppUsersId] nvarchar(450) NOT NULL,
        [ProjectsId] int NOT NULL,
        CONSTRAINT [PK_AppUserProject] PRIMARY KEY ([AppUsersId], [ProjectsId]),
        CONSTRAINT [FK_AppUserProject_AppUsers_AppUsersId] FOREIGN KEY ([AppUsersId]) REFERENCES [app].[AppUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AppUserProject_Projects_ProjectsId] FOREIGN KEY ([ProjectsId]) REFERENCES [app].[Projects] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230709013755_AppUser')
BEGIN
    CREATE INDEX [IX_TimeEntries_AppUserId] ON [app].[TimeEntries] ([AppUserId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230709013755_AppUser')
BEGIN
    CREATE INDEX [IX_AppUserProject_ProjectsId] ON [app].[AppUserProject] ([ProjectsId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230709013755_AppUser')
BEGIN
    ALTER TABLE [app].[TimeEntries] ADD CONSTRAINT [FK_TimeEntries_AppUsers_AppUserId] FOREIGN KEY ([AppUserId]) REFERENCES [app].[AppUsers] ([Id]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230709013755_AppUser')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230709013755_AppUser', N'7.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230709023946_AppUserIdToTimeEntity')
BEGIN
    ALTER TABLE [app].[TimeEntries] DROP CONSTRAINT [FK_TimeEntries_AppUsers_AppUserId];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230709023946_AppUserIdToTimeEntity')
BEGIN
    DROP INDEX [IX_TimeEntries_AppUserId] ON [app].[TimeEntries];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230709023946_AppUserIdToTimeEntity')
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[TimeEntries]') AND [c].[name] = N'AppUserId');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [app].[TimeEntries] DROP CONSTRAINT [' + @var1 + '];');
    EXEC(N'UPDATE [app].[TimeEntries] SET [AppUserId] = N'''' WHERE [AppUserId] IS NULL');
    ALTER TABLE [app].[TimeEntries] ALTER COLUMN [AppUserId] nvarchar(max) NOT NULL;
    ALTER TABLE [app].[TimeEntries] ADD DEFAULT N'' FOR [AppUserId];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230709023946_AppUserIdToTimeEntity')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230709023946_AppUserIdToTimeEntity', N'7.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230709024106_AppUserToProject')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230709024106_AppUserToProject', N'7.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230709060431_SimplifyModel')
BEGIN
    DROP TABLE [app].[AppUserProject];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230709060431_SimplifyModel')
BEGIN
    DROP TABLE [app].[AppUsers];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230709060431_SimplifyModel')
BEGIN
    EXEC sp_rename N'[app].[TimeEntries].[AppUserId]', N'UserId', N'COLUMN';
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230709060431_SimplifyModel')
BEGIN
    CREATE TABLE [app].[ProjectUsers] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(max) NOT NULL,
        [ProjectId] int NOT NULL,
        CONSTRAINT [PK_ProjectUsers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectUsers_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [app].[Projects] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230709060431_SimplifyModel')
BEGIN
    CREATE INDEX [IX_ProjectUsers_ProjectId] ON [app].[ProjectUsers] ([ProjectId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230709060431_SimplifyModel')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230709060431_SimplifyModel', N'7.0.8');
END;
GO

COMMIT;
GO

