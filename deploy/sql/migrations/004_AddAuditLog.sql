BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416110610_AddAuditLog'
)
BEGIN
    CREATE TABLE [app].[AuditLog] (
        [Id] uniqueidentifier NOT NULL,
        [OccurredAt] datetimeoffset NOT NULL,
        [Kind] nvarchar(64) NOT NULL,
        [UserId] nvarchar(450) NULL,
        [UserName] nvarchar(256) NULL,
        [TargetType] nvarchar(256) NOT NULL,
        [TargetId] nvarchar(64) NOT NULL,
        [Summary] nvarchar(512) NOT NULL,
        [PayloadJson] nvarchar(max) NULL,
        [CorrelationId] nvarchar(128) NULL,
        [IpAddress] nvarchar(64) NULL,
        CONSTRAINT [PK_AuditLog] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416110610_AddAuditLog'
)
BEGIN
    CREATE INDEX [IX_AuditLog_Kind] ON [app].[AuditLog] ([Kind]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416110610_AddAuditLog'
)
BEGIN
    CREATE INDEX [IX_AuditLog_OccurredAt] ON [app].[AuditLog] ([OccurredAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416110610_AddAuditLog'
)
BEGIN
    CREATE INDEX [IX_AuditLog_UserId_OccurredAt] ON [app].[AuditLog] ([UserId], [OccurredAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416110610_AddAuditLog'
)
BEGIN
    INSERT INTO [app].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260416110610_AddAuditLog', N'10.0.0');
END;

COMMIT;
GO

