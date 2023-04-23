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

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230410072238_InitialCreate')
BEGIN
    CREATE TABLE [TimeEntries] (
        [Id] int NOT NULL IDENTITY,
        [Project] nvarchar(max) NOT NULL,
        [Start] datetime2 NOT NULL,
        [End] datetime2 NULL,
        [DateCreated] datetime2 NOT NULL,
        [DateUpdated] datetime2 NULL,
        CONSTRAINT [PK_TimeEntries] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230410072238_InitialCreate')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230410072238_InitialCreate', N'7.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230422032113_Projects')
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TimeEntries]') AND [c].[name] = N'Project');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [TimeEntries] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [TimeEntries] DROP COLUMN [Project];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230422032113_Projects')
BEGIN
    ALTER TABLE [TimeEntries] ADD [ProjectId] int NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230422032113_Projects')
BEGIN
    CREATE TABLE [Projects] (
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

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230422032113_Projects')
BEGIN
    CREATE INDEX [IX_TimeEntries_ProjectId] ON [TimeEntries] ([ProjectId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230422032113_Projects')
BEGIN
    ALTER TABLE [TimeEntries] ADD CONSTRAINT [FK_TimeEntries_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230422032113_Projects')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230422032113_Projects', N'7.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230422042616_ProjectDetails')
BEGIN
    CREATE TABLE [ProjectDetails] (
        [Id] int NOT NULL IDENTITY,
        [Description] nvarchar(max) NULL,
        [StartDate] datetime2 NULL,
        [EndDate] datetime2 NULL,
        [ProjectId] int NOT NULL,
        CONSTRAINT [PK_ProjectDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectDetails_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230422042616_ProjectDetails')
BEGIN
    CREATE UNIQUE INDEX [IX_ProjectDetails_ProjectId] ON [ProjectDetails] ([ProjectId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230422042616_ProjectDetails')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230422042616_ProjectDetails', N'7.0.4');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230422043432_Users')
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230422043432_Users')
BEGIN
    CREATE TABLE [ProjectUser] (
        [ProjectsId] int NOT NULL,
        [UsersId] int NOT NULL,
        CONSTRAINT [PK_ProjectUser] PRIMARY KEY ([ProjectsId], [UsersId]),
        CONSTRAINT [FK_ProjectUser_Projects_ProjectsId] FOREIGN KEY ([ProjectsId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProjectUser_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230422043432_Users')
BEGIN
    CREATE INDEX [IX_ProjectUser_UsersId] ON [ProjectUser] ([UsersId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230422043432_Users')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230422043432_Users', N'7.0.4');
END;
GO

COMMIT;
GO

