# Specifiche Tecniche — Prima Nota Aziendale

> Registro centralizzato di tutte le dipendenze del progetto. Aggiornato contestualmente
> ad ogni aggiunta/aggiornamento/rimozione di pacchetti. Vedi DIR-015.

## Runtime

- Linguaggio: **C# 14**
- Runtime: **.NET 10.0** (LTS, supporto fino a novembre 2028)
- Package Manager: **NuGet** con **Central Package Management** (`Directory.Packages.props`) e lock file (`packages.lock.json` per ogni progetto)
- SDK pinnato in `global.json`: **10.0.201** (rollForward `latestFeature`)
- Database: **SQL Server 2022** (produzione); **Testcontainers MsSql 2022** (test integration)
- Web server: **IIS 10+** (Windows Server 2022+) con ASP.NET Core Hosting Bundle

## Schemi DB in uso

- `app` — entità di dominio, `__EFMigrationsHistory`, `Esercizi`, `AuditLog`
- `identity` — tabelle ASP.NET Core Identity (`AspNetUsers`, `AspNetRoles`, ecc.)
- `hangfire` — tabelle Hangfire (create automaticamente da `PrepareSchemaIfNecessary` al primo avvio)

## Migrations applicate

| Ordine | MigrationId | Contenuto | Script idempotente |
|--------|-------------|-----------|---------------------|
| 1 | `Initial` | Solo `app.__EFMigrationsHistory` | `deploy/sql/migrations/001_Initial.sql` |
| 2 | `AddIdentity` | Tabelle Identity nello schema `identity` | `deploy/sql/migrations/002_AddIdentity.sql` |
| 3 | `AddEsercizi` | `app.Esercizi` con indice unico su Anno | `deploy/sql/migrations/003_AddEsercizi.sql` |
| 4 | `AddAuditLog` | `app.AuditLog` con indici su OccurredAt, (UserId, OccurredAt), Kind | `deploy/sql/migrations/004_AddAuditLog.sql` |

## Dipendenze Principali (production)

### Web host (`PrimaNota.Web`)

| Pacchetto | Versione | Scopo | Licenza |
|-----------|----------|-------|---------|
| AspNetCore.HealthChecks.SqlServer | 9.0.0 | Probe SQL Server su `/health/ready` | Apache-2.0 |
| Microsoft.AspNetCore.Authentication.Google | 10.0.0 | OAuth Google 2.0 | MIT |
| Microsoft.EntityFrameworkCore.Design | 10.0.0 | Tooling migrations (PrivateAssets=all) | MIT |
| MudBlazor | 8.10.0 | Component library UI responsive | MIT |
| Serilog.AspNetCore | 9.0.0 | Integrazione Serilog + ASP.NET Core, request logging | Apache-2.0 |
| Serilog.Settings.Configuration | 9.0.0 | Configurazione Serilog da `appsettings.json` | Apache-2.0 |
| Serilog.Sinks.Console | 6.0.0 | Sink console | Apache-2.0 |
| Serilog.Sinks.File | 6.0.0 | Sink file con rolling giornaliero | Apache-2.0 |
| Serilog.Enrichers.Environment | 3.0.1 | MachineName, EnvironmentName | Apache-2.0 |
| Serilog.Enrichers.Thread | 4.0.0 | ThreadId | Apache-2.0 |
| Serilog.Exceptions | 8.4.0 | Destrutturazione exception | MIT |

### Infrastructure (`PrimaNota.Infrastructure`)

| Pacchetto | Versione | Scopo | Licenza |
|-----------|----------|-------|---------|
| AspNetCore.HealthChecks.SqlServer | 9.0.0 | SQL Server health probe | Apache-2.0 |
| Hangfire.AspNetCore | 1.8.21 | Dashboard + integrazione ASP.NET Core | LGPL-3.0 con linking exception |
| Hangfire.NetCore | 1.8.21 | DI integration | LGPL-3.0 con linking exception |
| Hangfire.SqlServer | 1.8.21 | Storage SQL Server schema `hangfire` | LGPL-3.0 con linking exception |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.0 | Identity store su EF Core | MIT |
| Microsoft.EntityFrameworkCore | 10.0.0 | ORM core | MIT |
| Microsoft.EntityFrameworkCore.Relational | 10.0.0 | Relational core (migrations) | MIT |
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.0 | Provider SQL Server | MIT |
| Newtonsoft.Json | 13.0.3 | **Pin esplicito** per override della transitiva Hangfire 11.0.1 vulnerabile (GHSA-5crp-9r3c-p9vr) | MIT |

FrameworkReference: `Microsoft.AspNetCore.App` (porta `IHttpContextAccessor`, `IHealthChecksBuilder`, Options.DataAnnotations, ecc.).

### Application (`PrimaNota.Application`)

| Pacchetto | Versione | Scopo | Licenza |
|-----------|----------|-------|---------|
| Microsoft.EntityFrameworkCore | 10.0.0 | Richiesto da `IApplicationDbContext` (espone `DatabaseFacade`) | MIT |

### Domain & Shared

Nessuna dipendenza esterna. Solo BCL di .NET 10.

## Dipendenze di Sviluppo (dev)

### Analyzer (src/ only — disabilitati nei progetti test)

| Pacchetto | Versione | Scopo |
|-----------|----------|-------|
| StyleCop.Analyzers | 1.2.0-beta.556 | Stile .NET |
| SonarAnalyzer.CSharp | 10.4.0.108396 | Quality gate |

### Tool locali

| Tool | Versione | Manifest |
|------|----------|----------|
| `dotnet-ef` | 10.0.0 | `.config/dotnet-tools.json` |

### Test projects

| Pacchetto | Versione | Progetti |
|-----------|----------|----------|
| Microsoft.NET.Test.Sdk | 17.14.1 | Tutti |
| xunit | 2.9.3 | Tutti |
| xunit.runner.visualstudio | 3.1.4 | Tutti |
| coverlet.collector | 6.0.4 | Tutti |
| FluentAssertions | 6.12.2 | Tutti |
| NSubstitute | 5.3.0 | Unit |
| AutoFixture + AutoFixture.Xunit2 | 4.18.1 | Unit |
| Bogus | 35.6.1 | Unit |
| Microsoft.EntityFrameworkCore.InMemory | 10.0.0 | Unit |
| Testcontainers.MsSql | 4.1.0 | Integration |
| bunit | 1.34.0 | Component |
| Microsoft.Playwright | 1.48.0 | E2E |

## Vincoli di Compatibilita

- **Central Package Management**: nessun `PackageReference` deve specificare `Version`; le versioni vivono solo in `Directory.Packages.props`.
- **`TreatWarningsAsErrors = true`**: ogni warning blocca la build. Le eccezioni sono codificate per regola in `.editorconfig`.
- **Analyzer non applicati ai progetti test** (`IsTestProject=true`) per evitare falsi positivi su pattern xUnit (`X_Y_Z` naming, `*Collection` class).
- **Blazor Server** richiede WebSocket Protocol abilitato su IIS.
- **EF Core 10** compatibile con SQL Server 2022; SQL Server 2019 minimo, SQL 2017 non supportato.
- **Hangfire 1.8.x** porta `Newtonsoft.Json 11.0.1` come transitiva, con CVE GHSA-5crp-9r3c-p9vr (severità High). Pin esplicito a 13.0.3 in `Directory.Packages.props`. Rimuovere il pin quando Hangfire aggiorna la sua dipendenza.
- **Google OAuth** è opzionale: l'app avvia anche senza credenziali configurate (la sezione `Authentication:Google` può essere assente in dev).
- **Tesseract.NET** (modulo 12) richiederà `tessdata` con lingua `ita` copiato nell'output path.
- **QuestPDF** (modulo 15) licenza Community gratuita sotto soglia fatturato 1M€.

## Dipendenze Deprecate / Da Sostituire

| Pacchetto | Motivo | Sostituto proposto | Priorita |
|-----------|--------|-------------------|----------|
| — | — | — | — |

## Dipendenze Previste (ancora da aggiungere)

| Modulo / Task | Pacchetto |
|---------------|-----------|
| Modulo 05 Mediator | MediatR |
| Modulo 05 Validation | FluentValidation |
| Modulo 05 Mapping | Mapster |
| Modulo 09 Import | CsvHelper, ClosedXML, PdfPig |
| Modulo 12 OCR | Tesseract |
| Modulo 15 Reporting | QuestPDF |
