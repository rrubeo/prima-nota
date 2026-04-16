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
