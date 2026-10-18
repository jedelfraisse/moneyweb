# Security Policy

MoneyWeb (money.delfraisse.com) handles household financial information, so security reports are taken seriously.

## Reporting a vulnerability

Please **do not open a public GitHub issue** for a security problem.

Report it privately instead, either:
- through GitHub's **[Report a vulnerability](https://github.com/jedelfraisse/moneyweb/security/advisories/new)** form, or
- by email to **support@delfraisse.com** with "Security" in the subject.

Include what you found, how to reproduce it, and what it could affect. You'll get an acknowledgement within a few days, and updates as it's investigated and fixed.

Please give us a reasonable chance to fix the issue before disclosing it publicly, and don't access, change or delete other users' data while testing.

## Supported versions

Only the current production deployment at money.delfraisse.com (the `release` branch) is supported.

## How security is managed

MoneyWeb follows a Written Information Security Program: see [docs/compliance/WISP.md](docs/compliance/WISP.md). Dependency alerts (Dependabot), code scanning (CodeQL) and secret scanning with push protection are enabled on this repository.
