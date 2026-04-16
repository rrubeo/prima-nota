BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416130858_AddContiFinanziari'
)
BEGIN
    CREATE TABLE [app].[ContiFinanziari] (
        [Id] uniqueidentifier NOT NULL,
        [Codice] nvarchar(32) NOT NULL,
        [Nome] nvarchar(200) NOT NULL,
        [Tipo] nvarchar(32) NOT NULL,
        [Istituto] nvarchar(200) NULL,
        [Iban] nvarchar(34) NULL,
        [Bic] nvarchar(11) NULL,
        [Intestatario] nvarchar(200) NULL,
        [Ultime4Cifre] nchar(4) NULL,
        [SaldoIniziale] decimal(18,2) NOT NULL,
        [DataSaldoIniziale] date NOT NULL,
        [Valuta] nvarchar(3) NOT NULL,
        [Attivo] bit NOT NULL,
        [Note] nvarchar(500) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_ContiFinanziari] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416130858_AddContiFinanziari'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ContiFinanziari_Codice] ON [app].[ContiFinanziari] ([Codice]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416130858_AddContiFinanziari'
)
BEGIN
    CREATE INDEX [IX_ContiFinanziari_Iban] ON [app].[ContiFinanziari] ([Iban]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416130858_AddContiFinanziari'
)
BEGIN
    CREATE INDEX [IX_ContiFinanziari_Tipo_Attivo] ON [app].[ContiFinanziari] ([Tipo], [Attivo]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416130858_AddContiFinanziari'
)
BEGIN
    INSERT INTO [app].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260416130858_AddContiFinanziari', N'10.0.0');
END;

COMMIT;
GO

