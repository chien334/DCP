# Phase 8: API Integration And Data Loading - Research

**Researched:** 2026-04-05  
**Domain:** Blazor Server UI integration with existing Minimal API endpoints  
**Confidence:** HIGH

## Objective

Integrate existing buyer/vendor Blazor pages with existing backend APIs so that Phase 7 placeholder UIs become functional workflows with enforced role boundaries and complete contract lifecycle behavior. [VERIFIED: .planning/ROADMAP.md][VERIFIED: src/ProcureFlow.Web/Components/Pages/Buyer/*.razor][VERIFIED: src/ProcureFlow.Web/Components/Pages/Vendor/*.razor]

Phase 8 should prioritize wiring and data-loading correctness over visual redesign: load list/detail state from API, submit forms to existing endpoints, and handle business-rule responses (404/409/422/403) consistently in the UI. [VERIFIED: src/ProcureFlow.Web/Endpoints/Buyer/*.cs][VERIFIED: src/ProcureFlow.Web/Endpoints/Vendor/*.cs]

## Current Gaps From Phase 7 TODO Placeholders

1. Buyer and vendor dashboard metrics are hardcoded and not loaded from API. [VERIFIED: src/ProcureFlow.Web/Components/Pages/Buyer/BuyerDashboard.razor][VERIFIED: src/ProcureFlow.Web/Components/Pages/Vendor/VendorDashboard.razor]
2. Buyer pages have TODO API hooks but currently render static table/card data. [VERIFIED: src/ProcureFlow.Web/Components/Pages/Buyer/RfpListPage.razor][VERIFIED: src/ProcureFlow.Web/Components/Pages/Buyer/RfpDetailPage.razor][VERIFIED: src/ProcureFlow.Web/Components/Pages/Buyer/RfpCreatePage.razor][VERIFIED: src/ProcureFlow.Web/Components/Pages/Buyer/RfpFinalizeModal.razor][VERIFIED: src/ProcureFlow.Web/Components/Pages/Buyer/ContractViewPage.razor]
3. Vendor pages have TODO API hooks but currently render static table/form data. [VERIFIED: src/ProcureFlow.Web/Components/Pages/Vendor/RfpInviteListPage.razor][VERIFIED: src/ProcureFlow.Web/Components/Pages/Vendor/BidCreatePage.razor][VERIFIED: src/ProcureFlow.Web/Components/Pages/Vendor/BidEditPage.razor][VERIFIED: src/ProcureFlow.Web/Components/Pages/Vendor/ContractSignPage.razor]
4. Several TODO endpoint paths are incorrect vs actual backend routes:
   - UI references `/api/vendor/invites` but no such endpoint exists. [VERIFIED: src/ProcureFlow.Web/Components/Pages/Vendor/RfpInviteListPage.razor][VERIFIED: src/ProcureFlow.Web/Endpoints/Vendor/*.cs]
   - UI references `/api/vendor/contracts/{contractId}` GET but only sign/decline POST exist. [VERIFIED: src/ProcureFlow.Web/Components/Pages/Vendor/ContractSignPage.razor][VERIFIED: src/ProcureFlow.Web/Endpoints/Vendor/VendorContractEndpoints.cs]
   - UI references `/api/buyer/contracts/{contractId}` load endpoint, but buyer read endpoint is `/api/buyer/rfps/{rfpId}/contract`. [VERIFIED: src/ProcureFlow.Web/Components/Pages/Buyer/ContractViewPage.razor][VERIFIED: src/ProcureFlow.Web/Endpoints/Buyer/RfpContractEndpoints.cs]
   - UI references vendor bid update PUT endpoint, but backend currently provides create/list/detail only for vendor bids. [VERIFIED: src/ProcureFlow.Web/Components/Pages/Vendor/BidEditPage.razor][VERIFIED: src/ProcureFlow.Web/Endpoints/Vendor/BidEndpoints.cs]
5. No shared API client/service layer exists in the web UI project yet (no `HttpClient` usage in components). [VERIFIED: src/ProcureFlow.Web/**/*]

## API Mapping Matrix (UI Page -> Endpoint -> DTOs -> Auth Role)

| UI Page | Endpoint(s) | DTOs | Auth Role Boundary | Integration Notes |
|---|---|---|---|---|
| `/buyer/rfps` | `GET /api/buyer/rfps?companyId=&status=&page=&pageSize=` | `RfpListResponse`, `RfpListItem` | Buyer/Admin only | Replace placeholder rows with API paging/filtering. [VERIFIED: src/ProcureFlow.Web/Endpoints/Buyer/RfpEndpoints.cs] |
| `/buyer/rfps/{RfpId}` | `GET /api/buyer/rfps/{rfpId}`; `GET /api/buyer/rfps/{rfpId}/invites`; `GET /api/buyer/rfps/{rfpId}/bids` | `RfpDetailResponse`, `VendorInviteDto`, `BidListItem` | Buyer/Admin only | Compose page from 3 calls: RFP aggregate + invite list + bids table. [VERIFIED: src/ProcureFlow.Web/Endpoints/Buyer/RfpEndpoints.cs][VERIFIED: src/ProcureFlow.Web/Endpoints/Buyer/VendorInviteEndpoints.cs][VERIFIED: src/ProcureFlow.Web/Endpoints/Buyer/RfpBidReviewEndpoints.cs] |
| `/buyer/rfps/create` | `GET /api/master-data/categories`; optional `GET /api/master-data/admin-units`; `POST /api/buyer/rfps` | `CategoryFlatResponse`, `AdministrativeUnitsResponse`, `CreateRfpRequest` | Categories/AdminUnits: Buyer/Admin. Create RFP: Buyer/Admin | Build strongly typed form model to match nested `CreateRfpRequest` item/spec/attachment shape. [VERIFIED: src/ProcureFlow.Web/Endpoints/MasterData/CategoriesEndpoints.cs][VERIFIED: src/ProcureFlow.Web/Endpoints/MasterData/AdministrativeUnitsEndpoints.cs][VERIFIED: src/ProcureFlow.Web/Endpoints/Buyer/RfpEndpoints.cs] |
| `/buyer/rfps/{RfpId}/finalize` | `GET /api/buyer/rfps/{rfpId}/bids`; `POST /api/buyer/rfps/{rfpId}/finalize`; optional `GET /api/buyer/rfps/{rfpId}/finalize` | `BidListItem`, `FinalizeBidRequest`, `FinalizeDetailResponse` | Buyer/Admin only | After POST success, route to contract flow; surface `409` (already finalized) and `422` errors inline. [VERIFIED: src/ProcureFlow.Web/Endpoints/Buyer/RfpFinalizeEndpoints.cs] |
| `/buyer/contracts/{ContractId}` | `POST /api/buyer/contracts/{contractId}/sign` plus read by `GET /api/buyer/rfps/{rfpId}/contract` | `BuyerSignContractRequest`, `ContractDetailResponse` | Buyer/Admin only | Route model currently uses `ContractId`; page needs `RfpId` context or a new contract lookup endpoint. [VERIFIED: src/ProcureFlow.Web/Components/Pages/Buyer/ContractViewPage.razor][VERIFIED: src/ProcureFlow.Web/Endpoints/Buyer/RfpContractEndpoints.cs] |
| `/vendor/invites` | Missing vendor-scoped invite list endpoint in current API | N/A (missing contract) | Should be Vendor/Admin | Add `GET /api/vendor/invites?companyId=` (or equivalent) before UI wiring can complete. [VERIFIED: src/ProcureFlow.Web/Endpoints/Vendor/*.cs][ASSUMED] |
| `/vendor/bids/create/{RfpId}` | Data preload via buyer detail endpoint is role-incompatible; submit uses `POST /api/vendor/bids` | `CreateBidRequest`, `CreateBidItemRequest`, `CreateBidItemSpecRequest` | Vendor/Admin for POST; buyer RFP detail endpoint is Buyer/Admin only | Add vendor-safe RFP detail endpoint or vendor RFP snapshot endpoint for bid form preload. [VERIFIED: src/ProcureFlow.Web/Endpoints/Buyer/RfpEndpoints.cs][VERIFIED: src/ProcureFlow.Web/Endpoints/Vendor/BidEndpoints.cs][ASSUMED] |
| `/vendor/bids/{BidId}/edit` | `GET /api/vendor/bids/{bidId}` exists; update endpoint missing | `BidDetailResponse` for load; update DTO missing | Vendor/Admin | Add `PUT/PATCH /api/vendor/bids/{bidId}` or limit page to read-only details. [VERIFIED: src/ProcureFlow.Web/Endpoints/Vendor/BidEndpoints.cs][ASSUMED] |
| `/vendor/contracts/{ContractId}` | `POST /api/vendor/contracts/{contractId}/sign`; `POST /api/vendor/contracts/{contractId}/decline`; read endpoint missing | `VendorActionRequest` for POSTs; read DTO missing | Vendor/Admin | Add vendor contract detail GET endpoint for page preload and status refresh after sign/decline. [VERIFIED: src/ProcureFlow.Web/Endpoints/Vendor/VendorContractEndpoints.cs][ASSUMED] |
| `/buyer/dashboard` and `/vendor/dashboard` | Composite metrics from existing list endpoints | Existing list DTOs | Role-specific | Implement dashboard aggregation in UI service layer first (no new API strictly required). [VERIFIED: src/ProcureFlow.Web/Endpoints/Buyer/*.cs][VERIFIED: src/ProcureFlow.Web/Endpoints/Vendor/*.cs][ASSUMED] |

## Role Boundaries And Contract Lifecycle Paths

### Role boundaries to preserve

- Buyer/UI pages must only call `/api/buyer/*` and `/api/master-data/*` endpoints. [VERIFIED: src/ProcureFlow.Web/Program.cs]
- Vendor/UI pages must only call `/api/vendor/*` endpoints; avoid accidental dependency on buyer-only read endpoints. [VERIFIED: src/ProcureFlow.Web/Program.cs]
- Admin is permitted in both buyer/vendor groups by policy and can be used for ops support/testing paths. [VERIFIED: src/ProcureFlow.Web/Program.cs]

### Contract lifecycle UI path (must stay in workflow order)

1. Buyer finalizes winning bid: `POST /api/buyer/rfps/{rfpId}/finalize`. [VERIFIED: src/ProcureFlow.Web/Endpoints/Buyer/RfpFinalizeEndpoints.cs]
2. Buyer creates contract: `POST /api/buyer/rfps/{rfpId}/contract`. [VERIFIED: src/ProcureFlow.Web/Endpoints/Buyer/RfpContractEndpoints.cs]
3. Buyer signs contract: `POST /api/buyer/contracts/{contractId}/sign`. [VERIFIED: src/ProcureFlow.Web/Endpoints/Buyer/RfpContractEndpoints.cs]
4. Vendor signs or declines: `POST /api/vendor/contracts/{contractId}/sign` or `POST /api/vendor/contracts/{contractId}/decline`. [VERIFIED: src/ProcureFlow.Web/Endpoints/Vendor/VendorContractEndpoints.cs]

## Risks And Mitigation

| Risk | Impact | Mitigation |
|---|---|---|
| UI routes expect endpoints that do not exist (vendor invites list, vendor contract detail, vendor bid update). | Phase 8 blocked mid-integration. | Treat endpoint-gap closure as explicit Plan 1 scope; implement minimal missing endpoints before page wiring. [VERIFIED: src/ProcureFlow.Web/Components/Pages/Vendor/*.razor][VERIFIED: src/ProcureFlow.Web/Endpoints/Vendor/*.cs] |
| Buyer contract page route key (`ContractId`) does not match available buyer read endpoint key (`rfpId`). | Contract detail load fails or requires fragile lookup hacks. | Choose one contract lookup strategy early: add `GET /api/buyer/contracts/{contractId}` or route page by `RfpId`. [VERIFIED: src/ProcureFlow.Web/Components/Pages/Buyer/ContractViewPage.razor][VERIFIED: src/ProcureFlow.Web/Endpoints/Buyer/RfpContractEndpoints.cs][ASSUMED] |
| Vendor bid create page currently plans to read `/api/rfps/{rfpId}` without role-safe endpoint. | Vendor cannot load bid form data under current RBAC. | Add vendor-scoped RFP snapshot read endpoint and test with vendor role headers. [VERIFIED: src/ProcureFlow.Web/Components/Pages/Vendor/BidCreatePage.razor][VERIFIED: src/ProcureFlow.Web/Program.cs][ASSUMED] |
| Role leakage from shared UI services (buyer page accidentally calling vendor API or reverse). | 403 errors and security regressions. | Create role-scoped API service interfaces (`IBuyerApi`, `IVendorApi`, `IMasterDataApi`) and keep page injection explicit. [ASSUMED] |
| Workflow status conflicts (`409` and `422`) not surfaced in UI. | User confusion and duplicate actions. | Standardize API error envelope handling and render state-aware inline messages on finalize/sign pages. [VERIFIED: src/ProcureFlow.Web/Endpoints/Buyer/RfpFinalizeEndpoints.cs][VERIFIED: src/ProcureFlow.Web/Endpoints/Buyer/RfpContractEndpoints.cs][VERIFIED: src/ProcureFlow.Web/Endpoints/Vendor/VendorContractEndpoints.cs][ASSUMED] |

## Recommended Plan Split (3 Executable Plans)

### Plan 08-01: Integration Foundation + Missing API Contract Closure

**Goal:** Remove blocking contract mismatches and add shared client foundation.

**Scope:**
- Add role-scoped API client services and typed DTO adapters in web UI project. [ASSUMED]
- Add missing vendor-facing read/update endpoints required by existing pages:
  - vendor invite list endpoint
  - vendor contract detail GET endpoint
  - vendor bid update endpoint (or intentionally de-scope edit page behavior)
- Resolve buyer contract detail lookup gap (`contractId` vs `rfpId`).

**Done when:** all page-level TODO endpoints are resolvable and callable under correct role policy.

### Plan 08-02: Buyer Workflow Data Loading And Actions

**Goal:** Fully wire buyer pages to API calls with proper loading/error states.

**Scope:**
- Wire buyer dashboard, list, detail, create, finalize, and contract pages.
- Load categories/admin-units for create form and submit nested `CreateRfpRequest` payload.
- Implement finalize->contract sequence UI transitions and disable invalid actions by status.
- Map API conflict/validation responses to user-visible messages.

**Done when:** Buyer can run create RFP -> view detail -> finalize -> create/sign contract in UI backed by live API.

### Plan 08-03: Vendor Workflow Data Loading And Contract Actions

**Goal:** Fully wire vendor pages to API calls with role-safe lifecycle transitions.

**Scope:**
- Wire vendor dashboard and invites list to vendor-scoped data.
- Wire bid create/edit pages to vendor endpoints and preload data through vendor-safe RFP context.
- Wire contract sign/decline page, refresh status after action, and block repeated actions.
- Add integration tests covering vendor role boundaries for all newly added endpoints.

**Done when:** Vendor can complete invite->bid->contract sign/decline using UI with real backend state.

## Verification Strategy (Build + Integration + UAT)

### Build verification

- `dotnet build ProcureFlow.sln` [VERIFIED: environment dotnet 8.0.403]
- `dotnet test ProcureFlow.sln --no-build` (fast regression after first full build) [VERIFIED: tests discovered via list-tests]

### Integration verification (automated)

Run focused suites first, then full suite:

1. `dotnet test tests/ProcureFlow.Web.IntegrationTests/ProcureFlow.Web.IntegrationTests.csproj --filter "FullyQualifiedName~VendorBidEndpointsTests"`
2. `dotnet test tests/ProcureFlow.Web.IntegrationTests/ProcureFlow.Web.IntegrationTests.csproj --filter "FullyQualifiedName~ContractLifecycleEndpointsTests"`
3. `dotnet test tests/ProcureFlow.Web.IntegrationTests/ProcureFlow.Web.IntegrationTests.csproj --filter "FullyQualifiedName~RbacAndAuditTests"`
4. `dotnet test ProcureFlow.sln`

Existing integration infrastructure is already present with role-header testing via `X-Role` and `X-User-Id`, so Phase 8 should extend this pattern for new endpoints and page-action paths. [VERIFIED: tests/ProcureFlow.Web.IntegrationTests/**/*.cs]

### UAT verification (manual, role-specific)

1. Buyer role UAT:
   - Navigate dashboard and confirm metrics reflect API data.
   - Create RFP with items/specs; verify appears in list/detail.
   - Finalize a winning bid; create contract; sign contract.
2. Vendor role UAT:
   - View pending invites; open bid create from invite.
   - Submit and edit bid before finalization cutoff.
   - Open contract and perform sign and decline paths in separate scenarios.
3. RBAC UAT:
   - Confirm vendor cannot access buyer pages/actions and buyer cannot execute vendor-only actions (expect denial UX, no silent failure).
4. Contract lifecycle UAT:
   - Verify status progression Created -> PartiallySigned -> Signed, and decline terminal path.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|---|---|---|---|---|
| .NET SDK | Build and tests | Yes | 8.0.403 | None |
| xUnit + ASP.NET Core test host packages | Integration suite | Yes | Present in test csproj | None |

No blocking environment gaps were found for Phase 8 research/planning. [VERIFIED: dotnet --version][VERIFIED: tests/ProcureFlow.Web.IntegrationTests/ProcureFlow.Web.IntegrationTests.csproj]

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|---|---|---|
| A1 | Add vendor invite list endpoint as `GET /api/vendor/invites?companyId=` | API Mapping Matrix | Endpoint shape may need different query contract |
| A2 | Add vendor-safe RFP snapshot endpoint for bid preload | API Mapping Matrix | Might be possible via existing endpoint changes instead |
| A3 | Use role-scoped API services (`IBuyerApi`/`IVendorApi`/`IMasterDataApi`) in Blazor UI | Risks And Mitigation | Different DI pattern may be preferred by codebase owner |

## Sources

### Primary (HIGH confidence)

- `src/ProcureFlow.Web/Program.cs` - API group authorization boundaries
- `src/ProcureFlow.Web/Endpoints/Buyer/*.cs` - buyer endpoint contracts and DTOs
- `src/ProcureFlow.Web/Endpoints/Vendor/*.cs` - vendor endpoint contracts and DTOs
- `src/ProcureFlow.Web/Endpoints/MasterData/*.cs` - master data contracts
- `src/ProcureFlow.Web/Components/Pages/Buyer/*.razor` - buyer TODO placeholders and routes
- `src/ProcureFlow.Web/Components/Pages/Vendor/*.razor` - vendor TODO placeholders and routes
- `.planning/phases/07-buyer-vendor-ui-and-dashboards/*` - Phase 7 planned intent and completion summaries
- `.planning/ROADMAP.md`, `.planning/REQUIREMENTS.md`, `.planning/STATE.md` - roadmap and current focus
- `tests/ProcureFlow.Web.IntegrationTests/**/*.cs` - current integration verification patterns

## Metadata

**Confidence breakdown:**
- Gap analysis: HIGH (direct TODO and route/endpoint comparison)
- API matrix: HIGH (direct endpoint source inspection)
- Plan split: MEDIUM (execution sequencing recommendation)
- Verification strategy: HIGH (validated commands and existing suites)

**Research date:** 2026-04-05  
**Valid until:** 2026-05-05
