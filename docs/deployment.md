# Deployment Guide — Prima Nota

> Target: **IIS 10+** su **Windows Server 2022+**, con **SQL Server 2022** raggiungibile dalla app.

## Ambienti

Il progetto prevede tre ambienti **completamente isolati**: database, login, server,
configurazione — niente viene condiviso tra ambienti.

| Aspetto | Development | Staging | Production |
|---------|-------------|---------|------------|
| **Scopo** | sviluppo locale, unit/integration test | validazione pre-rilascio, UAT, smoke test di deploy | dati reali aziendali |
| **Nome DB** | `PrimaNota_Dev` | `PrimaNota_Staging` | `PrimaNota_Production` |
| **Server DB** | LocalDB oppure SQL Server in Docker sulla workstation | `sql-staging.azienda.local` | `sql.azienda.local` |
| **Login SQL** | SA LocalDB (Windows auth) oppure `sa` del container | `primanota_app_staging` (db_owner) | `primanota_app_prod` (db_owner) |
| **Host app** | `https://localhost:7070` (`dotnet run`) | IIS su `iis-staging.azienda.local` | IIS su `iis.azienda.local` |
| **Hostname** | `localhost` | `primanota-staging.azienda.local` | `primanota.azienda.local` |
| **Dove vive la connection string** | `dotnet user-secrets` del progetto Web | env variable `Database__ConnectionString` sull'App Pool IIS | idem |
| **Chi crea il database** | DBA (manuale, una sola volta per ambiente) | DBA (manuale, una sola volta) | DBA (manuale, una sola volta) |
| **Chi crea/aggiorna lo schema** | app allo startup (`DbContext.Database.MigrateAsync`) | app allo startup (idem) | app allo startup (idem) |
| **Trigger deploy** | — | push su `develop` | push su `main` + approvazione manuale |
| **Dati** | finti, generati con Bogus | dataset realistico ma anonimizzato | dati reali (GDPR: accesso ristretto, backup cifrati) |
| **ASPNETCORE_ENVIRONMENT** | `Development` | `Staging` | `Production` |

Ogni ambiente ha una propria entry `Environment` su GitHub (`staging`, `production`)
con i suoi secrets e variables — vedi [sezione 5](#5-deploy-via-github-actions).

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
- Abilitare **TDE** (Transparent Data Encryption) sul database di produzione.
- Il **DBA crea manualmente** un database vuoto per ciascun ambiente (vedi sezione 3).
- Un **unico login SQL** per ambiente con ruolo `db_owner` (serve sia per creare/aggiornare lo schema all'avvio dell'app, sia a Hangfire per creare il proprio schema).
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

La responsabilità del database è **tutta sul DBA** (una volta per ambiente):

1. **Crea il database vuoto** con collation consigliata `Latin1_General_CI_AS`.
2. **Crea il login SQL Server** dedicato a quell'ambiente.
3. **Mappa** il login come `USER` del database con ruolo `db_owner`.

Esempio SSMS / `sqlcmd` per l'ambiente Staging (ripetere 1:1 cambiando i nomi per Dev
e Production):

```sql
-- 1. Database
CREATE DATABASE [PrimaNota_Staging] COLLATE Latin1_General_CI_AS;
ALTER DATABASE [PrimaNota_Staging] SET RECOVERY FULL;       -- Staging / Production
GO

-- 2. Login
CREATE LOGIN [primanota_app_staging]
    WITH PASSWORD = N'REDACTED_STRONG_PWD',
         DEFAULT_DATABASE = [PrimaNota_Staging],
         CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;
GO

-- 3. User + db_owner
USE [PrimaNota_Staging];
GO
CREATE USER [primanota_app_staging] FOR LOGIN [primanota_app_staging];
ALTER ROLE db_owner ADD MEMBER [primanota_app_staging];
GO
```

Valori consigliati:

| Ambiente | Database | Login |
|----------|----------|-------|
| Development | `PrimaNota_Dev` | `primanota_app_dev` |
| Staging | `PrimaNota_Staging` | `primanota_app_staging` |
| Production | `PrimaNota_Production` | `primanota_app_prod` |

### 3.1 Schema creato/aggiornato dall'app allo startup

Non occorre creare tabelle né applicare migration a mano. L'app, al primo avvio di
ogni ambiente, chiama `DbContext.Database.MigrateAsync()` in `Program.cs` →
`InitializeInfrastructureAsync()` e crea/aggiorna schema (`app`, `identity`) e tabelle.
Hangfire, a sua volta, crea autonomamente lo schema `hangfire` al primo startup — per
questo il login deve avere `db_owner` (non solo `ddl_admin` + `datareader` + `datawriter`).

### 3.2 Trade-off noto

Le migration con DDL lunga (es. creazione indice su tabella multi-milione di righe)
possono far aspettare il primo HTTP request dopo un deploy. Per un'app con carico
modesto è accettabile. Se in futuro serve disaccoppiare lo schema change dal restart,
si può applicare a mano uno script `.sql` pre-generato (gli script idempotenti sono
comunque committati in `deploy/sql/migrations/`, generati con
`dotnet ef migrations script --idempotent`) e disattivare `MigrateAsync` via una
configurazione a scelta.

---

## 4. Segreti e configurazione

### 4.1 Dove vivono le credenziali DB (riassunto per ambiente)

Le credenziali del database **non sono mai** in `appsettings.*.json`, né su GitHub, né nel
repo. Per ciascun ambiente vivono in un posto diverso:

| Ambiente | Posto | Chiave | Come si imposta |
|----------|-------|--------|------------------|
| **Development** | `dotnet user-secrets` del progetto `PrimaNota.Web` (file locale `~/.microsoft/usersecrets/prima-nota-web/secrets.json` su Linux/macOS, `%APPDATA%\Microsoft\UserSecrets\prima-nota-web\` su Windows) | `Database:ConnectionString` | `dotnet user-secrets set "Database:ConnectionString" "..."` |
| **Staging** | Environment variable dell'**App Pool IIS** `PrimaNota.Staging` sul server IIS di staging | `Database__ConnectionString` (doppio underscore → mappa a `Database:ConnectionString`) | `appcmd.exe` come Administrator (vedi §4.3) |
| **Production** | Environment variable dell'**App Pool IIS** `PrimaNota.Production` sul server IIS di produzione | `Database__ConnectionString` | `appcmd.exe` come Administrator |

Esempi di valore (sostituire con i tuoi dati reali):

```
Development
  Server=localhost,1433;Database=PrimaNota_Dev;User Id=primanota_app_dev;Password=...;Encrypt=True;TrustServerCertificate=True;

Staging
  Server=sql-staging.azienda.local,1433;Database=PrimaNota_Staging;User Id=primanota_app_staging;Password=...;Encrypt=True;TrustServerCertificate=False;

Production
  Server=sql.azienda.local,1433;Database=PrimaNota_Production;User Id=primanota_app_prod;Password=...;Encrypt=True;TrustServerCertificate=False;
```

> Regola d'oro: **la password del login DB non transita mai in GitHub**. Il workflow di
> CI/CD non ha bisogno delle credenziali database perché non tocca il DB (lo schema lo
> gestisce l'app allo startup).

### 4.2 Altre variabili di ambiente da impostare sull'App Pool IIS

Oltre a `Database__ConnectionString`, sui server di staging/produzione vanno impostate:

| Variabile | Obbligatoria | Descrizione |
|-----------|--------------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Sì | `Staging` o `Production` |
| `Database__ConnectionString` | Sì | Vedi §4.1 |
| `Authentication__Google__ClientId` | Se Google OAuth attivo | OAuth client id |
| `Authentication__Google__ClientSecret` | Se Google OAuth attivo | OAuth client secret |
| `Identity__Bootstrap__Email` | Solo al primo avvio | Email admin iniziale |
| `Identity__Bootstrap__Password` | Solo al primo avvio | Password admin (≥12 caratteri, strong) |
| `Identity__Bootstrap__FullName` | Solo al primo avvio | Nome admin iniziale |

### 4.3 Configurazione via appcmd (esempio completo)

Eseguire come Administrator sul server IIS, una sola volta per ciascun ambiente:

```powershell
$Pool = 'PrimaNota.Staging'
$Conn = 'Server=sql-staging.azienda.local,1433;Database=PrimaNota_Staging;User Id=primanota_app;Password=REDACTED;Encrypt=True;TrustServerCertificate=False;'
$appcmd = 'C:\Windows\System32\inetsrv\appcmd.exe'

# Helper
function Set-PoolEnv([string]$Name, [string]$Value) {
    & $appcmd set config -section:system.applicationHost/applicationPools `
        "/+[name='$Pool'].environmentVariables.[name='$Name',value='$Value']" /commit:apphost
}

Set-PoolEnv 'ASPNETCORE_ENVIRONMENT'                  'Staging'
Set-PoolEnv 'Database__ConnectionString'              $Conn
Set-PoolEnv 'Authentication__Google__ClientId'        'xxxxxxxx.apps.googleusercontent.com'
Set-PoolEnv 'Authentication__Google__ClientSecret'    'GOCSPX-xxxxxxxx'

# Solo al primissimo avvio: crea l'admin iniziale e poi rimuovi queste 3 righe.
Set-PoolEnv 'Identity__Bootstrap__Email'              'admin@azienda.it'
Set-PoolEnv 'Identity__Bootstrap__Password'           'ChangeMeStrongPwd2026!'
Set-PoolEnv 'Identity__Bootstrap__FullName'           'Amministratore'

Restart-WebAppPool -Name $Pool
```

Dopo il primo login del bootstrap admin, rimuovi le tre `Identity__Bootstrap__*` con:

```powershell
& $appcmd set config -section:system.applicationHost/applicationPools `
    "/-[name='$Pool'].environmentVariables.[name='Identity__Bootstrap__Email']" /commit:apphost
& $appcmd set config -section:system.applicationHost/applicationPools `
    "/-[name='$Pool'].environmentVariables.[name='Identity__Bootstrap__Password']" /commit:apphost
& $appcmd set config -section:system.applicationHost/applicationPools `
    "/-[name='$Pool'].environmentVariables.[name='Identity__Bootstrap__FullName']" /commit:apphost
Restart-WebAppPool -Name $Pool
```

> ⚠️ `Database__ConnectionString` resta sul server in chiaro (come ogni env var di app pool).
> Le ACL su `applicationHost.config` devono permetterne la lettura solo ad Administrator
> e al service account del pool. Le variabili non vengono MAI versionate nel repository.

Dopo aver popolato l'admin iniziale con successo, **rimuovere le variabili `Identity__Bootstrap__*`** e riavviare l'app pool.

---

## 5. Deploy via GitHub Actions

### Segreti e variabili da configurare (una tantum)

Su GitHub → Settings → Environments → `staging` (poi duplicare per `production`).

Il workflow **non tocca mai il database** (niente `sqlcmd`, niente migration): lo schema
è gestito dall'app allo startup. Quindi servono solo i segreti IIS/WebDeploy.

**Secrets (valori sensibili, mai visualizzati):**

| Nome | Esempio | Scopo |
|------|---------|-------|
| `STAGING_IIS_USER` | `DOMAIN\\deploy-svc` o `deploy@azienda` | Utente abilitato a WebDeploy (IIS Management Service) |
| `STAGING_IIS_PASS` | `••••••••••` | Password dell'utente WebDeploy |

**Variables (valori non sensibili, visibili nei log):**

| Nome | Esempio | Scopo |
|------|---------|-------|
| `STAGING_IIS_HOST` | `iis-staging.azienda.local` | Host IIS (porta 8172 per Management Service) |
| `STAGING_IIS_SITE` | `PrimaNota.Staging` | Nome del sito IIS creato da `app-pool-setup.ps1` |
| `STAGING_HOSTNAME` | `primanota-staging.azienda.local` | Hostname HTTPS per lo smoke test |

> Le credenziali del database (server, nome, login, password) **non** si mettono su GitHub:
> vivono solo come env variable `Database__ConnectionString` sull'App Pool IIS (vedi
> [sezione 4](#4-segreti-e-configurazione)).

Se `STAGING_IIS_HOST` non è impostato, il job `deploy` viene saltato (il workflow produce comunque l'artefatto).

### Trigger

- Automatico su `push` al branch **`develop`**.
- Manuale da **Actions → Deploy · Staging → Run workflow**.

### Flusso del workflow

1. `publish`: build Release, `dotnet publish` con runtime `win-x64`, copia di `web.config`, upload artefatto.
2. `deploy`: deploy via `msdeploy.exe`, smoke test con retry su `/health` (l'app al primo
   request dopo deploy applica le migration, quindi il retry gestisce il tempo di schema upgrade).

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

### Database locale (Development)

Il DB di sviluppo si chiama **`PrimaNota_Dev`** ed è completamente separato da Staging/Production.

**Opzione A — SQL Server in Docker (consigliata, cross-platform):**

```bash
docker run --name mssql-primanota -d \
  -e ACCEPT_EULA=Y \
  -e MSSQL_SA_PASSWORD='Strong_Password_123!' \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest
```

Il database `PrimaNota_Dev` viene creato automaticamente dalla app al primo avvio
(via `DbContext.Database.MigrateAsync()`), oppure puoi provisioningarlo manualmente
con lo script `deploy/sql/provision-environment.sql` (vedi sezione 3).

**Opzione B — SQL Server LocalDB (solo Windows):** la connection string di default
in `appsettings.json` punta già a `(localdb)\\mssqllocaldb;Database=PrimaNota_Dev`.

### Secrets locali

```bash
cd src/PrimaNota.Web
dotnet user-secrets init   # solo al primo setup

# Se usi Docker (Opzione A), imposta la connection string:
dotnet user-secrets set "Database:ConnectionString" \
  "Server=localhost,1433;Database=PrimaNota_Dev;User Id=sa;Password=Strong_Password_123!;TrustServerCertificate=True;"

# Bootstrap admin per il primo login (rimuovilo dopo il primo avvio):
dotnet user-secrets set "Identity:Bootstrap:Email"    "admin@local"
dotnet user-secrets set "Identity:Bootstrap:Password" "Admin_Password_123!"
dotnet user-secrets set "Identity:Bootstrap:FullName" "Administrator"

# (Opzionale) Credenziali Google OAuth per testare il flusso esterno:
dotnet user-secrets set "Authentication:Google:ClientId"     "xxxx.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "GOCSPX-xxxx"
```

I secrets di development vivono in `~/.microsoft/usersecrets/prima-nota-web/` (Linux/macOS)
o `%APPDATA%\Microsoft\UserSecrets\prima-nota-web\` (Windows) — **mai nel repo**.

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
