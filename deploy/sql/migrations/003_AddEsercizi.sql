BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416110334_AddEsercizi'
)
BEGIN
    IF SCHEMA_ID(N'app') IS NULL EXEC(N'CREATE SCHEMA [app];');
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416110334_AddEsercizi'
)
BEGIN
    CREATE TABLE [app].[Esercizi] (
        [Id] int NOT NULL,
        [Anno] int NOT NULL,
        [DataInizio] date NOT NULL,
        [DataFine] date NOT NULL,
        [Stato] nvarchar(20) NOT NULL,
        [DataChiusura] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_Esercizi] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416110334_AddEsercizi'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Esercizi_Anno] ON [app].[Esercizi] ([Anno]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416110334_AddEsercizi'
)
BEGIN
    INSERT INTO [app].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260416110334_AddEsercizi', N'10.0.0');
END;

COMMIT;
GO

