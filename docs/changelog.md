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

### Bank-statement import — CSV connectors

- **Connector architecture** for bank-statement import: new `IBankStatementConnector` abstraction (one per institute/format) plus `EstratoContoParserDispatcher` that routes a file to the right connector either by explicit selection (manual override) or by content-based auto-detection. Adding a new bank is a single connector class + one DI registration.
- `IEstratoContoParser` now exposes `AvailableConnectors` and accepts an optional `connectorId`; new `BankConnectorInfo` record and `ListBankConnectors` query expose the connector list to the UI.
- `BancoPostaCsvConnector`: parses the Poste Italiane "BancoPosta — Saldo e Movimenti" CSV export. Auto-detected delimiter (tab / semicolon), `it-IT` amounts, BOM-tolerant; columns mapped by header name (resilient to reordering). Period read from `FILTRI DI RICERCA` (fallback to movement date span), balance from `RIEPILOGO`; credit (Accredito) mapped positive, debit (Addebito) negative.
- UI (`/estratto-conto`): CSV is now the default import format (`Accept=".csv"`, "Carica CSV"); a new **Istituto** selector offers "Auto-rileva" plus the registered connectors and forwards the chosen `ConnectorId`.
- 10 unit tests covering detection, movement count, period, balance, sign consistency and semicolon-delimiter support.

### Changed

- CSV is now the default (and only) bank-statement import format.

### Removed

- Legacy PDF bank-statement parser (`PdfEstratoContoParser`, `BancoPostaParser`) and the `PdfPig` dependency, superseded by the CSV connector architecture.

### Reconciliation — memorized classification rules

- New `RegolaRiconciliazione` aggregate: when a user generates a movement from a bank-statement row, the chosen classification (causale, categoria, anagrafica, aliquota IVA, conto destinazione) is stored against a deterministic `RegolaSignature` of the row — bank cause code + operation name + a normalized description fragment — scoped per financial account.
- `RegolaSignature` normalizer strips volatile parts of the description (amounts, dates, IBAN/TRN/distinta codes, month names) so two rows for the same counterparty collapse to one key; recurring fees (whose only variable part is the month) collapse to an empty description key and match on cause + operation alone.
- On the next matching row, `GeneraMovimentoDaRiga` upserts the rule (latest choice wins, usage counter incremented). New `GetRegolaSuggerita` query returns the memorized classification (exact match, else a generic cause+operation fallback).
- UI: the "Genera nuovo" tab of the reconciliation dialog pre-fills the fields from the matched rule and shows a "suggested" badge with the usage count; the user always confirms — nothing is registered automatically.
- Persistence: `RegoleRiconciliazione` table with a unique index on (account + signature); migration `AddRegoleRiconciliazione` + idempotent SQL `014`.
- 14 unit tests covering the signature normalizer and the suggestion handler (exact match, generic fallback, per-account isolation, no-match).

### Admin user management + email two-factor authentication

- **User management** page at `/admin/utenti` (Admin only): list users with roles, status, 2FA and last login; create users (name, email, roles, initial password, email auto-confirmed), edit (name, roles, active), reset password, activate/deactivate (with a guard against disabling your own account). Built directly on `UserManager`/`RoleManager`; actions written to the audit log.
- **Email infrastructure**: new `IEmailSender` abstraction + MailKit-based `SmtpEmailSender` with `SmtpOptions` (Enabled/Host/Port/credentials/From/StartTls) bound from the `Smtp` appsettings section.
- **Email two-factor authentication**: a per-user toggle in the management page enables 2FA via an email code (enabling also confirms the email so the Email token provider is valid). The login flow now detects `RequiresTwoFactor`, emails a code and routes to a new `/Account/TwoFactor` verification page (with resend); registered the `TwoFactorUserId`/`TwoFactorRememberMe` cookie schemes required by the two-step `SignInManager` flow.
- 4 unit tests for the SMTP sender configuration/guard logic.

### Electronic invoice import from Aruba

- **Provider-agnostic abstraction** `IFatturaProvider` (list + download) with `ArubaFatturaProvider`, an adapter for the Aruba "Fatturazione Elettronica" REST API v2: OAuth2 password-grant token caching, the API's 2-day date-window limit handled by automatic chunking with throttling (12 req/min cap), pagination, and CAdES (`.p7m`) unwrapping via BouncyCastle. Downloaded XML is fed to the existing `FatturaElettronicaParser`/`ImportFatturaElettronica` pipeline.
- **Admin config** at `/admin/integrazione-aruba`: enable/disable, username, password (stored encrypted via ASP.NET Core Data Protection through the new `ISecretProtector`), demo/production toggle. New `IntegrazioneAruba` singleton aggregate + table.
- **Import UI** at `/import-fatture` (Contabile): pick direction (active/passive), date range and account, search remote invoices (with an "already imported" badge), select and bulk-import them.
- **Dedup**: `MovimentoPrimaNota` gains `IdentificativoSdi` (the SdI id, indexed); `ListFattureRemote` flags already-imported invoices and `ImportFattureRemote` skips them.
- Persistence: migration `AddIntegrazioneArubaAndIdSdi` + idempotent SQL `015`. Dependencies: MailKit (added earlier) and `BouncyCastle.Cryptography`.
- 8 unit tests covering the `.p7m`/CAdES extraction and the 2-day date-window splitting.
