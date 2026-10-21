<!--
Sync Impact Report
Version change: (unratified template) → 1.0.0
Modified principles: none renamed (all placeholders replaced; initial adoption)
Added sections: Core Principles I–VI, Stack and Security Constraints,
  Development Workflow and Quality Gates, Governance
Removed sections: none
Follow-up TODOs: none
-->
# SI-18 Constitution

## Core Principles

### I. Accessibility WCAG 2.1 AA (NON-NEGOTIABLE)
Every user-facing screen and component MUST conform to WCAG 2.1 level AA. Interactive elements
MUST be keyboard operable with visible focus, have accessible names and labels, and meet AA
colour-contrast ratios. Errors and status changes (e.g. a rejected booking, a validated QR)
MUST NOT rely on colour alone and MUST be announced to assistive technologies. A feature is not
done until its UI has been checked against these criteria.
Rationale: the app serves all citizens of a municipality, so accessibility is a legal and
social requirement, not an enhancement.

### II. Business Logic Is Tested
All business-rule logic MUST be covered by automated tests, including both sides of every
boundary (e.g. exactly 7 days, exactly 24 h, exactly 15 minutes). Bug fixes MUST include a
regression test. Tests for business logic MUST be deterministic: time MUST be injected through
an abstraction (clock), never read from the system clock directly.
Rationale: the rules decide money and access; they are cheap to test and costly to get wrong.

### III. Business Rules Live in the Backend
The ASP.NET Core API is the single source of truth for business rules. The following rules
MUST be implemented and enforced only in the backend, in one place each:
- A court can be booked at most 7 days in advance.
- A cancellation is refunded only if it is made MORE than 24 h before the start time;
  otherwise there is no refund.
- A booking's QR code is valid from 15 minutes before the start until the end of the booking,
  and is invalid once the booking is cancelled.
The Blazor client MAY mirror these rules for user feedback, but it MUST NOT be trusted and MUST
obtain the authoritative result from the API. Rule thresholds MUST be named constants or
configuration, never duplicated literals.
Rationale: a single enforcement point prevents client/server drift and bypass.

### IV. Consistent API Errors
All API errors MUST use one response shape (RFC 9457 Problem Details) with a stable machine-readable
error code, a human-readable message in Spanish suitable for display, and the correct HTTP
status (400/422 validation or rule violation, 401/403 auth, 404 not found, 409 conflict).
Unhandled exceptions MUST be converted by a global handler and MUST NOT leak stack traces or
internal details. Rule rejections (e.g. booking beyond 7 days) MUST return an explanatory
message.
Rationale: clients can handle errors uniformly and users receive understandable feedback.

### V. No Secrets in the Repository (NON-NEGOTIABLE)
Credentials, connection strings, API keys, payment-provider keys and signing keys MUST NEVER
be committed. They MUST be supplied via environment variables, user-secrets or a secret store;
the repo MAY contain only placeholder examples. A secret that is committed MUST be treated as
compromised and rotated.
Rationale: leaked secrets cannot be reliably retracted from git history.

### VI. Fictitious Test Data
Seed data, fixtures, tests and demos MUST use fictitious data only. Real personal data
(names, emails, phone numbers, ID numbers, payment details) MUST NOT appear in the repository,
logs or test databases.
Rationale: protects citizens' privacy and keeps the project compliant with data-protection law.

## Stack and Security Constraints

- Frontend: Blazor WebAssembly with MudBlazor components (which MUST be used in an accessible
  manner per Principle I).
- Backend: ASP.NET Core Minimal API; business rules in a layer independent from HTTP endpoints
  so they can be unit tested.
- Database: PostgreSQL, with schema changes applied through versioned migrations.
- Monetary amounts MUST use exact decimal types, and booking time slots are 1 hour.
- Deviations from this stack MUST be justified in the plan and approved per Governance.

## Development Workflow and Quality Gates

- Specifications, plans and tasks MUST be written through the Spec Kit flow before
  implementation of a feature.
- Every pull request MUST pass build and tests, and the reviewer MUST confirm compliance with
  Principles I–VI.
- A pull request that adds or changes a business rule MUST include tests (Principle II) and MUST
  place the rule in the backend (Principle III).
- A pull request that changes UI MUST state how WCAG AA was verified (Principle I).
- Secret scanning MUST be performed before merging when tooling is available (Principle V).

## Governance

This constitution supersedes other practices in this repository. Amendments MUST be proposed
through a pull request describing the change, rationale and impact on existing specs, plans and
code, and MUST be approved by the project team before merge.

Versioning follows semantic versioning: MAJOR for backward-incompatible removal or redefinition
of principles; MINOR for a new principle/section or materially expanded guidance; PATCH for
clarifications and wording fixes.

Compliance MUST be reviewed in every pull request and at the planning stage (Constitution
Check) of every feature. Violations MUST be fixed or explicitly justified and approved before
merge. Runtime guidance for contributors is in [README.md](../../README.md).

**Version**: 1.0.0 | **Ratified**: 2026-10-21 | **Last Amended**: 2026-10-21
