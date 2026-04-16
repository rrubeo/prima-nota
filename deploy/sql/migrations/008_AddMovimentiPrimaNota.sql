BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416140239_AddMovimentiPrimaNota'
)
BEGIN
    ALTER TABLE [app].[Esercizi] ADD CONSTRAINT [AK_Esercizi_Anno] UNIQUE ([Anno]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416140239_AddMovimentiPrimaNota'
)
BEGIN
    CREATE TABLE [app].[MovimentiPrimaNota] (
        [Id] uniqueidentifier NOT NULL,
        [Data] date NOT NULL,
        [EsercizioAnno] int NOT NULL,
        [Descrizione] nvarchar(500) NOT NULL,
        [Numero] nvarchar(64) NULL,
        [CausaleId] uniqueidentifier NOT NULL,
        [AnagraficaId] uniqueidentifier NULL,
        [Stato] nvarchar(20) NOT NULL,
        [Note] nvarchar(2000) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_MovimentiPrimaNota] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MovimentiPrimaNota_Anagrafiche_AnagraficaId] FOREIGN KEY ([AnagraficaId]) REFERENCES [app].[Anagrafiche] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MovimentiPrimaNota_Causali_CausaleId] FOREIGN KEY ([CausaleId]) REFERENCES [app].[Causali] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MovimentiPrimaNota_Esercizi_EsercizioAnno] FOREIGN KEY ([EsercizioAnno]) REFERENCES [app].[Esercizi] ([Anno]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416140239_AddMovimentiPrimaNota'
)
BEGIN
    CREATE TABLE [app].[AllegatiMovimento] (
        [Id] uniqueidentifier NOT NULL,
        [MovimentoId] uniqueidentifier NOT NULL,
        [NomeFile] nvarchar(260) NOT NULL,
        [MimeType] nvarchar(100) NOT NULL,
        [Size] bigint NOT NULL,
        [HashSha256] nchar(64) NOT NULL,
        [PathRelativo] nvarchar(500) NOT NULL,
        [UploadedAt] datetimeoffset NOT NULL,
        [UploadedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_AllegatiMovimento] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AllegatiMovimento_MovimentiPrimaNota_MovimentoId] FOREIGN KEY ([MovimentoId]) REFERENCES [app].[MovimentiPrimaNota] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416140239_AddMovimentiPrimaNota'
)
BEGIN
    CREATE TABLE [app].[RigheMovimento] (
        [Id] uniqueidentifier NOT NULL,
        [MovimentoId] uniqueidentifier NOT NULL,
        [Importo] decimal(18,2) NOT NULL,
        [ContoFinanziarioId] uniqueidentifier NOT NULL,
        [CategoriaId] uniqueidentifier NOT NULL,
        [AnagraficaId] uniqueidentifier NULL,
        [AliquotaIvaId] uniqueidentifier NULL,
        [Note] nvarchar(500) NULL,
        CONSTRAINT [PK_RigheMovimento] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RigheMovimento_AliquoteIva_AliquotaIvaId] FOREIGN KEY ([AliquotaIvaId]) REFERENCES [app].[AliquoteIva] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RigheMovimento_Anagrafiche_AnagraficaId] FOREIGN KEY ([AnagraficaId]) REFERENCES [app].[Anagrafiche] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RigheMovimento_Categorie_CategoriaId] FOREIGN KEY ([CategoriaId]) REFERENCES [app].[Categorie] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RigheMovimento_ContiFinanziari_ContoFinanziarioId] FOREIGN KEY ([ContoFinanziarioId]) REFERENCES [app].[ContiFinanziari] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RigheMovimento_MovimentiPrimaNota_MovimentoId] FOREIGN KEY ([MovimentoId]) REFERENCES [app].[MovimentiPrimaNota] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416140239_AddMovimentiPrimaNota'
)
BEGIN
    CREATE INDEX [IX_AllegatiMovimento_HashSha256] ON [app].[AllegatiMovimento] ([HashSha256]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416140239_AddMovimentiPrimaNota'
)
BEGIN
    CREATE INDEX [IX_AllegatiMovimento_MovimentoId] ON [app].[AllegatiMovimento] ([MovimentoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416140239_AddMovimentiPrimaNota'
)
BEGIN
    CREATE INDEX [IX_MovimentiPrimaNota_AnagraficaId] ON [app].[MovimentiPrimaNota] ([AnagraficaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416140239_AddMovimentiPrimaNota'
)
BEGIN
    CREATE INDEX [IX_MovimentiPrimaNota_CausaleId] ON [app].[MovimentiPrimaNota] ([CausaleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416140239_AddMovimentiPrimaNota'
)
BEGIN
    CREATE INDEX [IX_MovimentiPrimaNota_EsercizioAnno] ON [app].[MovimentiPrimaNota] ([EsercizioAnno]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416140239_AddMovimentiPrimaNota'
)
BEGIN
    CREATE INDEX [IX_MovimentiPrimaNota_EsercizioAnno_Data] ON [app].[MovimentiPrimaNota] ([EsercizioAnno], [Data]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416140239_AddMovimentiPrimaNota'
)
BEGIN
    CREATE INDEX [IX_MovimentiPrimaNota_Stato] ON [app].[MovimentiPrimaNota] ([Stato]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416140239_AddMovimentiPrimaNota'
)
BEGIN
    CREATE INDEX [IX_RigheMovimento_AliquotaIvaId] ON [app].[RigheMovimento] ([AliquotaIvaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416140239_AddMovimentiPrimaNota'
)
BEGIN
    CREATE INDEX [IX_RigheMovimento_AnagraficaId] ON [app].[RigheMovimento] ([AnagraficaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416140239_AddMovimentiPrimaNota'
)
BEGIN
    CREATE INDEX [IX_RigheMovimento_CategoriaId] ON [app].[RigheMovimento] ([CategoriaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416140239_AddMovimentiPrimaNota'
)
BEGIN
    CREATE INDEX [IX_RigheMovimento_ContoFinanziarioId] ON [app].[RigheMovimento] ([ContoFinanziarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416140239_AddMovimentiPrimaNota'
)
BEGIN
    CREATE INDEX [IX_RigheMovimento_MovimentoId] ON [app].[RigheMovimento] ([MovimentoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416140239_AddMovimentiPrimaNota'
)
BEGIN
    INSERT INTO [app].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260416140239_AddMovimentiPrimaNota', N'10.0.0');
END;

COMMIT;
GO

