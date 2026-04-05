---
phase: 01-foundation-and-access-control
plan: 03
status: complete
requirements_met:
  - AUD-01
  - AUD-02
tests:
  integration: 6/6
---

## Summary

Plan 01-03 delivered cross-cutting RBAC enforcement and automatic audit stamping/event logging across all Phase 1 endpoints.

### What was built

**RBAC (AUD-02)**
- `Permissions.cs` — `Roles` constants (Admin, Buyer, Vendor)
- `RolePolicyMatrix.cs` — centralized list of `(method, path-prefix) → allowed roles` policies with deny-by-default
- `RbacPolicyMiddleware.cs` — evaluates policy and responds 401/403 before the endpoint handler runs; unknown paths fall through to ASP.NET authorization
- Wired via `app.UseRbacPolicy()` between `UseAuthentication` and `UseAuthorization`

**Audit stamping (AUD-01)**
- `IAuditable` interface on `Company` and `CompanyEmployee` — marks entities for interceptor detection
- `IActorContextAccessor` / `HttpActorContextAccessor` — resolves current actor from `ClaimTypes.NameIdentifier` without creating ASP.NET dependency in Infrastructure
- `AuditEvent` — immutable record (EntityType, EntityId, Operation, Actor, OccurredAtUtc); payload-free per T-01-16
- `AuditEventWriter` — structured log output with `AUDIT` prefix
- `AuditStampInterceptor` — `SaveChangesInterceptor` that stamps `CreatedAtUtc`, `CreatedBy`, `UpdatedAtUtc`, `UpdatedBy` on Added/Modified entities and emits an `AuditEvent` per write
- Interceptor registered via `options.AddInterceptors(...)` in `Program.cs` alongside scoped `IActorContextAccessor` and `IAuditEventWriter`

### Threat mitigations applied

| Threat | Mitigation |
|--------|-----------|
| T-01-13 (role claim parsing) | Claims validated through `HeaderAuthenticationHandler`; policy resolved from `ClaimTypes.Role` |
| T-01-14 (audit field tampering) | Fields stamped by interceptor at `SaveChanges` time; client-provided values overwritten |
| T-01-15 (mutation accountability) | Immutable `AuditEvent` emitted per write with actor, entity, operation, UTC time |
| T-01-16 (audit payload exposure) | `AuditEvent` contains only identifiers and metadata, no body content |
| T-01-17 (middleware availability) | Middleware fails closed (401/403) when no matching role; `NoPolicy` defers to ASP.NET |
| T-01-18 (privilege escalation) | Tests explicitly assert Buyer→admin and Vendor→master-data are denied |

### Test results

```
Passed  RbacAndAuditTests (6/6)
  AUD02_Unauthenticated_Returns401
  AUD02_BuyerCannotWriteAdmin_Returns403
  AUD02_VendorDeniedMasterData_Returns403
  AUD02_AdminCanWrite_Returns201
  AUD01_CreateStampsActorAndTimestamp
  AUD01_UpdateStampsUpdatedBy_NotCreatedBy

Full suite: 7/7 infrastructure + 15/15 integration = 22/22 total
```
