# MoneyWeb — Written Information Security Program (WISP)

| | |
|---|---|
| **Version** | 0.1 (draft, development phase) |
| **Effective** | pending |
| **Owner / Qualified Individual** | Jonathan Delfraisse, Owner & Operator, delfraisse.com |
| **Contact** | support@delfraisse.com |
| **Next review** | 12 months from effective date, or sooner per §13 |

## 1. Purpose and scope

MoneyWeb (money.delfraisse.com) is a household finance planner operated by delfraisse.com. This program describes the administrative, technical and physical safeguards used to protect the information MoneyWeb holds about its users. It is written to meet the FTC Safeguards Rule (16 CFR Part 314, under the Gramm-Leach-Bliley Act), and it is sized for MoneyWeb's current stage:

- **Invite-only.** Every account must be approved by an administrator before it can see or enter data.
- **One operator.** One person develops, operates and administers the service.
- **Fewer than 5,000 consumers.** The provisions of 16 CFR 314.6 apply.

The program covers MoneyWeb's production web application and database, its source code and deployment pipeline, and every service provider that stores or processes customer information for it.

## 2. Qualified Individual

The Owner named above is the Qualified Individual responsible for overseeing, implementing and enforcing this program. Their responsibilities are to:

- maintain this document and the risk assessment (§3)
- approve access to systems and customer information (§5)
- select and oversee service providers (§10)
- lead incident response (§11)
- review the program at least once a year (§13)

## 3. Risk assessment

The Qualified Individual maintains a risk assessment that identifies reasonably foreseeable internal and external risks to customer information, evaluates the safeguards in place against them, and records planned improvements. It is reviewed:

- at least once a year
- before opening MoneyWeb to the public
- before storing bank transaction data or connecting to a bank-data provider in production
- after any security incident

The risk assessment names specific systems and weaknesses, so it is **kept confidential** and is not published with this document.

## 4. Information covered

**Customer information** means any information MoneyWeb holds about a user, including:

| Category | Examples |
|---|---|
| Identity & contact | Name, email address, postal code |
| Financial records entered by the user | Income, bills, debts, loans, sinking funds, bank account names and balances, cash-flow entries and history |
| Bank transaction data *(planned)* | Transactions imported from bank files or received from a bank-data provider; provider access tokens |
| Account & security data | Sign-in records, approval status, sharing relationships, consent records |

MoneyWeb does **not** collect or store passwords, full bank account numbers, card numbers, bank sign-in credentials or Social Security numbers. Bank connections, once offered, go through a provider so that MoneyWeb never sees bank sign-in credentials.

**Where it lives:** the production database, hosted in the United States, and its backups. No other copies are made. Customer information must not be copied to personal devices, email or development environments.

## 5. Access control

- **Least privilege.** Only the Qualified Individual has administrative access to production systems (hosting control panel, database, deployment secrets, source repository settings). Any additional person must be approved in writing and is limited to what their role needs.
- **User isolation.** Every query is scoped to the signed-in user. A user can see another user's data only through explicit sharing that the owner grants.
- **Account approval.** New accounts have no access to data until an administrator approves them. Approval can be revoked at any time.
- **Access review.** Administrative accounts and approved users are reviewed at least once a year and whenever someone's role changes. Unneeded access is removed.
- **Development uses no production data.** Development and testing use synthetic data or provider sandboxes only.

## 6. Authentication

- **Users** sign in without a password, using a one-time code or link sent to their email address. Sessions use secure, HTTP-only cookies that expire after a period of inactivity.
- **Multi-factor authentication** is required for:
  - all operator access to production systems (hosting, database, GitHub, email)
  - administrator access within the application
  - offered to all users, and required before connecting a bank, once bank connections exist
- Shared accounts are not permitted for operator access.

## 7. Encryption

- **In transit.** All traffic between users and MoneyWeb uses HTTPS (TLS 1.2 or higher) with HSTS. Connections from the application to its database and its email provider are encrypted. Deployments to the host use encrypted transfer (FTPS).
- **At rest.** The most sensitive values (bank-data provider access tokens, and any account identifiers received from imports) are encrypted at the application level using ASP.NET Core Data Protection. The Data Protection key ring is persisted and protected from other tenants and processes. Database and backup encryption provided by the host is documented in the risk assessment.
- If a control cannot be encrypted, the Qualified Individual approves an effective alternative control and records it in the risk assessment.

## 8. Secrets management

- Production secrets (database connection, email credentials, deployment credentials, provider keys) are stored only in GitHub Actions encrypted secrets and on the production host.
- Secrets are never committed to source control. Production configuration files are excluded by `.gitignore` and generated during deployment.
- GitHub secret scanning and push protection are enabled on the repository.
- Secrets are rotated when someone with access leaves, when exposure is suspected, and at least once a year for database and deployment credentials.

## 9. Data retention and disposal

- Customer information is kept only while it is needed to provide the service.
- **Self-service deletion.** Users can delete their account and all associated data at any time.
- **Inactive accounts.** An account with no sign-in for **2 years** is deleted, after reminder emails to the address on file. This meets the Safeguards Rule requirement to dispose of customer information no later than two years after it was last used to serve the customer.
- Deletion removes the user's records from the production database and revokes any bank-data provider connection. Backups age out on the host's retention schedule and are not restored except for disaster recovery.
- Raw imported bank data is kept only as long as it is needed for matching and preventing duplicates.
- Audit logs are kept for _[12 months]_ and contain no financial details or tokens.

## 10. Service providers

Service providers that store or process customer information are selected for their ability to protect it, and are reviewed at least once a year.

| Provider | Purpose | Customer information |
|---|---|---|
| Web/database host (US) | Runs the application and database | All |
| Email provider (SMTP) | Sends sign-in codes and notices | Email address |
| GitHub | Source code, CI/CD and secrets | None (code and secrets only) |
| Bank-data provider *(planned; sandbox only)* | Bank connections and transaction feeds | Bank transaction data |

Before production use, a bank-data provider must:
- support revocation of access tokens
- publish an end-user privacy policy
- make its security attestations (for example SOC 2) available for review

## 11. Incident response

1. **Detect and contain.** Take the affected system offline or revoke the affected access, and rotate any exposed secrets.
2. **Assess.** Determine what information was involved, whose, whether it was encrypted, and how it happened.
3. **Notify.**
   - **FTC:** if unencrypted customer information of **500 or more consumers** was acquired without authorization, notify the FTC through its online form as soon as possible and no later than **30 days** after discovery.
   - **Users and states:** notify affected users, and state authorities where required, under applicable state breach-notification laws.
   - **Providers:** notify any bank-data provider as its agreement requires.
4. **Recover and learn.** Fix the root cause, update the risk assessment and this program, and keep a written record of the incident.

## 12. Change and vulnerability management

- All changes are tracked in source control and deployed only through the automated pipeline from the `release` branch, after being tested.
- Automated dependency alerts and updates (Dependabot), code scanning (CodeQL) and secret scanning run on the repository. Critical and high-severity findings are addressed promptly.
- Hosting, framework and runtime updates are applied on a regular schedule.
- Security-relevant events are logged with who did what and when: sign-ins, approvals, sharing changes, data exports, account deletion and bank connections.
- Vulnerability reports can be sent to support@delfraisse.com.

## 13. Training, monitoring and review

- The Qualified Individual stays current on security practices relevant to MoneyWeb and makes sure anyone given access is trained on this program before that access is granted.
- The program is evaluated and updated:
  - at least once a year
  - when the risk assessment identifies a material change
  - before public launch
  - before production use of bank transaction data
  - after any incident

## Revision history

| Version | Date | Change |
|---|---|---|
| 0.1 | 2026-10-17 | Initial draft for the development phase |

---
*This document is a working draft and is not legal advice. Before going to production with bank transaction data, confirm with an attorney whether MoneyWeb is a "financial institution" under the Safeguards Rule and whether any additional requirements apply.*
