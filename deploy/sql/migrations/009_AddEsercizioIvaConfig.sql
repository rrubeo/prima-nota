BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416155525_AddEsercizioIvaConfig'
)
BEGIN
    ALTER TABLE [app].[Esercizi] ADD [CoefficienteRedditivitaForfettario] decimal(5,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416155525_AddEsercizioIvaConfig'
)
BEGIN
    ALTER TABLE [app].[Esercizi] ADD [PeriodicitaIva] nvarchar(16) NOT NULL DEFAULT N'Trimestrale';
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416155525_AddEsercizioIvaConfig'
)
BEGIN
    ALTER TABLE [app].[Esercizi] ADD [RegimeIva] nvarchar(24) NOT NULL DEFAULT N'Ordinario';
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416155525_AddEsercizioIvaConfig'
)
BEGIN
    INSERT INTO [app].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260416155525_AddEsercizioIvaConfig', N'10.0.0');
END;

COMMIT;
GO

