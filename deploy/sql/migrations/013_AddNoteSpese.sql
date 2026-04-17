BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260417010250_AddNoteSpese'
)
BEGIN
    CREATE TABLE [app].[NoteSpese] (
        [Id] uniqueidentifier NOT NULL,
        [DipendenteId] uniqueidentifier NOT NULL,
        [Mese] int NOT NULL,
        [Anno] int NOT NULL,
        [Descrizione] nvarchar(500) NOT NULL,
        [Stato] nvarchar(20) NOT NULL,
        [MotivoRifiuto] nvarchar(1000) NULL,
        [MovimentoRimborsoId] uniqueidentifier NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_NoteSpese] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_NoteSpese_Anagrafiche_DipendenteId] FOREIGN KEY ([DipendenteId]) REFERENCES [app].[Anagrafiche] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260417010250_AddNoteSpese'
)
BEGIN
    CREATE TABLE [app].[RigheSpesa] (
        [Id] uniqueidentifier NOT NULL,
        [NotaSpeseId] uniqueidentifier NOT NULL,
        [Data] date NOT NULL,
        [Descrizione] nvarchar(500) NOT NULL,
        [Importo] decimal(18,2) NOT NULL,
        [CategoriaId] uniqueidentifier NOT NULL,
        [TipoPagamento] nvarchar(20) NOT NULL,
        [AllegatoPath] nvarchar(500) NULL,
        CONSTRAINT [PK_RigheSpesa] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RigheSpesa_NoteSpese_NotaSpeseId] FOREIGN KEY ([NotaSpeseId]) REFERENCES [app].[NoteSpese] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260417010250_AddNoteSpese'
)
BEGIN
    CREATE INDEX [IX_NoteSpese_Anno_Mese] ON [app].[NoteSpese] ([Anno], [Mese]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260417010250_AddNoteSpese'
)
BEGIN
    CREATE INDEX [IX_NoteSpese_DipendenteId] ON [app].[NoteSpese] ([DipendenteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260417010250_AddNoteSpese'
)
BEGIN
    CREATE INDEX [IX_NoteSpese_Stato] ON [app].[NoteSpese] ([Stato]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260417010250_AddNoteSpese'
)
BEGIN
    CREATE INDEX [IX_RigheSpesa_CategoriaId] ON [app].[RigheSpesa] ([CategoriaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260417010250_AddNoteSpese'
)
BEGIN
    CREATE INDEX [IX_RigheSpesa_NotaSpeseId] ON [app].[RigheSpesa] ([NotaSpeseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260417010250_AddNoteSpese'
)
BEGIN
    INSERT INTO [app].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260417010250_AddNoteSpese', N'10.0.0');
END;

COMMIT;
GO

