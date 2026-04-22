# Copilot Instructions & Project Overview

## Project Overview

MoneyWeb is part of the delfraisse.com family of applications, designed as a comprehensive financial tracking solution to help users manage their personal finances effectively. It provides tools to monitor future spending, track current loans (both incoming and outgoing), and maintain a clear overview of cash flow. The application aims to simplify financial management by offering features such as debt tracking, payment scheduling, recurring bills, and cash flow analysis.

**Deployment URL**: [money.delfraisse.com](https://money.delfraisse.com)

The goal is to create a user-friendly platform that empowers individuals to make informed financial decisions and stay on top of their financial commitments, seamlessly integrated into the broader delfraisse.com ecosystem.


## Copilot Instructions
    - When specifying a file, please specify the project name and the file name.

    - **Branching Workflow:**
        - Before making any code changes, ensure you are on the `main` branch or a temporary feature branch created from `main`.
        - All development and testing should be performed on `main` or a feature branch (never directly on `release`).
        - After development is complete and tested, merge changes from `main` (or the feature branch) into the `release` branch to prepare for production deployment.
        - Only merge into `release` when the code is production-ready.
        - Hotfixes for production should be made on `release` and then merged back into `main` if needed.

## Technical Stack
    - **Development**: .NET Aspire
    - **Language**, C#, ASP.NET Core
    - **Database**: SQL Server
    - **ORM**: Dapper
    - **Migration**: FluentMigrator
    - **Frontend**: Blazor

## Projects
     - **MoneyWeb.Data**:         
        - Contains the entity classes that define the structure of the database objects (e.g., `User`, `Debt`, `Loan`, `CashFlowTransaction`).
        - Responsible for encapsulating the domain models and data-related logic.
    - **MoneyWeb.Blazor**: 
        - A Blazor web interface for both API control and the MoneyWeb application itself.
        - Using Bootsrap for responsive design and user interface components.
        - Provides a user-friendly frontend for managing finances, interacting with the API, and visualizing data.