# Prima Nota Aziendale

Software gestionale web per la **prima nota aziendale semplificata**: importa estratti conto bancari, riconcilia automaticamente i movimenti con voci di prima nota (regole + apprendimento storico), gestisce anagrafiche, causali, IVA periodica, registri IVA e note spese dipendenti con OCR, su base di esercizi annuali (01/01 – 31/12).

## Stack

- **.NET 10 LTS** — Blazor Server + MudBlazor (UI)
- **Entity Framework Core 10** + **SQL Server 2022**
- **ASP.NET Core Identity** (email/password + Google OAuth)
- **Hangfire** (background jobs)
- **Serilog** (structured logging)
- **Tesseract** (OCR locale) + **OpenRouter** (LLM gateway opzionale)
- **xUnit** + **bUnit** + **Playwright** + **Testcontainers** (testing)
- **GitHub Actions** (CI/CD) → IIS (Windows Server) per staging/produzione

## Struttura

```
prima-nota/
├── src/
│   ├── PrimaNota.Shared/          # Constants, enum, resource IT
│   ├── PrimaNota.Domain/          # Entita, value object, domain services
│   ├── PrimaNota.Application/     # Handler MediatR, DTO, validators
│   ├── PrimaNota.Infrastructure/  # EF Core, integrazioni esterne, Hangfire
│   └── PrimaNota.Web/             # Blazor Server host (IIS entry point)
├── tests/
│   ├── PrimaNota.UnitTests/
│   ├── PrimaNota.IntegrationTests/  # Testcontainers su SQL Server
│   ├── PrimaNota.ComponentTests/    # bUnit
│   └── PrimaNota.E2ETests/          # Playwright
├── docs/
│   ├── project-spec.md              # Specifica tecnica APPROVATA
│   ├── implementation-plan.md       # Piano di implementazione
│   ├── tech-specs.md                # Registro dipendenze
│   ├── changelog.md
│   └── adr/                         # Architecture Decision Records
├── doe-framework/                   # Framework D.O.E.
├── Directory.Build.props            # Config MSBuild globale
├── Directory.Packages.props         # Central Package Management
├── global.json                      # Pin SDK .NET 10
└── PrimaNota.slnx                   # Solution file (XML format)
```

## Getting Started

### Prerequisiti

- .NET SDK **10.0.201** o superiore (rispetta `global.json`)
- Accesso a un'istanza **SQL Server 2022** (istanza aziendale in rete, Dev DB dedicato)
- Git

### Build

```bash
dotnet restore PrimaNota.slnx
dotnet build PrimaNota.slnx
```

### Test

```bash
dotnet test PrimaNota.slnx
```

### Run (Blazor Server locale)

```bash
dotnet run --project src/PrimaNota.Web
```

## Documentazione

- [Specifica tecnica](docs/project-spec.md) — cosa fa, architettura, rischi
- [Piano di implementazione](docs/implementation-plan.md) — fasi e task
- [Changelog](docs/changelog.md)
- [ADR](docs/adr/) — decisioni architetturali

## Framework D.O.E.

Progetto sviluppato seguendo il [D.O.E. Framework](doe-framework/DOE.md) (Direttiva, Orchestrazione, Esecuzione).
