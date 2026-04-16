BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416211717_AddConfigurazioneAziendaAndPagamenti'
)
BEGIN
    ALTER TABLE [app].[MovimentiPrimaNota] ADD [DataCompetenza] date NOT NULL DEFAULT '0001-01-01';
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416211717_AddConfigurazioneAziendaAndPagamenti'
)
BEGIN
    UPDATE [app].[MovimentiPrimaNota] SET [DataCompetenza] = [Data] WHERE [DataCompetenza] = '0001-01-01';
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416211717_AddConfigurazioneAziendaAndPagamenti'
)
BEGIN
    CREATE TABLE [app].[ConfigurazioneAzienda] (
        [Id] int NOT NULL,
        [Denominazione] nvarchar(200) NOT NULL,
        [PartitaIva] nvarchar(16) NULL,
        [CodiceFiscale] nvarchar(16) NULL,
        [IndirizzoVia] nvarchar(200) NULL,
        [IndirizzoCap] nvarchar(10) NULL,
        [IndirizzoCitta] nvarchar(100) NULL,
        [IndirizzoProvincia] nvarchar(4) NULL,
        [IndirizzoCountryCode] nvarchar(2) NOT NULL,
        [Email] nvarchar(254) NULL,
        [Telefono] nvarchar(32) NULL,
        [Pec] nvarchar(254) NULL,
        [EsigibilitaIvaPredefinita] nvarchar(16) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_ConfigurazioneAzienda] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416211717_AddConfigurazioneAziendaAndPagamenti'
)
BEGIN
    CREATE TABLE [app].[PagamentiMovimento] (
        [Id] uniqueidentifier NOT NULL,
        [MovimentoId] uniqueidentifier NOT NULL,
        [Data] date NOT NULL,
        [Importo] decimal(18,2) NOT NULL,
        [ContoFinanziarioId] uniqueidentifier NOT NULL,
        [Note] nvarchar(500) NULL,
        CONSTRAINT [PK_PagamentiMovimento] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PagamentiMovimento_ContiFinanziari_ContoFinanziarioId] FOREIGN KEY ([ContoFinanziarioId]) REFERENCES [app].[ContiFinanziari] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PagamentiMovimento_MovimentiPrimaNota_MovimentoId] FOREIGN KEY ([MovimentoId]) REFERENCES [app].[MovimentiPrimaNota] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416211717_AddConfigurazioneAziendaAndPagamenti'
)
BEGIN
    CREATE INDEX [IX_MovimentiPrimaNota_EsercizioAnno_DataCompetenza] ON [app].[MovimentiPrimaNota] ([EsercizioAnno], [DataCompetenza]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416211717_AddConfigurazioneAziendaAndPagamenti'
)
BEGIN
    CREATE INDEX [IX_PagamentiMovimento_ContoFinanziarioId] ON [app].[PagamentiMovimento] ([ContoFinanziarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416211717_AddConfigurazioneAziendaAndPagamenti'
)
BEGIN
    CREATE INDEX [IX_PagamentiMovimento_Data] ON [app].[PagamentiMovimento] ([Data]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416211717_AddConfigurazioneAziendaAndPagamenti'
)
BEGIN
    CREATE INDEX [IX_PagamentiMovimento_MovimentoId] ON [app].[PagamentiMovimento] ([MovimentoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416211717_AddConfigurazioneAziendaAndPagamenti'
)
BEGIN
    INSERT INTO [app].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260416211717_AddConfigurazioneAziendaAndPagamenti', N'10.0.0');
END;

COMMIT;
GO

