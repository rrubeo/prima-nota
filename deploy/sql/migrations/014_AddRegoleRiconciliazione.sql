BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613035147_AddRegoleRiconciliazione'
)
BEGIN
    CREATE TABLE [app].[RegoleRiconciliazione] (
        [Id] uniqueidentifier NOT NULL,
        [ContoFinanziarioId] uniqueidentifier NOT NULL,
        [CausaleOperazione] nvarchar(32) NOT NULL,
        [Operazione] nvarchar(200) NOT NULL,
        [DescrizioneChiave] nvarchar(200) NOT NULL,
        [CausaleId] uniqueidentifier NOT NULL,
        [CategoriaId] uniqueidentifier NOT NULL,
        [AnagraficaId] uniqueidentifier NULL,
        [AliquotaIvaId] uniqueidentifier NULL,
        [ContoDestinazioneId] uniqueidentifier NULL,
        [UtilizziCount] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_RegoleRiconciliazione] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RegoleRiconciliazione_ContiFinanziari_ContoFinanziarioId] FOREIGN KEY ([ContoFinanziarioId]) REFERENCES [app].[ContiFinanziari] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613035147_AddRegoleRiconciliazione'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RegoleRiconciliazione_ContoFinanziarioId_CausaleOperazione_Operazione_DescrizioneChiave] ON [app].[RegoleRiconciliazione] ([ContoFinanziarioId], [CausaleOperazione], [Operazione], [DescrizioneChiave]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613035147_AddRegoleRiconciliazione'
)
BEGIN
    INSERT INTO [app].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260613035147_AddRegoleRiconciliazione', N'10.0.0');
END;

COMMIT;
GO

