# Implementation Plan — Prima Nota Aziendale

> **Status:** IN REVISIONE
> **Versione:** 1.0
> **Data creazione:** 2026-04-16
> **Basato su:** `docs/project-spec.md` v1.1 (APPROVATA)
> **Riferimenti:** DIR-L2-01 (Decision Engine), DIR-L2-02 (Task Decomposition)

---

## Output del Decision Engine

| Passo | Valutazione | Decisione |
|-------|-------------|-----------|
| [1] Richiesta chiara? | SI — specifica approvata v1.1 | Procedo |
| [2] Direttive applicabili? | SI — DIR-001 (Intake, gia eseguita), DIR-002 (Architecture Patterns → Monolite modulare feature-based), DIR-015 (Dependency Management), L3 tutti | Applico |
| [3] Task atomico o composto? | **COMPOSTO** — 17 moduli, multiple dipendenze, piu fasi logiche | Attivo Task Decomposition |
| [4] Impatto irreversibile? | NO in Fase 1 (solo creazione progetto); SI in fasi successive (deploy, migrations) | Checkpoint dedicati pre-deploy |
| [5] Risorse a pagamento? | NO in MVP core; SI per OpenRouter (modulo 13) | Rate limiting + budget cap (Rischio R3) |

---

## Strategia di Decomposizione

**Approccio:** Pianificazione **dettagliata solo per la Fase 1** (Fondazioni). Le fasi successive (2-9) sono delineate ad alto livello e verranno decomposte in task atomici solo al momento giusto, per evitare sovra-pianificazione e incorporare l'apprendimento dalle fasi precedenti.

**Checkpoint base:** dopo ogni modulo completato + dopo ogni 5 task + pre-deploy staging + pre-deploy produzione.

---

## Fase 1 — Fondazioni (sprint 1-2, 2 settimane)

Obiettivo: progetto buildabile, deployabile su IIS locale, con autenticazione funzionante e pipeline CI verde.

### TASK-001 — Bootstrap soluzione .NET

- **Modulo:** 01 Core Infrastructure
- **Dipende da:** nessuno
- **Complessita:** S
- **Descrizione:** Creare la solution `PrimaNota.sln` con i progetti definiti in spec §7, `global.json` pin a SDK .NET 10, `Directory.Build.props` con `TreatWarningsAsErrors`, `Nullable`, analyzer (StyleCop, SonarAnalyzer), `Directory.Packages.props` per Central Package Management.
- **Output:** solution + 7 progetti csproj compilabili (anche vuoti).
- **Acceptance:**
  - [ ] `dotnet build` verde
  - [ ] `dotnet format --verify-no-changes` verde
  - [ ] Versione .NET SDK pinnata via `global.json`
- **Test:** build CI.

### TASK-002 — Setup repository: git hooks, .gitignore, CLAUDE.md in gitignore

- **Modulo:** 01
- **Dipende da:** TASK-001
- **Complessita:** S
- **Descrizione:** Eseguire `install-doe.sh hooks` e `scaffold`, verificare che `CLAUDE.md` sia in `.gitignore`, `.gitignore` .NET-appropriato, commit hook strip-AI-attribution installato.
- **Output:** `.gitignore`, `.git/hooks/commit-msg` attivo.
- **Acceptance:**
  - [ ] Commit test con trailer "Co-Authored-By: Claude" viene strippato
  - [ ] `CLAUDE.md` non tracciato
- **Test:** creazione commit di test e ispezione messaggio.

### TASK-003 — Configurazione Serilog e health check

- **Modulo:** 01
- **Dipende da:** TASK-001
- **Complessita:** S
- **Descrizione:** Integrare Serilog con sink Console + File + SQL Server (tabella `Logs`), endpoint `/health` e `/health/ready` con `Microsoft.Extensions.Diagnostics.HealthChecks`.
- **Acceptance:**
  - [ ] Log strutturato con `RequestId`, `UserId`, `CorrelationId`
  - [ ] `/health` risponde 200 OK con JSON stato componenti
- **Test:** unit test su configurazione logger, integration test su `/health`.

### TASK-004 — Setup EF Core + DbContext + prima migration

- **Modulo:** 01
- **Dipende da:** TASK-003
- **Complessita:** M
- **Descrizione:** Creare `AppDbContext` in `PrimaNota.Infrastructure/Persistence`, configurare connection string via `IOptions`, migration iniziale (solo schema base, tabelle Identity vengono aggiunte in TASK-006). Abilitare audit interceptor base (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy).
- **Acceptance:**
  - [ ] `dotnet ef migrations add Initial` genera script SQL
  - [ ] Script SQL review-ato e committato in `deploy/sql/migrations/`
  - [ ] Database creato via script applica correttamente
- **Test:** integration test con Testcontainers SQL Server (prima infrastruttura test).

### TASK-005 — Setup Testcontainers e skeleton test projects

- **Modulo:** 01
- **Dipende da:** TASK-004
- **Complessita:** S
- **Descrizione:** Configurare Testcontainers in `PrimaNota.IntegrationTests` con fixture SQL Server condivisa, skeleton xUnit + bUnit + Playwright; `PrimaNota.UnitTests` con AutoFixture + NSubstitute.
- **Acceptance:**
  - [ ] `dotnet test` verde (0 test iniziali OK)
  - [ ] Smoke test integration che crea DB e applica migration
- **Test:** il test stesso.

**📍 CHECKPOINT 1 — dopo TASK-005:** verifica ambiente dev funzionante prima di procedere con Identity.

### TASK-006 — ASP.NET Core Identity + schema utenti

- **Modulo:** 02 Identity & Auth
- **Dipende da:** TASK-004
- **Complessita:** M
- **Descrizione:** Aggiungere Identity con EF Core store. Ruoli seed: `Admin`, `Contabile`, `Dipendente`. Entita `ApplicationUser` estesa con `FullName`, `IsActive`.
- **Acceptance:**
  - [ ] Migration applicata
  - [ ] Seed dei 3 ruoli al startup (idempotente)
  - [ ] Bootstrap admin via env variables (rimosso dopo primo run)

### TASK-007 — Login email/password + Google OAuth

- **Modulo:** 02
- **Dipende da:** TASK-006
- **Complessita:** M
- **Descrizione:** Pagina login Blazor, handler Google OAuth, gestione claim ruolo. Policy authorization `RequireContabile`, `RequireAdmin`, `RequireDipendente`.
- **Acceptance:**
  - [ ] Login email/password funzionante
  - [ ] Login Google OAuth funzionante (con fallback a email/password se Google non raggiungibile)
  - [ ] Redirect a pagina corretta per ruolo
- **Test:** integration test login flow, E2E Playwright per login Google (mock).

### TASK-008 — Layout base MudBlazor + tema + navigazione

- **Modulo:** UI trasversale
- **Dipende da:** TASK-001
- **Complessita:** M
- **Descrizione:** `MainLayout` con navbar, drawer laterale responsive, tema chiaro/scuro, menu filtrato per ruolo, switcher anno contabile in header.
- **Acceptance:**
  - [ ] Layout responsive (desktop, tablet 768px, mobile 375px)
  - [ ] Menu visibili in base al ruolo (test con utente fake per ciascun ruolo)
- **Test:** bUnit per rendering, Playwright per responsive viewport.

### TASK-009 — Modulo Esercizi Contabili

- **Modulo:** 03 Esercizi
- **Dipende da:** TASK-004, TASK-008
- **Complessita:** S
- **Descrizione:** Entita `EsercizioContabile` (Anno int PK, DataInizio 01/01, DataFine 31/12, Stato: `Aperto` | `InChiusura` | `Chiuso`). Service per esercizio corrente (auto-determinato da data di sistema), auto-creazione il 1 gennaio via Hangfire. Filtro anno presente in `IEsercizioContext` scoped.
- **Acceptance:**
  - [ ] Switching anno da header propaga a tutti i moduli
  - [ ] Job Hangfire schedulato per 01/01 00:05 crea nuovo esercizio
  - [ ] Test: simulazione cambio data sistema → auto-creazione
- **Test:** unit test su `EsercizioContext`, integration test su job Hangfire.

### TASK-010 — Audit trail base (trasversale)

- **Modulo:** 17 Audit & Sicurezza (baseline)
- **Dipende da:** TASK-004
- **Complessita:** S
- **Descrizione:** Interceptor EF Core popola `CreatedAt/By`, `UpdatedAt/By` da `ICurrentUserService`. Tabella `AuditLog` per log operazioni sensibili (login, logout, modifiche movimenti) — il modulo completo 17 arriva dopo, questa e solo la baseline.
- **Acceptance:**
  - [ ] Ogni INSERT/UPDATE popola i campi audit automaticamente
  - [ ] `AuditLog` riceve eventi login/logout

### TASK-011 — Hangfire setup

- **Modulo:** 01
- **Dipende da:** TASK-004
- **Complessita:** S
- **Descrizione:** Registrare Hangfire con SQL Server storage (schema `hangfire` separato), dashboard protetta da ruolo `Admin`.
- **Acceptance:**
  - [ ] `/hangfire` accessibile solo da admin autenticato
  - [ ] Job dummy recurring visibile in dashboard
- **Test:** integration test job esecuzione.

### TASK-012 — Pipeline CI/CD (GitHub Actions) — build + test

- **Modulo:** DevOps
- **Dipende da:** TASK-005
- **Complessita:** M
- **Descrizione:** Workflow `ci.yml` su push/PR: restore, build, test (unit + integration con Testcontainers), collect coverage, fail se warnings. Workflow `security-scan.yml` settimanale con `dotnet list package --vulnerable` e CodeQL.
- **Acceptance:**
  - [ ] Badge CI verde sul README
  - [ ] PR bloccata se test falliscono o CVE High/Critical rilevate

### TASK-013 — Deploy script IIS per ambiente staging (skeleton)

- **Modulo:** DevOps
- **Dipende da:** TASK-007, TASK-012
- **Complessita:** M
- **Descrizione:** Workflow `deploy.yml` che su merge in `develop` pubblica artefatto `dotnet publish`, upload via WebDeploy/MSDeploy su IIS staging, script PowerShell per setup app pool + binding HTTPS. `appsettings.Staging.json` con placeholder segreti.
- **Acceptance:**
  - [ ] Deploy staging funzionante con smoke test post-deploy (GET /health)
  - [ ] Procedure documentate in `docs/deployment.md`

### TASK-014 — Documentazione deployment + tech-specs iniziale

- **Modulo:** Documentazione
- **Dipende da:** TASK-013
- **Complessita:** S
- **Descrizione:** Popolare `docs/deployment.md` (IIS prerequisiti, binding HTTPS, permessi, env vars), `docs/tech-specs.md` con tutte le dipendenze installate finora (seguendo struttura DIR-015).
- **Acceptance:**
  - [ ] Un developer nuovo deve poter replicare il setup seguendo la documentazione

**📍 CHECKPOINT 2 — fine Fase 1:** dimostrazione funzionante: login, switch anno, deploy staging completato. Approvazione utente per procedere in Fase 2.

---

## Fasi 2-9 — Outline ad Alto Livello

Decomposizione atomica verra prodotta al termine della Fase 1, incorporando apprendimenti.

### Fase 2 — Dominio contabile base (sprint 3-4)

Moduli: 04 Anagrafiche, 05 Piano Conti & Causali, 06 Conti Finanziari.

Aree di task attese:
- Entita anagrafiche (Cliente, Fornitore, Dipendente) con indirizzi e contatti
- Categorie piatte con gerarchia "Entrate / Uscite" top-level
- Causali predefinite seed (es. "Incasso fattura", "Pagamento fornitore", "Stipendio", "F24")
- Aliquote IVA italiane seed (22%, 10%, 5%, 4%, 0%, N.I., esenti, ecc.)
- ContoFinanziario con tipo (Cassa/Banca/Carta), IBAN, saldo iniziale per anno, saldo calcolato
- UI CRUD con MudBlazor

Checkpoint: fine modulo 04, fine modulo 05, fine modulo 06.

### Fase 3 — Core Prima Nota (sprint 5-6)

Modulo: 07 Prima Nota.

Aree:
- Entita `MovimentoPrimaNota`, `RigaMovimento` (per split)
- CRUD con split multi-conto, allegati (upload file, hash SHA-256, storage filesystem cifrato)
- Stati DRAFT/CONFIRMED/RECONCILED
- Concurrency token (rowversion) per multiutente
- Filtri avanzati (anno, conto, categoria, anagrafica, date range, importo range)
- Tabella virtualizzata MudBlazor
- Undo entry recenti (30 giorni)

Checkpoint: MVP CRUD, poi split, poi allegati, poi concurrency.

### Fase 4 — Gestione IVA (sprint 7)

Modulo: 08 Gestione IVA.

Aree:
- Aliquote configurabili (seed + custom)
- Flag regime: `Ordinario` | `Forfettario`
- Regime ordinario: registro IVA vendite/acquisti/corrispettivi, calcolo liquidazione mensile o trimestrale
- Regime forfettario: imponibili per coefficiente di redditivita, nessuna liquidazione IVA
- Switch regime a livello annuale (nuovo esercizio puo cambiare regime)
- ADR dedicato: "Implementazione regime IVA italiano — regole applicate"
- Validazione con commercialista dell'utente prima del rilascio in produzione

Checkpoint: dopo registri, dopo liquidazione, dopo forfettario.

### Fase 5 — Import & Riconciliazione (sprint 8-10) **← modulo piu rischioso**

Moduli: 09 Import EC, 10 Motore Riconciliazione.

Aree:
- Parser CSV configurabile (mapping colonne salvabile per banca)
- Parser XLSX
- Parser PDF come "ultima spiaggia" (template per banca)
- Staging `MovimentoBancario` con stati IMPORTED/AUTO_MATCHED/MATCHED/PENDING/IGNORED
- Motore matching:
  - Match esatto importo+data (±tolleranza configurabile)
  - Applicazione regole utente (keyword contiene/regex/IBAN)
  - Scoring storico (Naive Bayes su keyword→categoria)
  - Confidence score
  - Auto-apply solo ≥ 95%
- UI riconciliazione: dashboard "Da riconciliare", wizard match manuale, split, undo
- Storico regole e apprendimento

**Questa fase ha il rischio piu alto (R1, R4). POC anticipato su CSV Intesa Sanpaolo, UniCredit, Banco BPM come validazione.**

Checkpoint: ogni sotto-area (parser → matching engine → UI → apprendimento).

### Fase 6 — OCR & LLM (sprint 11-12)

Moduli: 12 OCR Service, 13 LLM Gateway.

Aree:
- Integrazione Tesseract.NET, pre-processing immagine (OpenCvSharp o ImageSharp)
- POC accuratezza su dataset di 50-100 scontrini reali
- LLM Gateway OpenRouter con rate limiting e budget cap
- Prompt template per: "suggerisci categoria", "estrai campi da testo OCR"
- Caching hash-based (stesso input → stessa risposta)

Checkpoint: dopo POC OCR (decisione se Tesseract sufficiente o upgrade necessario).

### Fase 7 — Note Spese Dipendenti (sprint 13-14)

Modulo: 11 Note Spese.

Aree:
- Upload mobile-friendly (fotocamera da PWA)
- OCR → proposta campi → conferma utente
- Workflow approvativo: DRAFT → SUBMITTED → APPROVED/REJECTED → REIMBURSED
- Notifica email via SMTP al contabile su SUBMITTED
- Flag "pagato con mezzi propri" / "mezzi aziendali" → genera o non genera movimento di rimborso
- Dashboard dipendente con andamento YTD
- Export nota spese (riepilogo + allegati zip)

Checkpoint: dopo OCR flow, dopo workflow approvativo.

### Fase 8 — Dashboard & Reporting (sprint 15-16)

Moduli: 14 Dashboard, 15 Reporting.

Aree:
- Dashboard contabile: saldi per conto, entrate/uscite mensili, top 10 categorie, riconciliazioni pendenti, alert IVA
- Dashboard dipendente: note spese YTD, stato approvazioni
- Grafici MudBlazor (charts)
- Export Excel: prima nota, registri IVA, liquidazione IVA, pacchetto commercialista (multi-sheet)
- Export PDF: registri IVA stampabili, note spese, report mensile/annuale

Checkpoint: dopo dashboard, dopo export Excel, dopo export PDF.

### Fase 9 — Admin, Hardening, Go-Live (sprint 17)

Moduli: 16 Admin, completamento 17 Audit.

Aree:
- Gestione utenti: invita, attiva/disattiva, reset password, assegnazione ruoli
- Configurazione regole riconciliazione
- Configurazione aziendale (logo, intestazione stampe, partita IVA, codice fiscale)
- Audit log completo con filtro e export
- Security review (OWASP ZAP baseline, penetration test base)
- Performance test su dataset 5k movimenti
- Manuale utente completo in italiano (`docs/user-manual.md`)
- Procedura chiusura anno documentata
- Backup restore test

Checkpoint: pre-produzione obbligatorio con approvazione esplicita utente.

---

## Summary

| Fase | Sprint | Moduli | Checkpoint | Rischio |
|------|--------|--------|------------|---------|
| 1 | 1-2 | 01, 02, 03, 17 (base) | 2 | Basso |
| 2 | 3-4 | 04, 05, 06 | 3 | Basso |
| 3 | 5-6 | 07 | 3 | Medio |
| 4 | 7 | 08 | 3 | **Alto** (fiscalita) |
| 5 | 8-10 | 09, 10 | 4 | **Molto alto** (R1, R4) |
| 6 | 11-12 | 12, 13 | 2 | Medio (R2, R3) |
| 7 | 13-14 | 11 | 2 | Medio |
| 8 | 15-16 | 14, 15 | 3 | Basso |
| 9 | 17 | 16, 17 (completo) | 2 | Medio |

**Totale:** ~17 sprint di 2 settimane ≈ 34 settimane (~8 mesi) a tempo pieno singolo dev.

---

## Storico Modifiche

| Data | Versione | Modifica | Motivazione |
|------|----------|----------|-------------|
| 2026-04-16 | 1.0 | Creazione iniziale | Task Decomposition su project-spec.md v1.1 APPROVATA |
