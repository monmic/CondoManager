# AGENTS.md — CondoManager

## Project context
CondoManager is a condominium-management web application designed for both a single property-management office and a multi-tenant SaaS product. Work incrementally; preserve existing working features and data.

## Technology and architecture
- Backend: .NET 8, ASP.NET Core, EF Core, PostgreSQL.
- Frontend: React, TypeScript, Vite.
- Deployment: Docker; keep local development reproducible.
- Modular monolith with Domain, Application, Infrastructure, and API boundaries. Follow the repository's actual structure rather than creating parallel implementations.
- Keep business rules in Domain/Application; keep persistence and external integrations in Infrastructure; expose DTOs rather than EF entities through API endpoints.
- Prefer existing project conventions and packages; avoid introducing microservices or dependencies without a concrete need.

## Start of every task
1. Read this file, README.md, relevant docs and existing code.
2. Inspect Git status and identify pre-existing user changes; do not overwrite them.
3. Identify the requested milestone's scope and provide a short implementation plan.
4. Reuse existing implementations; avoid duplicate entities, migrations, services, endpoints or components.
5. Work in small verifiable increments, continuing autonomously unless a consequential decision or destructive operation needs approval.

## Multi-tenancy and security (non-negotiable)
- Tenant represents a management office; each tenant may manage multiple condominiums.
- Users may belong to multiple tenants through memberships and tenant-scoped roles.
- Derive and validate the active tenant server-side against the authenticated user's active membership; never trust a TenantId supplied by the client.
- Scope all tenant-owned reads and writes; validate foreign-key relationships to prevent cross-tenant associations and IDOR vulnerabilities.
- Use authorization policies on APIs, not only UI visibility. Platform administrators do not automatically receive unrestricted access to tenant business data.
- Default to authenticated access; explicitly document intentionally public endpoints.
- Do not commit secrets, passwords, access tokens, production data, or unredacted condominium documents.
- Use secure password handling through ASP.NET Core Identity or the existing approved identity solution. Prefer secure HttpOnly cookies and CSRF protection for browser sessions; never persist sensitive tokens in localStorage.
- Never log secrets, passwords, cookies or authentication tokens.

## Domain and accounting correctness
- Preserve historical relationships between people and property units; avoid overwriting ownership history.
- Support buildings, staircases, property units, allocation tables and accounting periods without assuming a calendar-year fiscal period.
- Use decimal for millesimal shares and monetary amounts; never float/double for financial calculations.
- Make allocation rules explicit, deterministic, testable and auditable. Define rounding policies before implementing financial distribution.
- Preserve draft versus approved document/budget versions; do not overwrite approved records silently.
- Never infer real resident or financial data from example documents. Use synthetic or anonymized fixtures only.

## Backend conventions
- Respect dependency direction: Domain independent; Application depends on Domain; Infrastructure implements persistence/integrations; API wires dependencies and HTTP contracts.
- Validate inputs and return consistent, safe error responses; do not expose stack traces in production.
- Use EF Core migrations for schema changes. Review generated SQL for potentially destructive changes.
- Do not automatically reset, drop, truncate, or reseed an existing database.
- Add indexes, uniqueness constraints and concurrency safeguards where justified by the domain.

## Frontend conventions
- Use TypeScript types matching API DTOs; avoid `any` unless justified.
- Build reusable components and clear loading, empty, validation, unauthorized and error states.
- Keep the UI desktop-first but responsive.
- Never treat hidden buttons or client-side route guards as a substitute for server authorization.
- Use real API integration for completed functionality; label temporary mocks clearly and remove them before claiming completion.

## Tests and verification
- Add unit tests for domain rules and integration tests for database, authorization and tenant isolation.
- Test unauthorized access, cross-tenant reads/writes, invalid relationships, decimal precision and accounting-period overlap where relevant.
- Prefer PostgreSQL-backed integration tests for behavior dependent on PostgreSQL.
- Run relevant builds, tests and frontend checks after changes. Report exact commands and results; if tooling is unavailable, say so instead of claiming success.
- Do not weaken validation, security or tests just to obtain a passing build.

## Git and files
- Do not commit, push, rebase, reset, clean, or force-update Git unless explicitly requested.
- Do not modify or remove unrelated files or user changes.
- Keep `.env`, local secrets, binaries, build outputs, node_modules and private PDF documents out of Git. Version `.env.example`, migrations, lockfiles, docs and this AGENTS.md.
- Keep documentation in `docs/` and update README for new setup requirements.

## Milestone execution policy
- Implement only the milestone requested. Do not begin the next milestone automatically.
- Continue through planned phases without asking for repeated “OK” messages when the task is clear.
- Stop and ask before irreversible operations, security-sensitive policy decisions without a safe default, or changes that could destroy existing data.
- At completion, summarize: implemented features; changed files; migrations; build/test commands and actual outcomes; startup steps; known limitations; suggested next milestone.

## Current roadmap
- M01: project foundation and runnable application.
- M02: condominiums, property units, people, allocation tables, accounting periods.
- M03: authentication, tenant memberships, roles, authorization, audit and security tests.
- M04: chart of accounts, expense recording and accounting movements.
- M05: allocation engine and adjustments.
- M06: budgets, actuals and installments.
- M07: meetings, minutes and document generation.

This roadmap is planning context, not proof that any milestone is complete. Verify repository state before relying on previous milestone work.
