BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614075623_AddIntegrazioneArubaAndIdSdi'
)
BEGIN
    ALTER TABLE [app].[MovimentiPrimaNota] ADD [IdentificativoSdi] nvarchar(256) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614075623_AddIntegrazioneArubaAndIdSdi'
)
BEGIN
    CREATE TABLE [app].[IntegrazioneAruba] (
        [Id] int NOT NULL,
        [Abilitata] bit NOT NULL,
        [Username] nvarchar(256) NULL,
        [PasswordProtetta] nvarchar(2048) NULL,
        [UsaDemo] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_IntegrazioneAruba] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614075623_AddIntegrazioneArubaAndIdSdi'
)
BEGIN
    CREATE INDEX [IX_MovimentiPrimaNota_IdentificativoSdi] ON [app].[MovimentiPrimaNota] ([IdentificativoSdi]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614075623_AddIntegrazioneArubaAndIdSdi'
)
BEGIN
    INSERT INTO [app].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260614075623_AddIntegrazioneArubaAndIdSdi', N'10.0.0');
END;

COMMIT;
GO

