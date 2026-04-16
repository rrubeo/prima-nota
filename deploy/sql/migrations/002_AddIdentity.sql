BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416105439_AddIdentity'
)
BEGIN
    IF SCHEMA_ID(N'identity') IS NULL EXEC(N'CREATE SCHEMA [identity];');
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416105439_AddIdentity'
)
BEGIN
    CREATE TABLE [identity].[AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416105439_AddIdentity'
)
BEGIN
    CREATE TABLE [identity].[AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [LastLoginAt] datetimeoffset NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416105439_AddIdentity'
)
BEGIN
    CREATE TABLE [identity].[AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [identity].[AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416105439_AddIdentity'
)
BEGIN
    CREATE TABLE [identity].[AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416105439_AddIdentity'
)
BEGIN
    CREATE TABLE [identity].[AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416105439_AddIdentity'
)
BEGIN
    CREATE TABLE [identity].[AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [identity].[AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416105439_AddIdentity'
)
BEGIN
    CREATE TABLE [identity].[AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416105439_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [identity].[AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416105439_AddIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [identity].[AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416105439_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [identity].[AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416105439_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [identity].[AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416105439_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [identity].[AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416105439_AddIdentity'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [identity].[AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416105439_AddIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [identity].[AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [app].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416105439_AddIdentity'
)
BEGIN
    INSERT INTO [app].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260416105439_AddIdentity', N'10.0.0');
END;

COMMIT;
GO

