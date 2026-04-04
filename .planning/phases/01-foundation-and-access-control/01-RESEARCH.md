# Phase 1 Research: Foundation And Access Control

## 1) Scope understanding for phase 1

Phase 1 is the platform control-plane foundation, not transactional procurement yet. It must deliver stable admin-managed identity anchors (company, employee, master data), plus cross-cutting controls (RBAC + audit) that every later phase depends on.

In-scope requirements:
- COMP-01, COMP-02: create/update company profile and status
- EMP-01, EMP-02, EMP-03: create/list/detail/update employee (including status)
- MSTR-01, MSTR-02: category hierarchy + administrative units read services
- AUD-01, AUD-02: actor/timestamp traceability + role boundary enforcement

Recommended phase boundary:
- Build only the entities/endpoints needed for admin onboarding and reference data serving.
- Do not implement RFP/Bid/Finalize state machines in this phase.
- Ensure every write-path is audit-ready now so later domains inherit the same pattern.

## 2) Domain model recommendations for company/employee/master data

Company aggregate:
- Company as root: legal identity (TaxCode, FullName, ShortName), lifecycle status, type.
- CompanyAddress as owned child (1:1 by FK), but exposed as flattened DTO for API ergonomics.
- Add uniqueness constraints early:
  - TaxCode unique (global)
  - Email unique per company scope or global (pick one and document)
  - ShortName optional unique if used as slug/lookup

Employee model:
- CompanyEmployee belongs to exactly one company.
- EmpId should be immutable business key (if externally referenced).
- Status should be enum-backed (Active, Inactive, Suspended) instead of magic ints.
- Keep profile fields mutable, identity linkage immutable:
  - Mutable: name, phone, avatar, status
  - Immutable (post-create): companyId, empId

Master data:
- Category: hierarchical tree via ParentId + TreePath; serve both flat and tree projections.
- AdministrativeUnit: code-based hierarchy (province/district/ward) with Level and ParentCode.
- Treat both as reference/master tables with controlled write access (admin or seed/migration only).

Cross-entity conventions:
- Standard audit columns on all phase tables: createdAt, createdBy, updatedAt, updatedBy.
- Soft-delete/status-first strategy preferred over hard delete for traceability.
- Normalize ID strategy now (see section 5) before relationships spread to RFP/Bid domains.

## 3) API design recommendations for phase 1 endpoints

API shape:
- Keep admin surface explicit (e.g., /api/admin/companies, /api/admin/employees, /api/admin/master-data/*) to simplify policy mapping.
- Use resource-oriented paths and avoid action verbs except lifecycle transitions.

Suggested endpoints by requirement:
- COMP-01: POST /api/admin/companies
- COMP-02: PATCH /api/admin/companies/{companyId}
- EMP-01: POST /api/admin/companies/{companyId}/employees
- EMP-02: GET /api/admin/companies/{companyId}/employees, GET /api/admin/employees/{employeeId}
- EMP-03: PATCH /api/admin/employees/{employeeId}
- MSTR-01: GET /api/master-data/categories, GET /api/master-data/categories/tree
- MSTR-02: GET /api/master-data/admin-units?level=&parentCode=

Behavioral recommendations:
- Return stable envelopes including id, status, audit metadata.
- Support pagination and filtering on list endpoints from day one.
- Enforce optimistic concurrency on update endpoints (rowVersion/etag or updatedAt check).
- Centralize validation:
  - Required legal fields for company create/update
  - Employee email/phone format and company existence checks
  - Master-data query params constrained by known level values

## 4) RBAC and audit trail strategy (admin/buyer/vendor boundaries)

RBAC baseline for phase 1:
- Admin: full access to company/employee/master-data write + read
- Buyer: read-only where needed later; no company/employee/master write in phase 1
- Vendor: no access to admin control-plane endpoints
- SuperAdmin (if retained): bootstrap and elevated management

Policy model:
- Role-based coarse gate first (Admin/Buyer/Vendor).
- Add ownership/scope checks as second gate for future buyer/vendor domains.
- Reject by default: endpoints require explicit policy, never implicit allow.

Audit strategy:
- Mandatory write audit capture:
  - who: actor user id, actor role, actor company id (if applicable)
  - when: UTC timestamp
  - what: entity type, entity id, operation (create/update/status-change)
- Minimum implementation:
  - Automatic metadata stamping in persistence layer
  - Structured audit event log per write transaction
- Phase 1 should establish one reusable audit mechanism that later RFP/Bid/Contract modules inherit.

## 5) Data integrity and migration notes (int vs objectId mismatch risks)

Current risk:
- Schema mixes int PK/FK and objectId fields across related entities (e.g., Category objectId while RFP references CategoryId int; user refs as objectId while employee/company keys are int).

Why this is dangerous:
- FK enforcement becomes partial or impossible.
- Query joins and indexes become inconsistent.
- DTO contracts drift (string/objectId in one endpoint, int in another).
- Migration cost multiplies after phase 2+ entities depend on these keys.

Recommendation:
- Pick one canonical identifier type per bounded context now.
- For this project baseline (.NET + EF + MySQL), int/long (or GUID) is cleaner than objectId emulation.
- If objectId must remain for compatibility, isolate it:
  - internal PK type canonical
  - external legacy id as alternate key with mapping table

Migration approach:
- Phase 1 migration slice should include:
  - Key normalization migration for company/employee/master tables
  - Backfill scripts for mismatched FK columns
  - Constraint creation after backfill (unique + FK + check constraints)
- Add integrity guardrails:
  - NOT NULL on required FK/audit columns
  - FK with restrict/cascade rules explicitly defined
  - Check constraints for status enums and hierarchy levels

## 6) Risks and mitigations

Risk: ID-type inconsistency causes broken relations in phase 2+
- Mitigation: decide canonical ID policy in first migration; block new tables from violating it.

Risk: RBAC rules are endpoint-local and drift over time
- Mitigation: centralized policy registry + endpoint policy tests per route.

Risk: audit fields captured inconsistently by developers
- Mitigation: persistence interceptor/base entity pattern auto-populating audit fields.

Risk: master-data hierarchy corruption (cycles, invalid parent)
- Mitigation: validation rules for ParentId/ParentCode, cycle checks in write path, periodic integrity job.

Risk: overly broad admin endpoints leak to buyer/vendor clients
- Mitigation: explicit route namespace + deny-by-default authorization + role matrix tests.

## 7) Suggested plan slices (2-4 executable plan chunks)

1. Slice A: Identity and company/employee core (COMP-01/02, EMP-01/02/03)
- Define normalized entities + migrations
- Implement create/update/list/detail endpoints
- Add field validation and uniqueness checks
- Add repository/service layer tests for CRUD and status transitions

2. Slice B: Master data read models (MSTR-01, MSTR-02)
- Create/seed category + administrative unit structures
- Implement flat/tree/category and admin-unit query endpoints
- Add caching/read-optimized projections if needed
- Add hierarchy integrity tests

3. Slice C: RBAC enforcement layer (AUD-02)
- Define role-policy matrix (admin/buyer/vendor)
- Apply policies to all phase 1 endpoints
- Add unauthorized/forbidden test coverage for each route-role pair
- Add policy regression checklist for future endpoints

4. Slice D: Audit foundation (AUD-01)
- Implement automatic created/updated actor + timestamp stamping
- Emit structured audit events for all writes
- Add traceability tests ensuring every mutation records actor/time/entity/action
- Document audit event schema for later workflow domains

## 8) Validation architecture hints for Nyquist validation

Since Nyquist validation is enabled, keep validation lightweight-per-task and strict-at-wave gates.

Suggested test layers:
- Unit tests:
  - DTO/command validation
  - status transition guards for company/employee
  - hierarchy validation helpers (category/admin-unit)
- Integration/API tests:
  - endpoint behavior by role (admin allowed, buyer/vendor denied)
  - create/update/list/detail happy paths + conflict cases
  - audit metadata persistence on every write endpoint
- Schema/integrity checks:
  - migration tests for FK/unique/check constraints
  - seed integrity checks for master data hierarchy

Sampling cadence:
- Per task commit: run targeted unit + endpoint tests for changed module.
- Per slice merge: run full phase-1 API + RBAC + audit integration suite.
- Phase gate: full suite green with requirement-to-test traceability map for COMP/EMP/MSTR/AUD IDs.

Nyquist-oriented quality gates:
- Every requirement ID in phase has at least one automated test assertion.
- Every protected endpoint has at least one negative authorization test.
- Every write operation has explicit audit assertion (actor + timestamp + operation).
