# MoneyManager — Copilot Instructions

## Project Overview

Personal finance tracker replacing a manually-maintained Excel workbook. Core domains:

- **Debt management** — credit cards and loans with interest rates and minimum payments; supports avalanche (and other) payoff strategies, cascading freed payments to remaining debts
- **Bills** — recurring monthly/yearly expenses
- **Cash flow forecast** — combines debts and bills projected forward in time to predict future account balances and optimal payment timing
- **Outgoing loans** — tracking money lent to friends/family (principal, terms, repayment status)

## Tech Stack

- **Framework**: Blazor Server (.NET) — server-side rendering over SignalR; no WebAssembly
- **Language**: C#
- **Database**: SQL Server (LocalDB for local dev, full SQL Server for staging/prod)
- **ORM**: Entity Framework Core (target: code-first migrations)
- **UI**: Blazor components (`.razor` files); use `@inject` for services, `@code` blocks for component logic

## Solution Structure (planned)

```
MoneyManager.sln
├── MoneyManager.Web/          # Blazor Server app (entry point)
│   ├── Components/            # Razor components (.razor)
│   │   ├── Layout/            # MainLayout, NavMenu, etc.
│   │   └── Pages/             # Routable page components
│   ├── Services/              # Business logic / application services
│   └── Program.cs
├── MoneyManager.Core/         # Domain models, interfaces, business rules
│   ├── Models/                # Debt, Bill, Loan, CashFlowEntry, etc.
│   └── Services/              # Payoff strategy logic, forecast engine
└── MoneyManager.Data/         # EF Core DbContext, migrations, repositories
    ├── AppDbContext.cs
    └── Migrations/
```

## Build & Run Commands

```bash
# Restore and build
dotnet restore
dotnet build

# Run the web app (Blazor Server)
dotnet run --project MoneyManager.Web

# Run all tests
dotnet test

# Run a single test project or filter
dotnet test MoneyManager.Tests --filter "FullyQualifiedName~AvalancheStrategy"

# Add a new EF Core migration
dotnet ef migrations add <MigrationName> --project MoneyManager.Data --startup-project MoneyManager.Web

# Apply migrations to the database
dotnet ef database update --project MoneyManager.Data --startup-project MoneyManager.Web
```

## Key Domain Concepts

### Debt Payoff Strategies
The **avalanche strategy** targets the highest-interest debt first. When a debt is fully paid off, its freed payment amount is redirected ("cascaded") to the next highest-interest debt. The `Core` project owns this logic; the `Web` project only calls into it via injected services.

### Cash Flow Forecasting
The forecast engine iterates forward in time (day or month granularity) applying:
1. Scheduled bill payments (fixed dates or relative intervals)
2. Debt minimum payments + any extra applied by the active strategy
3. Incoming payments on outgoing loans

The output is a time series of projected account balances.

### Outgoing Loans
Loans made to friends/family are tracked separately from debts. They have a principal, optional interest rate, and a repayment schedule. Incoming payments increase projected cash flow.

## Conventions

- **Services registered in `Program.cs`** via `builder.Services.AddScoped<IXxxService, XxxService>()`. Scoped lifetime is preferred for EF-dependent services.
- **Blazor components use constructor-style `@inject`** at the top of `.razor` files, not property injection in `@code` blocks.
- **Domain models live in `MoneyManager.Core`** and must not reference EF Core or `MoneyManager.Data`. Keep the domain layer clean.
- **EF Core entities** use explicit backing fields where navigation properties need encapsulation. Avoid exposing raw `List<T>` on aggregates; prefer `IReadOnlyCollection<T>`.
- **Monetary values** are stored and calculated as `decimal`, never `double` or `float`.
- **Dates** use `DateOnly` for calendar dates (bill due dates, debt payoff dates); `DateTime`/`DateTimeOffset` only when time-of-day matters.
- **Percentage rates** (APR, interest) are stored as a decimal fraction (e.g., `0.2199m` for 21.99%) not as a whole number percentage.
