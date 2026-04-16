# Changelog — Prima Nota Aziendale

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Project specification approved (v1.1) with all modules, architecture, stack and risks defined
- Implementation plan with Phase 1 decomposed into 14 atomic tasks
- .NET 10 solution scaffold with Clean Architecture layout (Shared, Domain, Application, Infrastructure, Web)
- Central Package Management via `Directory.Packages.props` and `Directory.Build.props` with strict analyzers (StyleCop, SonarAnalyzer) and `TreatWarningsAsErrors`
- Blazor Server empty app with interactive server components
- Test projects skeleton: Unit, Integration, Component (bUnit), E2E (Playwright)
- `.editorconfig` and `stylecop.json` enforcing .NET coding conventions
- `.gitignore` covering .NET, secrets, OS, tooling
- D.O.E. `commit-msg` git hook that strips AI attribution (portable: GNU sed + BSD sed)
- Serilog structured logging (console + daily rolling file) with request logging middleware
- `/health` and `/health/ready` endpoints, with SQL Server probe on the ready endpoint
- EF Core 10 wired up: `AppDbContext` on schema `app`, `DatabaseOptions` bound from config, retry-on-failure
- `AuditSaveChangesInterceptor` populating `CreatedAt/By` and `UpdatedAt/By` on entities implementing `IAuditable`
- `AuditableEntity<TId>`, `IEntity<TId>`, `IAuditable`, `IDateTimeProvider`, `ICurrentUserService`, `IApplicationDbContext` abstractions
- Initial EF migration (creates `app.__EFMigrationsHistory`) and idempotent deploy script at `deploy/sql/migrations/001_Initial.sql`
- Local tool manifest with `dotnet-ef` 10.0.0
- Testcontainers MsSql fixture and integration test validating migration applies to real SQL Server 2022
- Unit tests covering audit interceptor on Added/Modified/anonymous scenarios
- `docs/tech-specs.md` populated with the full dependency registry
- ASP.NET Core Identity with `ApplicationUser` (FullName, IsActive, LastLoginAt) and `ApplicationRole` (Description) on `identity` schema; roles seeding (Admin, Contabile, Dipendente) and bootstrap admin provisioning via env vars
- Authentication: cookie-based auth, Google OAuth 2.0 (optional, activated when credentials provided), `/Account/Login`, `/Account/Logout`, `/Account/AccessDenied`, `/Account/ExternalLogin`, `/Account/ExternalCallback` endpoints; external logins allowed only for pre-provisioned active users
- Authorization policies: `RequireAdmin`, `RequireContabile`, `RequireAuthenticated`
- MudBlazor UI: `MainLayout` responsive with AppBar, drawer, year badge, user menu; `NavMenu` with role-aware items; `RedirectToLogin`; dashboard home page with placeholder KPI cards
- Hangfire on SQL Server (`hangfire` schema), dashboard at `/hangfire` protected by Admin role
- `EsercizioContabile` aggregate (solar year, Aperto/InChiusura/Chiuso states), `EsercizioRegistrationService` (idempotent ensure), `IEsercizioContext` (cookie-persisted year switcher), `EsercizioYearlyJob` (cron `5 0 1 1 *` Europe/Rome)
- Audit log: `AuditLogEntry` append-only domain entity, `IAuditLogger` application contract, `AuditLogger` EF-backed implementation with TraceId/IP capture; login/logout/login-failed/external-login events wired
- Migrations: `AddIdentity`, `AddEsercizi`, `AddAuditLog` with idempotent SQL scripts in `deploy/sql/migrations/`
- Pinned Newtonsoft.Json 13.0.3 via Central Package Management to override transitive Hangfire resolution (CVE GHSA-5crp-9r3c-p9vr)
- GitHub Actions CI workflow: build + format + unit/component/integration tests, vulnerability scan (`dotnet list --vulnerable`), CodeQL static analysis
- Dependabot configuration for NuGet and GitHub Actions (weekly, grouped minor+patch)
- Deploy pipeline skeleton for IIS staging: `deploy-staging.yml` (publish → migrations via `sqlcmd` → `msdeploy` → smoke test), `deploy/iis/web.config` with security headers and WebSocket support, `deploy/iis/app-pool-setup.ps1` idempotent provisioning
- `appsettings.Staging.json` and `appsettings.Production.json`
- `docs/deployment.md` covering prerequisites, IIS setup, DB provisioning, secrets, workflow usage, rollback and local dev

### Phase 2 — Domain base

- Application layer bootstrap: MediatR 12.4.1 + FluentValidation 11.11.0 + Mapster 7.4.0 via `AddApplication()` assembly-scan registration
- **Module 04 — Anagrafiche**: unified `Anagrafica` aggregate with Cliente / Fornitore / Dipendente role flags (non-exclusive), `Contatti` and `Indirizzo` owned value objects, CQRS (Create/Update/ToggleActivation/Get/List with role filter + search), `/anagrafiche`, `/anagrafiche/{clienti|fornitori|dipendenti}`, `/anagrafiche/{id}` Blazor pages with MudForm + FluentValidation
- **Module 05 — Piano conti & Causali & Aliquote IVA**: `Categoria` (flat, Entrata/Uscita), `Causale` (TipoMovimento enum: Incasso/Pagamento/GirocontoInterno/StipendioNetto/F24/RimborsoNotaSpese, optional default Categoria), `AliquotaIva` (TipoIva: Ordinaria/Esente/NonImponibile/FuoriCampo/ReverseCharge with "natura" code N1..N7)
- `MasterDataSeeder` provisioning 14 Italian categories, 10 Italian VAT rates (22/10/5/4/0/Esente art.10/Non imponibile art.8/9/Fuori campo/Reverse charge), 11 common causali — idempotent at startup
- `/piano-conti`, `/causali`, `/aliquote-iva` pages with MudTable + MudDialog for inline create/edit
- **Module 06 — Conti finanziari**: `ContoFinanziario` aggregate with `TipoConto` (Cassa/Banca/CartaDiCredito/CartaDebitoPrepagata), bank fields (Istituto/IBAN/BIC), card fields (Intestatario/Ultime4Cifre), `SaldoIniziale` + `DataSaldoIniziale` as anchor for future balance computation
- `/conti-finanziari` page with MudTable, conditional fields per `TipoConto`, per-type balance aggregation cards (Cassa, Banche, Carte)
- Migrations `AddAnagrafiche`, `AddPianoContiCausaliIva`, `AddContiFinanziari` with idempotent SQL scripts (005, 006, 007)
- `IApplicationDbContext` exposes `DbSet` for Anagrafiche, Categorie, Causali, AliquoteIva, ContiFinanziari
- `.editorconfig` adjustments: SA1402/SA1649 disabled in Application (MediatR convention), SA1204 and S1135 disabled globally (opinionated ordering / forward-reference TODOs)

### Phase 3 — Core Prima Nota (Module 07)

- `MovimentoPrimaNota` aggregate with owned collections `RigaMovimento` (split) and `Allegato` (attachments); signed-amount lines, invariants (≥ 1 line, giroconto balance = 0, date belongs to fiscal year, confirmed/reconciled non-editable)
- State machine `StatoMovimento`: Draft → Confirmed → Reconciled with transitions
- Rowversion concurrency token on the aggregate
- `IAttachmentStorage` + `FileSystemAttachmentStorage` (streamed SHA-256, traversal-safe, 20 MB default cap)
- CQRS: Create/Update/Confirm/Unconfirm/Delete + List (filters stato/conto/categoria/anagrafica/causale/data/importo/search) + Get; UploadAllegato/GetAllegatoContent/DeleteAllegato
- `GetSaldoConto` and `GetDashboardStats` queries
- Minimal API `/allegati/{id}` (download) and `/allegati/{movimentoId}/upload`
- `/movimenti` virtualised list + `/movimenti/{id}` edit page with dynamic lines editor and attachments panel
- Home dashboard binds to real KPI (saldo totale/per-type, entrate-uscite mese, movimenti YTD + bozze); recenti panel
- `ListContiFinanziariHandler` now computes real SaldoCorrente
- Migration `AddMovimentiPrimaNota` + `deploy/sql/migrations/008_AddMovimentiPrimaNota.sql`
- 15 new unit tests on MovimentoPrimaNota (18 total green)
