# WMS architecture

## Goal

WMS favors a direct, readable implementation over framework-driven layering:

```text
UI or HTTP endpoint
  -> application service
       -> EF Core DbContext (load, query, save)
       -> domain operation (in-memory invariants and transitions)
       -> integration service when the use case crosses into 1C

Manual 1C synchronization UI
  -> corresponding 1C integration service
  -> catalog application service
  -> EF Core DbContext
```

## Responsibilities

### UI and endpoints

- Own form state and transport concerns.
- Create application commands at the operation boundary; do not use commands
  or domain entities as mutable form models.
- Display operation outcomes without repeating business rules.

### Application services

- Orchestrate one visible use case as a short sequence.
- Load persisted state, query cross-aggregate facts, call integrations, obtain
  the current user and time, and define the save boundary.
- Keep checks that require `DbContext`, another service, or an external system.
- Use a named private method when a substantial check would obscure the main
  operation. There is no general validation pipeline.

### Domain

- Own invariants and state transitions that require no external state.
- Expose operations rather than public mutation; use private setters,
  read-only collections, and immutable value objects for WMS-owned aggregates.
- Receive timestamps and user identifiers as operation inputs when needed.
- Remain independent of application commands, EF `DbContext`, UI, and 1C
  transport models.

### Data and integration

- EF configurations describe persistence, not business decisions.
- Integration services own the 1C protocol and mapping. A WMS aggregate may
  accept a domain import snapshot, but not a 1C DTO.
- Application-facing integration contracts use WMS terminology, while their
  concrete implementations under `Integration.OneS` name the corresponding 1C
  metadata object. For example, `IShippingOrderSource` is implemented by
  `Document_РасходныйОрдерНаТовары_InboundService`.
- Application owns both source and execution-sink ports and the synchronization
  operation that applies a snapshot and produces its assessment. UI and HTTP
  boundaries do not compose concrete 1C synchronization services.
- `Integration.OneS` implements those ports and owns OData DTOs, metadata
  names, protocol statuses, notification parsing, and PATCH/POST mechanics.
  Existing dependencies that point in the opposite direction are accepted
  follow-up work; do not add a pass-through facade around them.

## Domain model categories

| Category | Examples | Expected style |
| --- | --- | --- |
| WMS-owned aggregate | `InventoryTransfer`, `InventoryCount` | Rich lifecycle, private state, read-only children |
| Integrated process aggregate | `ReceivingOrder`, `ShippingOrder` | Rich local workflow and explicit reconciliation |
| Inventory fact | `InventoryMovement`, `InventoryBalance`, `InventoryTurnover` | Controlled creation and posting; immutable history |
| WMS configuration | `Zone`, `StorageLocation` | Rich local invariants and controlled activation |
| 1C-owned catalog | `StockKeepingUnit`, `Partner`, `Individual`, `OrganizationalUnit`, `UnitOfMeasure` | Simple import model unless WMS owns a local rule |
| Read projection | report and list items | Data-only; immutable where practical |

`Warehouse` remains a simple 1C-owned import model until WMS owns a concrete
local warehouse lifecycle or invariant that justifies more behavior.

## Application organization

- Organize application code by business feature, not under a technical
  `Services` folder. A feature keeps its services, commands, queries, and read
  models together.
- Use separate command and query services when they represent distinct public
  responsibilities. Database reads required to execute a command remain in its
  command service.
- A small 1C-owned catalog may keep one cohesive service for import persistence
  and reads; splitting it merely for naming symmetry is not required.
- Do not create a folder or handler for every operation. Introduce another
  nesting level only when the feature folder is no longer easy to scan.

The MAUI client keeps pages under `Pages`, grouped by operator workflow. Shared
scanner adapters, HTTP/session services, platform code, and resources remain in
their corresponding top-level folders. Physical page folders do not require a
namespace per folder while the client remains small.

Mobile HTTP clients and transport contracts are grouped by business feature
over one internal HTTP/session transport. A page owns controls, modes,
navigation, focus, dialogs, and rendering. If retry-aware API sequences make a
page difficult to read, a concrete feature process may own those sequences and
stable request ids; this does not imply a common page base, controller
interface, MVVM framework, or workflow engine.

A file around 300 lines deserves a responsibility review but is not required
to be split. Extract only a business-named or clearly technical cohesive
responsibility that reduces the context needed to read the primary path. Keep
a large cohesive algorithm or aggregate intact when splitting would scatter
its invariants.

## Commands and queries

- A command is an immutable typed input describing an application operation
  that changes state. It does not imply MediatR, a handler type, or full CQRS.
- Use a command object for several related inputs, cross-field validation, or
  an explicit editable-field boundary. Methods with a few obvious parameters
  do not need a command class.
- Reuse a domain value object when it already represents exactly the editable
  state; do not create a duplicate command.
- A query describes a read operation or its criteria and may project directly
  to list or detail models.
- Reserve `Request` for an actual UI, HTTP, or integration transport contract.

## Validation and operation outcomes

Put a rule at the lowest layer that has all data needed to decide it:

1. value object — valid construction;
2. application command — self-consistent ranges and field combinations;
3. domain operation — local entity transition;
4. application service — database, authorization, integration, or
   cross-aggregate state;
5. database constraint — final concurrency-safe uniqueness and integrity.

`OperationResult`, `OperationResult<T>`, `OperationError`, and
`OperationErrorType` form a small shared kernel in `Wms.Common`. Domain,
application, and integration operations use them for expected outcomes such as
invalid input, a missing record, or a business conflict. This keeps expected
branching explicit without exception adapters.
`OperationError` factories are intentionally non-generic: the entity type is
not part of the error contract. Every error requires an informative message;
for a missing record it names the object and includes its identifier when one
is available in the operation context.

Unexpected infrastructure failures and programming errors remain exceptions.
Do not catch every `Exception` inside domain or application operations merely
to turn failures into results; an outer UI or API boundary may provide the
last-resort user-facing response and logging.

Repeated database-backed eligibility checks may use a narrow, named policy
with one explicit context. Such a policy does not own feature lifecycle,
quantities, allocation, or persistence. Do not introduce `IValidator<T>`,
automatic validator discovery, per-command validators, validation pipelines,
or option objects containing independent rule flags.

## Persistence

- Application services use `ApplicationDbContext` directly; EF Core is the
  unit-of-work boundary, so repositories are not added by default.
- One operation normally has one explicit `SaveChangesAsync` boundary.
- When a persisted mobile command receipt must be atomic with an existing WMS
  change, the command service may expose an internal `Stage...Async` operation
  that mutates a caller-owned `ApplicationDbContext` without saving. The mobile
  orchestration stages the business change and receipt in that context and
  performs one `SaveChangesAsync`; repositories or cross-context transactions
  are not introduced for this purpose.
- A method named `Stage...` never saves. When a critical external transition
  intentionally persists a synchronization checkpoint before later effects,
  name and document that higher-level two-phase operation explicitly rather
  than hiding the checkpoint behind the staging contract.
- Persistent invariant changes include a migration.
- Once a migration may have been applied outside the developer's disposable
  local database, keep it immutable: do not edit, rename, or delete it. Fix the
  schema with a subsequent migration. Replacing migration history is allowed
  only as an explicit pre-production baseline reset in which every affected
  database is deleted and recreated.
- Read-only domain collections use explicit EF backing-field configuration.
- Optimistic concurrency is added to mutable aggregate roots or rows only when
  a stale save can break a documented lifecycle or inventory invariant. Do not
  put `RowVersion` on a universal base entity merely for consistency.
- A cross-process guard may use a targeted numeric operational revision on the
  shared resource. Inventory posting and storage-location lock changes both
  advance `StorageLocation.OperationalRevision` in their single save boundary;
  this coordinates movement and locking without a repository, distributed
  lock, or long-running database transaction.
- Expected concurrency failures and violations of specifically named
  concurrency-related constraints may become `OperationResult.Conflict` at the
  application save boundary. Unrecognized database failures remain exceptions.
- Shared save-boundary handling and its narrow persistence-conflict classifier
  live under `Wms.Application.Persistence`; they are not inventory services or
  a repository abstraction.

## Project conventions and verification

- User-facing messages, expected operation errors, and log messages are written
  in Russian. External protocol values and technical identifiers retain their
  original spelling.
- A class is `public` only when it is consumed by another project or is a
  deliberate boundary of the `Wms` assembly. Assembly implementation details
  are `internal`; their member modifiers may remain `public` when that keeps
  the local API straightforward.
- Broad automated coverage is not the current MVP strategy. Before an
  operational pilot, use focused integration tests for authentication,
  idempotency, optimistic concurrency, transaction boundaries, and database
  constraints where static inspection cannot establish the guarantee.
- A model change requires an EF migration and an explicit migration-drift
  check. Other verification follows the scope and risk of the task.

## Deliberate non-goals

The current architecture does not require MediatR, command/handler pairs,
repositories over EF Core, universal base entities, a validation framework,
domain events, event sourcing, or separate read storage. It also does not make
1C-owned catalogs rich merely for uniformity. Add such machinery only for a
demonstrated product need.
