# MoneyWeb — Copilot Instructions

## Project Overview

Personal finance tracker, part of the **delfraisse.com** family of apps. Deployed at [money.delfraisse.com](https://money.delfraisse.com).

Core domains:
- **Debt management** — credit cards and loans with interest rates and minimum payments; supports avalanche (and other) payoff strategies, cascading freed payments to remaining debts
- **Bills** — recurring monthly/yearly expenses
- **Cash flow forecast** — combines debts and bills projected forward in time to predict future account balances and optimal payment timing
- **Outgoing loans** — tracking money lent to friends/family (principal, terms, repayment status)

## Tech Stack

- **Orchestration**: .NET Aspire (local dev orchestration and service discovery)
- **Language**: C#, ASP.NET Core
- **Database**: SQL Server (LocalDB for local dev)
- **Data access**: Dapper (raw SQL, no EF Core)
- **Migrations**: FluentMigrator
- **Frontend**: Blazor Server with Bootstrap for responsive UI
- **Integration**: Part of delfraisse.com ecosystem; see `sub-application-copilot-instructions.md` for portal integration guidelines

## Solution Structure

```
MoneyWeb.sln
├── MoneyWeb.AppHost/          # .NET Aspire host — orchestrates all projects locally
├── MoneyWeb.Data/             # Entity/model classes + Dapper repositories + FluentMigrator migrations
│   └── Migrations/
└── MoneyWeb.Blazor/           # Blazor Server app: pages, components, Bootstrap UI
    ├── Components/
    │   ├── Layout/
    │   └── Pages/
    └── Program.cs
```

## Build & Run Commands

```bash
# Restore and build
dotnet restore
dotnet build

# Run via Aspire (starts all projects together — preferred for local dev)
dotnet run --project MoneyWeb.AppHost

# Run tests
dotnet test

# Run a single test filter
dotnet test --filter "FullyQualifiedName~AvalancheStrategy"
```

### FluentMigrator
Migrations live in `MoneyWeb.Data/Migrations/`. They are applied on startup (or via CLI runner). To add a migration, create a new class inheriting `Migration` with `[Migration(YYYYMMDDHHMMSS)]` attribute.

## Key Domain Concepts

### Debt Payoff Strategies
The **avalanche strategy** targets the highest-interest debt first. When a debt is fully paid off, its freed payment amount is redirected ("cascaded") to the next highest-interest debt.

### Cash Flow Forecasting
The forecast engine iterates forward in time applying:
1. Scheduled bill payments (fixed dates or relative intervals)
2. Debt minimum payments + any extra from the active payoff strategy
3. Incoming payments on outgoing loans

Output is a time series of projected account balances.

### Outgoing Loans
Loans made to friends/family tracked separately from debts — principal, optional interest rate, repayment schedule. Incoming payments increase projected cash flow.

## Conventions

- **Always specify the project name when referencing a file** (e.g., `MoneyWeb.Blazor/Components/Pages/Debts.razor`, not just `Debts.razor`).
- **Data access uses Dapper** — write explicit SQL; no LINQ-to-SQL or EF Core query building.
- **Services registered in `Program.cs`** via `builder.Services.AddScoped<IXxxService, XxxService>()`.
- **Blazor components use `@inject`** at the top of `.razor` files.
- **Monetary values** are `decimal`, never `double` or `float`.
- **Dates** use `DateOnly` for calendar dates; `DateTime`/`DateTimeOffset` only when time-of-day matters.
- **Percentage rates** stored as decimal fractions (e.g., `0.2199m` for 21.99%), not whole-number percentages.
- **Bootstrap** is the only CSS framework — use Bootstrap utility classes and components; do not introduce Tailwind or other CSS frameworks.
