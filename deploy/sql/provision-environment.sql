-- =============================================================================
-- Prima Nota — Environment provisioning (Development / Staging / Production)
-- =============================================================================
--
-- Purpose:
--   Create an isolated database, a SQL Server login, and a db_owner user,
--   so the application can authenticate with a single credential pair.
--
-- How to run (sysadmin required):
--
--   sqlcmd -S <server> -E ^
--          -v DbName="PrimaNota_Staging" ^
--             LoginName="primanota_app_staging" ^
--             LoginPassword="REDACTED_STRONG_PWD" ^
--          -i provision-environment.sql
--
-- Idempotent: every block checks for existence before creating, so re-running
-- the script is safe. Password is NOT reset on re-run — use ALTER LOGIN
-- separately if you need to rotate it.
--
-- Recommended values per environment:
--   Dev         : DbName=PrimaNota_Dev         LoginName=primanota_app_dev
--   Staging     : DbName=PrimaNota_Staging     LoginName=primanota_app_staging
--   Production  : DbName=PrimaNota_Production  LoginName=primanota_app_prod
-- =============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

-- --- 1. Login (instance-level) ---------------------------------------------
IF SUSER_ID('$(LoginName)') IS NULL
BEGIN
    DECLARE @create_login NVARCHAR(MAX) =
        N'CREATE LOGIN [' + N'$(LoginName)' + N'] ' +
        N'WITH PASSWORD = N''' + REPLACE(N'$(LoginPassword)', N'''', N'''''') + N''', ' +
        N'CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;';
    EXEC sp_executesql @create_login;
    PRINT 'Created login [$(LoginName)].';
END
ELSE
BEGIN
    PRINT 'Login [$(LoginName)] already exists, skipping.';
END
GO

-- --- 2. Database ------------------------------------------------------------
IF DB_ID(N'$(DbName)') IS NULL
BEGIN
    DECLARE @create_db NVARCHAR(MAX) =
        N'CREATE DATABASE [' + N'$(DbName)' + N'] COLLATE Latin1_General_CI_AS;';
    EXEC sp_executesql @create_db;
    PRINT 'Created database [$(DbName)].';
END
ELSE
BEGIN
    PRINT 'Database [$(DbName)] already exists, skipping.';
END
GO

-- --- 3. User + db_owner membership -----------------------------------------
USE [$(DbName)];
GO

IF USER_ID(N'$(LoginName)') IS NULL
BEGIN
    DECLARE @create_user NVARCHAR(MAX) =
        N'CREATE USER [' + N'$(LoginName)' + N'] FOR LOGIN [' + N'$(LoginName)' + N'];';
    EXEC sp_executesql @create_user;
    PRINT 'Created user [$(LoginName)] in [$(DbName)].';
END

IF IS_ROLEMEMBER('db_owner', N'$(LoginName)') = 0
BEGIN
    ALTER ROLE db_owner ADD MEMBER [$(LoginName)];
    PRINT 'Granted db_owner to [$(LoginName)] on [$(DbName)].';
END
GO

-- --- 4. Verification --------------------------------------------------------
PRINT '---------------------------------------------------------------';
PRINT 'Provisioning complete for database [$(DbName)].';
PRINT 'Login: [$(LoginName)] with db_owner membership.';
PRINT 'Next step: apply deploy/sql/migrations/*.sql (idempotent) or run';
PRINT '   dotnet ef database update from the Web project.';
PRINT '---------------------------------------------------------------';
GO
