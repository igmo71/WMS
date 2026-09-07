# Receiving command consistency

Status: frozen reference. Completed 2026-09-07.

## Outcome

Web completion of receiving treats the operator's selected receiving location
and the final receiving transition as one explicit final-save operation. The
mapped legacy receiving API remains available for unknown external consumers,
but delegates to the same application operations and returns actionable
business errors.

## Scope

- Add a receiving completion operation that accepts the selected receiving
  location.
- Perform the fresh 1C synchronization checkpoint before staging a changed
  location, then validate and stage the location before transition, posting,
  outbound completion, and the final classified save.
- Replace the Web page's preliminary location save plus completion call with
  the combined operation.
- Keep the legacy route group because repository search cannot rule out
  external callers.
- Route legacy start through the common start-receiving operation using the
  order's already assigned location, and expose typed status/message errors for
  both changing legacy routes.
- Preserve Mobile receipt behavior, routes, domain rules, and 1C protocol.

## Out of scope

- Removing or renaming legacy routes without external-consumer evidence.
- Adding a new legacy request contract for selecting a location.
- Refactoring application/1C ports or the synchronization checkpoint contract.
- Endpoint or page decomposition.
- New automated tests.

## Acceptance criteria

- A Web completion failure after fresh synchronization does not persist the
  newly selected receiving location.
- A successful Web completion persists the selected location, received state,
  inventory effects, and final local changes through the existing final save.
- Mobile completion continues using the location assigned at start and its
  receipt-protected staged operation.
- Legacy `set-in-receiving` uses the common start operation and rejects a
  missing or invalid assigned location instead of bypassing location checks.
- Legacy changing routes preserve their URLs and return the operation error
  message with status 404, 409, 422, or 400 as appropriate.
- Relevant projects build and `git diff --check` passes; no tests are created.

## Result

- Web completion now calls `CompleteReceivingAsync` once with the selected
  location. The required synchronization checkpoint is saved before that
  location is staged, so a later failure does not commit the location alone.
- The standalone public location-save command and the location-free receiving
  transition were removed. Start receiving continues through the combined
  location validation, transition, outbound call, and classified save.
- Repository search found no legacy route callers but cannot establish the
  absence of external consumers, so `/api/ReceivingOrder` remains mapped.
  Legacy start delegates to the common start operation with the order's
  assigned location; both changing routes return structured error responses.
- Mobile start and completion keep their existing receipt-protected staged
  operations and transport contracts.
- `Wms`, `Wms.WebApi`, and `Wms.WebApp` build successfully. WebApi retains the
  known `NU1903` warning for `Microsoft.OpenApi 2.0.0`.
- `git diff --check` passes and no automated tests were created.
