# MoneyWeb

MoneyWeb is a personal finance tracking application and part of the **delfraisse.com** family of apps. Built with .NET Aspire, Blazor, and SQL Server, it helps users manage loans, track debts, schedule payments, and analyze cash flow with a clean, user-friendly interface.

**🌐 Live at**: [money.delfraisse.com](https://money.delfraisse.com)

## Features
- 🔁 Recurring bill management
- 💸 Debt and loan tracking (incoming/outgoing)
- 📅 Payment scheduling
- 📊 Cash flow visualization

## Tech Stack
- .NET Aspire · ASP.NET Core · Blazor
- SQL Server · Dapper · FluentMigrator

Licensed under the MIT License.

## Branching & Deployment Workflow

### Main Branch (`main`)
- **Primary development branch**: Most feature development, bug fixes, and routine changes are made directly on `main`.
- For larger or experimental changes, create a temporary feature branch from `main`, then merge back into `main` via pull request after review.
- The `main` branch always reflects the latest stable development state.

### Release Branch (`release`)
- **Production deployment branch**: Contains the code and configuration used for production deployments.
- When ready to deploy, changes from `main` are merged into `release`.
- The `release` branch includes any additional actions or configuration needed to publish to the production web host.
- Only tested, production-ready code should be merged into `release`.

#### Typical Workflow
1. Develop features and fixes on `main` (or feature branches).
2. Merge feature branches into `main` after review.
3. When a release is ready, merge `main` into `release`.
4. The `release` branch triggers deployment actions to the production web host.
5. Hotfixes for production can be made directly on `release` and then merged back into `main` if needed.

---
