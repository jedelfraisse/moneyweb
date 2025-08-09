# Copilot Instructions & Project Overview

## Project Overview

This project is a financial tracking application designed to help users manage their personal finances effectively. It provides tools to monitor future spending, track current loans (both incoming and outgoing), and maintain a clear overview of cash flow. The application aims to simplify financial management by offering features such as debt tracking, payment scheduling, recurring bills, and cash flow analysis.

The goal is to create a user-friendly platform that empowers individuals to make informed financial decisions and stay on top of their financial commitments.

## Copilot Instructions
    - When specifying a file, please specify the project name and the file name.

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
        - Provides a user-friendly frontend for managing finances, interacting with the API, and visualizing data.