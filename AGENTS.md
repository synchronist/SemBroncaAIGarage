# SBGarage - Repository Guidelines

## Product

SBGarage is a SaaS platform for automotive repair shops.

The system is being built incrementally. Preserve existing behavior unless the current task explicitly requires changing it.

The main product areas include:

- customers;
- vehicles;
- service orders;
- diagnosis;
- estimates and approvals;
- workshop workflow;
- scheduling;
- WhatsApp-assisted customer interaction;
- future AI-assisted features such as voice-to-text diagnosis.

Do not implement future functionality unless explicitly requested.

---

## Technology Stack

- .NET 10
- C#
- ASP.NET Core Web API
- Blazor Web
- MudBlazor
- Entity Framework Core
- PostgreSQL 17
- xUnit
- Shouldly
- Docker Compose

Use the technologies already present in the repository.

Do not introduce a new framework, architectural pattern, UI library, database abstraction, mediator library, or major dependency without explicit approval.

---

## Project Structure

The solution follows a layered architecture under `src/`.

### SemBroncaAI.Garage.Domain

Contains:

- entities;
- domain rules;
- state transitions;
- guards;
- repository interfaces;
- domain messages.

Domain must not depend on:

- Application;
- Infrastructure;
- Api;
- Web.

Business rules and invariants belong here whenever appropriate.

### SemBroncaAI.Garage.Application

Contains feature-oriented application behavior:

- commands;
- queries;
- handlers;
- responses;
- application abstractions.

Organize behavior by feature.

Example:

`Features/ServiceOrders/CreateServiceOrder/`

Do not put HTTP or Blazor concerns in Application.

### SemBroncaAI.Garage.Infrastructure

Contains:

- EF Core;
- GarageDbContext;
- entity configurations;
- PostgreSQL repositories;
- migrations;
- infrastructure implementations.

Do not move business rules from Domain into repositories or EF configurations.

### SemBroncaAI.Garage.Api

Contains:

- ASP.NET Core controllers;
- dependency injection;
- HTTP/API configuration.

Controllers should remain thin.

Business logic should not be implemented directly in controllers.

### SemBroncaAI.Garage.Web

Contains the Blazor frontend:

- Razor pages/components;
- isolated `.razor.css`;
- Web models;
- HTTP client services;
- UI behavior.

Use the existing visual language of the application.

MudBlazor is the current component library.

Do not introduce another UI framework.

### Tests

Tests live in:

`tests/SemBroncaAI.Garage.Tests`

Tests use:

- xUnit;
- Shouldly.

Add or update tests when changing business rules.

---

## Architecture Rules

Preserve dependency direction.

Prefer:

Web
→ Api
→ Application
→ Domain

Infrastructure implements persistence abstractions required by the application/domain.

Do not:

- access GarageDbContext directly from Web;
- access the database directly from controllers;
- put business rules in Razor components;
- duplicate domain rules in the frontend as the source of truth;
- bypass repositories merely to make implementation faster.

Frontend validation may improve UX, but Domain/API rules remain authoritative.

---

## Multi-Tenancy

SBGarage is intended to support multiple garages.

`GarageId` represents the tenant boundary.

Whenever reading or modifying tenant-owned data, consider garage isolation.

Do not remove or bypass GarageId filtering.

Do not design features assuming that only one garage will ever exist.

Authentication and tenant resolution are still evolving, so do not invent a final authentication or tenancy architecture unless explicitly requested.

---

## Service Order Domain

Service orders have an explicit workflow.

Current states include:

- Received
- Diagnosis
- WaitingApproval
- InProgress
- WaitingParts
- Finished
- Delivered
- Cancelled

State transitions must respect Domain rules.

Do not change allowed transitions unless the task explicitly requires it.

Service-order history is important and must remain consistent with transitions.

### Diagnosis

A service order must have a registered diagnosis before it can be sent for approval.

The Domain is the source of truth for this rule.

Diagnosis currently contains information such as:

- description;
- internal notes.

The Web may disable actions to provide better UX, but backend/domain validation must remain in place.

Do not allow silent modification of workflow rules merely to satisfy UI requirements.

---

## Blazor / UI Guidelines

The existing Figma designs are visual references, not pixel-perfect specifications.

Prioritize:

- professional appearance;
- consistency;
- usability;
- maintainability;
- responsiveness;
- future customization.

Preserve the existing SBGarage visual language when creating new screens.

Before creating a new page, inspect relevant existing pages and their `.razor.css` files.

Prefer isolated CSS:

`Component.razor`
`Component.razor.css`

Do not rewrite a working page unnecessarily.

When fixing visual bugs, identify the root cause before replacing or adding large amounts of CSS.

Do not change business logic while fixing a purely visual issue.

Visible UI changes should be easy to review.

---

## Web/API Integration

Blazor communicates with the backend through Web services using HttpClient.

Keep API calls out of Razor markup whenever practical.

Prefer dedicated services under:

`SemBroncaAI.Garage.Web/Services`

Use Web-specific models under:

`SemBroncaAI.Garage.Web/Models`

Do not reference Infrastructure from Web.

---

## Entity Framework Core

Use EF Core migrations for schema changes.

Never edit an already-applied migration merely to change the current schema.

Create a new migration when the model changes.

Before creating a migration:

1. inspect the current entity;
2. inspect its EF configuration;
3. inspect existing migrations;
4. ensure the schema change is actually required.

Do not delete migrations or recreate the database unless explicitly requested.

PostgreSQL is the current database.

---

## Coding Style

Use standard C# conventions:

- four-space indentation;
- PascalCase for types, methods, and public members;
- camelCase for parameters and local variables;
- `_camelCase` for private fields;
- interfaces prefixed with `I`;
- nullable reference types;
- file-scoped namespaces where appropriate.

Prefer one primary type per file.

Match filenames to their primary type.

Follow existing repository conventions before introducing a different style.

Avoid unrelated formatting changes.

---

## Testing

Tests use xUnit and Shouldly.

Domain invariants and state transitions should have focused tests.

When a business rule changes:

1. update tests representing the previous valid behavior;
2. add a test for the new rule when appropriate;
3. verify invalid behavior is rejected;
4. verify valid behavior remains supported.

Bug fixes should include regression tests when practical.

---

## Required Validation

After code changes, run:

`dotnet build SemBroncaAI.Garage.slnx`

and:

`dotnet test SemBroncaAI.Garage.slnx`

Both should succeed before considering a task complete.

If either fails:

- inspect the error;
- determine whether it was caused by the current task;
- fix task-related failures;
- do not hide errors or disable tests to make the build pass.

For EF schema changes, also verify that the migration is valid.

For visible Web changes, describe what should be manually verified in the browser.

---

## Change Scope

Work incrementally.

Before editing:

1. inspect the relevant implementation;
2. understand the existing pattern;
3. identify the minimum set of files required.

Do not perform unrelated refactors.

Do not rename projects, folders, public APIs, entities, or established concepts unless explicitly requested.

Do not replace existing working implementations simply because another approach is preferred.

If a larger architectural change appears beneficial, explain it first instead of implementing it automatically.

---

## Git Safety

Assume the repository contains valuable working code.

Before broad changes, inspect:

`git status`

Do not:

- discard user changes;
- reset the repository;
- force checkout files;
- rewrite Git history;
- force push;
- delete branches;
- automatically commit or push changes

unless explicitly requested.

Never commit:

- credentials;
- production secrets;
- API keys;
- local machine configuration;
- generated build artifacts.

---

## Task Execution

For implementation tasks, follow this sequence:

1. understand the request;
2. inspect relevant files;
3. inspect related tests;
4. explain the intended approach briefly when the change is substantial;
5. implement the smallest coherent solution;
6. add/update tests where appropriate;
7. run build;
8. run tests;
9. inspect the resulting diff;
10. report:
   - root cause or implementation approach;
   - files changed;
   - tests/build results;
   - migrations created, if any;
   - manual verification still required.

Do not claim success solely because code was written.

A task is complete only after appropriate validation.