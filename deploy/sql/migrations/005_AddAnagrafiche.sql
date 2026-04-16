BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416123404_AddAnagrafiche'
)
BEGIN
    CREATE TABLE [app].[Anagrafiche] (
        [Id] uniqueidentifier NOT NULL,
        [RagioneSociale] nvarchar(200) NOT NULL,
        [Nome] nvarchar(100) NULL,
        [Cognome] nvarchar(100) NULL,
        [CodiceFiscale] nvarchar(16) NULL,
        [PartitaIva] nvarchar(16) NULL,
        [PersonaFisica] bit NOT NULL,
        [IsCliente] bit NOT NULL,
        [IsFornitore] bit NOT NULL,
        [IsDipendente] bit NOT NULL,
        [Mansione] nvarchar(100) NULL,
        [DataAssunzione] date NULL,
        [DataCessazione] date NULL,
        [Email] nvarchar(254) NULL,
        [Telefono] nvarchar(32) NULL,
        [Pec] nvarchar(254) NULL,
        [IndirizzoVia] nvarchar(200) NULL,
        [IndirizzoCap] nvarchar(10) NULL,
        [IndirizzoCitta] nvarchar(100) NULL,
        [IndirizzoProvincia] nvarchar(4) NULL,
        [IndirizzoCountryCode] nvarchar(2) NOT NULL,
        [Attivo] bit NOT NULL,
        [Note] nvarchar(2000) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_Anagrafiche] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416123404_AddAnagrafiche'
)
BEGIN
    CREATE INDEX [IX_Anagrafiche_CodiceFiscale] ON [app].[Anagrafiche] ([CodiceFiscale]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416123404_AddAnagrafiche'
)
BEGIN
    CREATE INDEX [IX_Anagrafiche_IsCliente_Attivo] ON [app].[Anagrafiche] ([IsCliente], [Attivo]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416123404_AddAnagrafiche'
)
BEGIN
    CREATE INDEX [IX_Anagrafiche_IsDipendente_Attivo] ON [app].[Anagrafiche] ([IsDipendente], [Attivo]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416123404_AddAnagrafiche'
)
BEGIN
    CREATE INDEX [IX_Anagrafiche_IsFornitore_Attivo] ON [app].[Anagrafiche] ([IsFornitore], [Attivo]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416123404_AddAnagrafiche'
)
BEGIN
    CREATE INDEX [IX_Anagrafiche_PartitaIva] ON [app].[Anagrafiche] ([PartitaIva]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416123404_AddAnagrafiche'
)
BEGIN
    CREATE INDEX [IX_Anagrafiche_RagioneSociale] ON [app].[Anagrafiche] ([RagioneSociale]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416123404_AddAnagrafiche'
)
BEGIN
    INSERT INTO [app].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260416123404_AddAnagrafiche', N'10.0.0');
END;

COMMIT;
GO

