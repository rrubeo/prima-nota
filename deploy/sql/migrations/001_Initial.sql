IF OBJECT_ID(N'[app].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'app') IS NULL EXEC(N'CREATE SCHEMA [app];');
    CREATE TABLE [app].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416101748_Initial'
)
BEGIN
    INSERT INTO [app].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260416101748_Initial', N'10.0.0');
END;

COMMIT;
GO

