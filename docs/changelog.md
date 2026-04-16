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
