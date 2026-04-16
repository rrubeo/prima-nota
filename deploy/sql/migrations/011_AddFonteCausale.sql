BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416221539_AddFonteCausale'
)
BEGIN
    ALTER TABLE [app].[Causali] ADD [Fonte] nvarchar(16) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416221539_AddFonteCausale'
)
BEGIN
    UPDATE [app].[Causali] SET [Fonte] = N'Fattura' WHERE [Codice] = N'INC-FATT';
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416221539_AddFonteCausale'
)
BEGIN
    UPDATE [app].[Causali] SET [Fonte] = N'Corrispettivo' WHERE [Codice] = N'INC-CASH';
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416221539_AddFonteCausale'
)
BEGIN
    INSERT INTO [app].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260416221539_AddFonteCausale', N'10.0.0');
END;

COMMIT;
GO

