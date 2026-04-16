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

### Phase 4 — Gestione IVA (Module 08)

- Domain: `RegimeIva` enum (Ordinario / Forfettario), `PeriodicitaIva` (Mensile / Trimestrale), `TipoRegistroIva` (Vendite / Acquisti / Corrispettivi)
- `EsercizioContabile.ConfiguraIva(regime, periodicita, coefficienteRedditivita)` invariant: Forfettario requires 0-100% coefficient; closed exercises cannot be reconfigured
- `IvaScorporo` pure helper: `Scorpora(lordo, %)` returns `(imponibile, imposta)` with banker's rounding; sign preserved on negative amounts
- Migration `AddEsercizioIvaConfig` adds `RegimeIva`, `PeriodicitaIva`, `CoefficienteRedditivitaForfettario` columns (defaults Ordinario/Trimestrale for existing rows) + idempotent SQL `009_AddEsercizioIvaConfig.sql`
- Application: `IvaPeriodo` record (supports monthly and quarterly, localised label), `GetRegistroIva` query (derives register from causale kind + category nature), `GetLiquidazioneIva` query (debito, credito totale/indetraibile/detraibile, saldo periodo, credito riportato dai periodi precedenti, saldo finale); `GetEsercizioIvaConfig` / `UpdateEsercizioIvaConfig` commands
- UI: `/iva/registri` with period selector + tabs Vendite/Acquisti/Corrispettivi showing rows with controparte, P.IVA/CF, aliquota chip, imponibile/imposta/totale and footer totals; `/iva/liquidazione` with three KPI cards (IVA debito, IVA credito detraibile, saldo finale a debito/credito); Forfettario shows an informational alert and skips the reports
- UI: `/admin/esercizi` page for administrators to configure the regime/periodicity/coefficient per fiscal year
- NavMenu: Esercizi link now active under Amministrazione; Utenti link marked as "(modulo 16)"
- 14 new unit tests (scorporo IVA standard/negative/zero/rounding + EsercizioContabile.ConfiguraIva invariants); total 32 green
- ADR-0001 `docs/adr/0001-regole-iva-implementate.md` documenting the scope choices, calculation formulas and known limitations (e.g. no opening VAT credit from prior year, no official printable registers)

### Phase 4.1 — IVA per cassa + parametri aziendali + pagamenti parziali

- Domain: new `EsigibilitaIva` enum (Immediata / Cassa) and new singleton aggregate `ConfigurazioneAzienda` (denominazione, P.IVA, CF, indirizzo, contatti, `EsigibilitaIvaPredefinita`) with `UpdateIdentificazione` / `UpdateIndirizzo` / `SetEsigibilitaIva` behaviours and fixed primary key (`SingletonId = 1`) — the application always has exactly one configuration row, seeded on first run
- `MovimentoPrimaNota` now carries `DataCompetenza` (VAT competence date, defaults to `Data`, settable via `SetDataCompetenza` to support XML imports where data documento ≠ data registrazione)
- `MovimentoPrimaNota` gains a `Pagamenti` owned collection of `PagamentoMovimento` (Data, Importo positive, ContoFinanziarioId, Note) to support partial payments and advances; derived properties `TotalePagato`, `Residuo`, `IsFullyPaid` (0,01 € tolerance) and a derived `DataPagamento` (= max(pagamenti.Data) only when fully paid); `AddPagamento` rejects over-payment and is forbidden on Reconciled state
- `RegistroIva` query now filters and orders by `DataCompetenza` (semantically correct for Italian VAT registers)
- `GetLiquidazioneIva` reads the company exigibility from `ConfigurazioneAzienda` and switches: Immediata sums VAT from the register by DataCompetenza; Cassa iterates invoice pagamenti in the period and computes pro-quota VAT via the ratio `Importo / |Totale fattura|`. The DTO now also returns the applied `Esigibilita`.
- Infrastructure: `ConfigurazioneAziendaConfiguration` (owned `Indirizzo`, string-converted enum), `MovimentoPrimaNotaConfiguration` updated with `DataCompetenza` and the new `PagamentiMovimento` table (indices on Data, ContoFinanziarioId, MovimentoId); migration `AddConfigurazioneAziendaAndPagamenti` + idempotent SQL `010`, including a backfill `UPDATE` that initialises `DataCompetenza = Data` on pre-existing rows
- `MasterDataSeeder` provisions the default `ConfigurazioneAzienda` row on first startup
- UI: liquidation page displays an exigibility chip (Immediata / Cassa)
- 5 new unit tests covering pagamenti partial/full, over-payment rejection and reconciled-state guards (40 total green)
- Application CQRS: `GetConfigurazioneAzienda`/`UpdateConfigurazioneAzienda` (with FluentValidation); `AddPagamentoMovimento`/`RemovePagamentoMovimento`; `GetSchedaAnagrafica` that assembles the customer / supplier ledger (invoices + pagamenti + running balance, Dare/Avere split driven by causale kind). `MovimentoInput` carries `DataCompetenza`; `MovimentoDto` surfaces DataCompetenza, Pagamenti[], Totale/TotalePagato/Residuo/IsFullyPaid/DataPagamento; `MovimentoListItemDto` adds Residuo and IsFullyPaid for list badges
- UI: `/admin/azienda` (admin-only) form for the singleton company configuration; Amministrazione nav entry added. Movement edit page now has a DataCompetenza IVA datepicker and a Pagamenti / incassi panel (add form + table + residuo chip), forbidden only on Reconciled movements. New `/anagrafiche/{id}/scheda` page with KPI cards (Dare/Avere/Saldo) + chronological ledger rows; a Receipt icon in the anagrafica list opens it directly
- Extracted the pro-quota Cassa math into the pure helper `LiquidazioneProQuotaCalculator` so the VAT liquidation handler delegates the in-memory computation to a testable unit. 12 new unit tests covering acconti, saldi, multi-aliquote, indetraibile %, over-payment cap and banker's rounding (52 total green)
- `GetRegistroIva` now switches on `ConfigurazioneAzienda.EsigibilitaIvaPredefinita`. Under **Immediata** the register is still filtered by `DataCompetenza` (unchanged). Under **Cassa** the handler emits one row per (riga × pagamento-in-periodo) with pro-quota imponibile/imposta and `Data = pagamento.Data`; movements without explicit Pagamenti[] (cash sales / corrispettivi registered as a single transaction) fall back to their registration date with full amount. The same fallback is now applied in `GetLiquidazioneIva` so cash-basis movements without Pagamenti contribute correctly to the periodo under Cassa
