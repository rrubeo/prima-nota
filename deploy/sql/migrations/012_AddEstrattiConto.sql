BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416224825_AddEstrattiConto'
)
BEGIN
    CREATE TABLE [app].[EstrattiConto] (
        [Id] uniqueidentifier NOT NULL,
        [ContoFinanziarioId] uniqueidentifier NOT NULL,
        [NomeFile] nvarchar(260) NOT NULL,
        [PeriodoDa] date NOT NULL,
        [PeriodoA] date NOT NULL,
        [SaldoContabile] decimal(18,2) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_EstrattiConto] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EstrattiConto_ContiFinanziari_ContoFinanziarioId] FOREIGN KEY ([ContoFinanziarioId]) REFERENCES [app].[ContiFinanziari] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416224825_AddEstrattiConto'
)
BEGIN
    CREATE TABLE [app].[RigheEstrattoConto] (
        [Id] uniqueidentifier NOT NULL,
        [ImportId] uniqueidentifier NOT NULL,
        [DataContabile] date NOT NULL,
        [DataValuta] date NOT NULL,
        [CausaleOperazione] nvarchar(32) NULL,
        [Operazione] nvarchar(200) NULL,
        [Descrizione] nvarchar(1000) NULL,
        [Importo] decimal(18,2) NOT NULL,
        [Stato] nvarchar(20) NOT NULL,
        [MovimentoId] uniqueidentifier NULL,
        [PagamentoId] uniqueidentifier NULL,
        CONSTRAINT [PK_RigheEstrattoConto] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RigheEstrattoConto_EstrattiConto_ImportId] FOREIGN KEY ([ImportId]) REFERENCES [app].[EstrattiConto] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416224825_AddEstrattiConto'
)
BEGIN
    CREATE INDEX [IX_EstrattiConto_ContoFinanziarioId] ON [app].[EstrattiConto] ([ContoFinanziarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416224825_AddEstrattiConto'
)
BEGIN
    CREATE INDEX [IX_RigheEstrattoConto_DataContabile] ON [app].[RigheEstrattoConto] ([DataContabile]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416224825_AddEstrattiConto'
)
BEGIN
    CREATE INDEX [IX_RigheEstrattoConto_ImportId] ON [app].[RigheEstrattoConto] ([ImportId]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416224825_AddEstrattiConto'
)
BEGIN
    CREATE INDEX [IX_RigheEstrattoConto_MovimentoId] ON [app].[RigheEstrattoConto] ([MovimentoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416224825_AddEstrattiConto'
)
BEGIN
    CREATE INDEX [IX_RigheEstrattoConto_Stato] ON [app].[RigheEstrattoConto] ([Stato]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416224825_AddEstrattiConto'
)
BEGIN
    INSERT INTO [app].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260416224825_AddEstrattiConto', N'10.0.0');
END;

COMMIT;
GO

