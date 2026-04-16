# Deployment Guide — Prima Nota

> Target: **IIS 10+** su **Windows Server 2022+**, con **SQL Server 2022** raggiungibile dalla app.

## Contenuti

- [1. Prerequisiti server](#1-prerequisiti-server)
- [2. Primo setup IIS](#2-primo-setup-iis)
- [3. Database — primo setup](#3-database--primo-setup)
- [4. Segreti e configurazione](#4-segreti-e-configurazione)
- [5. Deploy via GitHub Actions](#5-deploy-via-github-actions)
- [6. Smoke test post-deploy](#6-smoke-test-post-deploy)
- [7. Rollback](#7-rollback)
- [8. Ambiente di sviluppo locale](#8-ambiente-di-sviluppo-locale)

---

## 1. Prerequisiti server

Su ogni server IIS (staging e produzione):

1. **Windows Server 2022 Standard** o superiore.
2. **IIS 10** con feature:
   - Web Server (IIS) → Application Development → WebSocket Protocol (obbligatorio per Blazor Server/SignalR)
   - Web Server (IIS) → Management Tools → Management Service (per WebDeploy da GitHub Actions)
3. **ASP.NET Core Hosting Bundle** per **.NET 10** (installa il modulo `AspNetCoreModuleV2`).
4. **Web Deploy 4.0** (per `msdeploy.exe` usato dal workflow).
5. **URL Rewrite Module** (consigliato per redirect).
6. Certificato HTTPS valido installato in `LocalMachine\My` (annotare il thumbprint).
7. Service account dedicato (o Managed Service Account) con permessi `Execute` sulla cartella dell'app e `read/write` sulla cartella `attachments`.

### Prerequisiti SQL Server

- **SQL Server 2022 Standard** (o 2019 minimo) raggiungibile dalla macchina IIS.
- Abilitare **TDE** (Transparent Data Encryption) sul database `PrimaNota`.
- Creare due login SQL separati:
  - `primanota_app` — login runtime con permessi `db_datareader + db_datawriter + db_ddladmin` (serve a Hangfire per creare il suo schema).
  - `primanota_migrator` — login di deploy con `db_owner` (usato solo da `sqlcmd` nel workflow per applicare migrations).
- Pianificare backup: full giornaliero + log ogni 15 min.

---

## 2. Primo setup IIS

Eseguire lo script PowerShell in `deploy/iis/app-pool-setup.ps1` **come Administrator** sul server IIS:

```powershell
.\app-pool-setup.ps1 `
    -Environment Staging `
    -PhysicalPath "D:\Apps\PrimaNota.Staging" `
    -Hostname "primanota-staging.azienda.local" `
    -CertificateThumbprint "0123ABCD..."
```

Cosa fa lo script (idempotente):

- Crea App Pool `PrimaNota.Staging` in modalità `No Managed Code`, `AlwaysRunning`, `idleTimeout=0`.
- Crea sito `PrimaNota.Staging` con binding HTTPS sul hostname fornito.
- Lega il certificato TLS al binding.
- Avvia sito e app pool.

Copiare `deploy/iis/web.config` nella physical path iniziale (il workflow lo include automaticamente nelle publish successive).

---

## 3. Database — primo setup

Creare il database vuoto:

```sql
CREATE DATABASE PrimaNota COLLATE Latin1_General_CI_AS;
GO

-- Abilita TDE (richiede DEK + certificato nel master). Vedi docs SQL Server.

USE PrimaNota;
GO

CREATE USER primanota_app FOR LOGIN primanota_app;
EXEC sp_addrolemember 'db_datareader', 'primanota_app';
EXEC sp_addrolemember 'db_datawriter', 'primanota_app';
EXEC sp_addrolemember 'db_ddladmin',   'primanota_app';   -- Hangfire creates its schema
```

Applicare in ordine gli script in `deploy/sql/migrations/`:

```
001_Initial.sql
002_AddIdentity.sql
003_AddEsercizi.sql
004_AddAuditLog.sql
... (gli script sono idempotenti, possono essere ri-applicati senza effetti collaterali)
```

Sul workflow di deploy, `migrations.sql` è una singola concatenazione idempotente generata con `dotnet ef migrations script --idempotent`: può essere applicata a un DB vuoto o già popolato indifferentemente.

---

## 4. Segreti e configurazione

I segreti **non** vivono in `appsettings.*.json`: sono environment variables lette da IIS.

| Variabile | Obbligatoria | Descrizione |
|-----------|--------------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Sì | `Staging` o `Production` |
| `Database__ConnectionString` | Sì | Connection string SQL Server (login `primanota_app`) |
| `Authentication__Google__ClientId` | Se Google OAuth attivo | OAuth client id |
| `Authentication__Google__ClientSecret` | Se Google OAuth attivo | OAuth client secret |
| `Identity__Bootstrap__Email` | Solo al primo avvio | Email admin iniziale |
| `Identity__Bootstrap__Password` | Solo al primo avvio | Password admin (≥12 caratteri, strong) |
| `Identity__Bootstrap__FullName` | Solo al primo avvio | Nome admin iniziale |
| `DOTNET_ENVIRONMENT` | No | Ignorato da ASP.NET Core, ma utile per tool EF |

### Configurazione via appcmd

```powershell
$Env = 'Staging'
$Pool = "PrimaNota.$Env"

C:\Windows\System32\inetsrv\appcmd.exe set config -section:system.applicationHost/applicationPools `
  /[name='$Pool'].environmentVariables.[name='ASPNETCORE_ENVIRONMENT',value='Staging'] /commit:apphost

C:\Windows\System32\inetsrv\appcmd.exe set config -section:system.applicationHost/applicationPools `
  /[name='$Pool'].environmentVariables.[name='Database__ConnectionString',value='Server=...;Database=PrimaNota;User Id=primanota_app;Password=...;Encrypt=True;'] /commit:apphost
```

Dopo aver popolato l'admin iniziale con successo, **rimuovere le variabili `Identity__Bootstrap__*`** e riavviare l'app pool.

---

## 5. Deploy via GitHub Actions

### Segreti e variabili da configurare (una tantum)

Su GitHub → Settings → Environments → `staging` (anche `production`):

**Secrets:**
- `STAGING_DB_ADMIN_CONN` — connection string con login `primanota_migrator` (solo per applicare migration).
- `STAGING_IIS_USER` — utente abilitato a WebDeploy.
- `STAGING_IIS_PASS` — password.

**Variables:**
- `STAGING_IIS_HOST` — es. `iis-staging.azienda.local`.
- `STAGING_IIS_SITE` — es. `PrimaNota.Staging`.
- `STAGING_HOSTNAME` — es. `primanota-staging.azienda.local`.

Se `STAGING_IIS_HOST` non è impostato, il job `deploy` viene saltato (il workflow produce comunque l'artefatto).

### Trigger

- Automatico su `push` al branch **`develop`**.
- Manuale da **Actions → Deploy · Staging → Run workflow**.

### Flusso del workflow

1. `publish`: build Release, `dotnet publish` con runtime `win-x64`, generazione `migrations.sql` idempotente, copia di `web.config`, upload artefatto.
2. `deploy`: apply `migrations.sql` via `sqlcmd`, deploy via `msdeploy.exe`, smoke test su `/health`.

Per il deploy in **produzione**, duplicare `deploy-staging.yml` in `deploy-production.yml` con:
- trigger su `push` a `main`
- `environment: production` (con protection rules: approvazione manuale obbligatoria).

---

## 6. Smoke test post-deploy

Il workflow esegue automaticamente:

```
GET https://<hostname>/health  → 200 OK (JSON con stato componenti)
```

Check manuali aggiuntivi:

```powershell
Invoke-WebRequest https://primanota-staging.azienda.local/health/ready | Select-Object StatusCode
```

Verificare nel visualizzatore eventi Windows (o nei log Serilog in `D:\Apps\PrimaNota.Staging\logs\`) che:

- `Starting PrimaNota.Web` compare
- `Now listening on` compare
- Nessun `ERROR`/`FATAL` ricorrente

---

## 7. Rollback

1. Fermare app pool: `Stop-WebAppPool -Name "PrimaNota.Staging"`.
2. Ripristinare l'artefatto della versione precedente (cartella `publish/` in storage o ri-deploy della versione N-1 via workflow `Run workflow` selezionando tag/commit).
3. Se le migration hanno introdotto cambi schema non retrocompatibili, ripristinare il DB dal backup pre-deploy (la finestra di backup è documentata in `runbook.md` — TODO modulo 17).
4. Riavviare app pool e rifare smoke test.

---

## 8. Ambiente di sviluppo locale

### Prerequisiti

- .NET SDK 10.0.201 (rispetta `global.json`, `rollForward: latestFeature`)
- Docker Desktop (per Testcontainers e opzionalmente SQL Server container)
- Git

### Database locale

**Opzione A — SQL Server in Docker:**

```bash
docker run --name mssql-primanota -d \
  -e ACCEPT_EULA=Y \
  -e MSSQL_SA_PASSWORD='Strong_Password_123!' \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest
```

Connection string da usare in `dotnet user-secrets`:

```bash
cd src/PrimaNota.Web
dotnet user-secrets init
dotnet user-secrets set "Database:ConnectionString" "Server=localhost,1433;Database=PrimaNota_Dev;User Id=sa;Password=Strong_Password_123!;TrustServerCertificate=True;"
dotnet user-secrets set "Identity:Bootstrap:Email" "admin@local"
dotnet user-secrets set "Identity:Bootstrap:Password" "Admin_Password_123!"
dotnet user-secrets set "Identity:Bootstrap:FullName" "Administrator"
```

**Opzione B — SQL Server LocalDB** (Windows): la connection string di default in `appsettings.json` punta già a `(localdb)\\mssqllocaldb`.

### Avvio

```bash
dotnet restore PrimaNota.slnx
dotnet run --project src/PrimaNota.Web --launch-profile https
```

L'app applica migrations e seeda ruoli + admin al primo avvio. Connettiti a `https://localhost:7070`, login con le credenziali bootstrap.

### Test

```bash
# Unit + component (no Docker)
dotnet test tests/PrimaNota.UnitTests
dotnet test tests/PrimaNota.ComponentTests

# Integration (richiede Docker in esecuzione)
dotnet test tests/PrimaNota.IntegrationTests
```
