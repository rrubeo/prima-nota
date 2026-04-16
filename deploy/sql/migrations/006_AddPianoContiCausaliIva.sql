BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416130229_AddPianoContiCausaliIva'
)
BEGIN
    CREATE TABLE [app].[AliquoteIva] (
        [Id] uniqueidentifier NOT NULL,
        [Codice] nvarchar(16) NOT NULL,
        [Descrizione] nvarchar(200) NOT NULL,
        [Percentuale] decimal(5,2) NOT NULL,
        [PercentualeIndetraibile] decimal(5,2) NOT NULL,
        [Tipo] nvarchar(24) NOT NULL,
        [CodiceNatura] nvarchar(8) NULL,
        [Attiva] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_AliquoteIva] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416130229_AddPianoContiCausaliIva'
)
BEGIN
    CREATE TABLE [app].[Categorie] (
        [Id] uniqueidentifier NOT NULL,
        [Codice] nvarchar(32) NOT NULL,
        [Nome] nvarchar(200) NOT NULL,
        [Natura] nvarchar(16) NOT NULL,
        [Attiva] bit NOT NULL,
        [Note] nvarchar(500) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_Categorie] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416130229_AddPianoContiCausaliIva'
)
BEGIN
    CREATE TABLE [app].[Causali] (
        [Id] uniqueidentifier NOT NULL,
        [Codice] nvarchar(32) NOT NULL,
        [Nome] nvarchar(200) NOT NULL,
        [Tipo] nvarchar(32) NOT NULL,
        [CategoriaDefaultId] uniqueidentifier NULL,
        [Attiva] bit NOT NULL,
        [Note] nvarchar(500) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_Causali] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Causali_Categorie_CategoriaDefaultId] FOREIGN KEY ([CategoriaDefaultId]) REFERENCES [app].[Categorie] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416130229_AddPianoContiCausaliIva'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AliquoteIva_Codice] ON [app].[AliquoteIva] ([Codice]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416130229_AddPianoContiCausaliIva'
)
BEGIN
    CREATE INDEX [IX_AliquoteIva_Tipo_Attiva] ON [app].[AliquoteIva] ([Tipo], [Attiva]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416130229_AddPianoContiCausaliIva'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Categorie_Codice] ON [app].[Categorie] ([Codice]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416130229_AddPianoContiCausaliIva'
)
BEGIN
    CREATE INDEX [IX_Categorie_Natura_Attiva] ON [app].[Categorie] ([Natura], [Attiva]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416130229_AddPianoContiCausaliIva'
)
BEGIN
    CREATE INDEX [IX_Causali_CategoriaDefaultId] ON [app].[Causali] ([CategoriaDefaultId]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416130229_AddPianoContiCausaliIva'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Causali_Codice] ON [app].[Causali] ([Codice]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416130229_AddPianoContiCausaliIva'
)
BEGIN
    CREATE INDEX [IX_Causali_Tipo_Attiva] ON [app].[Causali] ([Tipo], [Attiva]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416130229_AddPianoContiCausaliIva'
)
BEGIN
    INSERT INTO [app].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260416130229_AddPianoContiCausaliIva', N'10.0.0');
END;

COMMIT;
GO

