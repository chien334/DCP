# Roadmap: RFP Procurement And Contract Workflow

## Overview

This roadmap delivers an end-to-end source-to-contract platform in six phases: foundation and master data, RFP authoring, vendor participation and bid intake, bid comparison, winning bid finalization, and contract execution/signing.

## Initialization References

- Bootstrap/config reference: `BASE_OLD_PROJECT.md`
- Domain/schema reference: `DATABASE.md`
- Diagram references: `RfpDiagram.png`, `RfpDiagram.drawio.xml`, `RfpDiagram.decoded.xml`

## Fixed Workflow Constraints

- Required sequence: Create RFP -> Invite Vendor -> Submit Bid -> Finalize -> Generate Contract -> Sign
- Required data chain: RFP -> RfpItem -> RfpItemSpec; RfpBid -> RfpBidItem -> RfpBidItemSpec; RfpFinalize -> RfpContract
- Required role boundaries: Admin, Buyer, Vendor

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

- [x] **Phase 1: Foundation And Access Control** - Company, employee, master data, authorization, audit foundation.
- [x] **Phase 2: RFP Authoring Core** - Create/list/detail RFP with item/spec and attachment structures.
- [x] **Phase 3: Vendor Invite And Bid Submission** - Invite vendors and receive structured bids.
- [x] **Phase 4: Buyer Bid Review And Comparison** - Compare bids against RFP requirements and retrieve decision context.
- [x] **Phase 5: Finalization Workflow** - Select winning bid and lock finalized pricing snapshot.
- [x] **Phase 6: Contract Lifecycle And Signatures** - Create contract, sign/decline flow, final legal status tracking.

## Phase Details

### Phase 1: Foundation And Access Control
**Goal**: Establish tenant/company/employee/master data and role-based security primitives for all downstream workflow APIs.
**Depends on**: Nothing (first phase)
**Requirements**: COMP-01, COMP-02, EMP-01, EMP-02, EMP-03, MSTR-01, MSTR-02, AUD-01, AUD-02
**Success Criteria** (what must be TRUE):
1. Admin can create and update company and employee records through secured APIs.
2. Category and administrative unit data is available and queryable.
3. API access is restricted by role boundaries (admin, buyer, vendor).
**Plans**: 3 plans

Plans:
- [x] 01-01: Implement company and employee entities, repositories, and CRUD endpoints.
- [x] 01-02: Implement category and administrative unit read models.
- [x] 01-03: Implement RBAC middleware and audit metadata handling.

### Phase 2: RFP Authoring Core
**Goal**: Allow buyer to create RFP with items/specs/attachments and query RFP list/detail.
**Depends on**: Phase 1
**Requirements**: RFP-01, RFP-02, RFP-03, RFP-04, RFP-05, AUD-04
**Success Criteria** (what must be TRUE):
1. Buyer can create one RFP request containing nested items and specs in one workflow.
2. RFP attachments are linked and retrievable.
3. RFP list and detail endpoints return complete, consistent aggregate data.
**Plans**: 3 plans

Plans:
- [x] 02-01: Implement RFP aggregate model and create endpoint transaction flow.
- [x] 02-02: Implement item/spec/attachment persistence and validation rules.
- [x] 02-03: Implement RFP read APIs and aggregate mapping.

### Phase 3: Vendor Invite And Bid Submission
**Goal**: Enable invite participation tracking and vendor bid submission with bid items/specs.
**Depends on**: Phase 2
**Requirements**: VEND-01, VEND-02, BID-01, BID-02, BID-03
**Success Criteria** (what must be TRUE):
1. Buyer can invite vendors and the system tracks invite/response events.
2. Vendor can submit one bid mapped to RFP items.
3. Bid item specs support text/number/boolean typed values.
**Plans**: 3 plans

Plans:
- [x] 03-01: Implement vendor participation invite and response state model.
- [x] 03-02: Implement vendor bid create endpoint with totals and currency fields.
- [x] 03-03: Implement bid item and bid item spec persistence/validation.

### Phase 4: Buyer Bid Review And Comparison
**Goal**: Provide buyer retrieval and comparison capability across vendor bids for the same RFP.
**Depends on**: Phase 3
**Requirements**: BID-04, BID-05
**Success Criteria** (what must be TRUE):
1. Buyer can retrieve all bids under one RFP.
2. Buyer can inspect per-item and per-spec comparisons between RFP demand and vendor offer.
3. Comparison output is accurate for quantity, unit price, totals, and key specs.
**Plans**: 2 plans

Plans:
- [x] 04-01: Implement buyer bid query APIs scoped by RFP.
- [x] 04-02: Implement bid comparison projection and response contract.

### Phase 5: Finalization Workflow
**Goal**: Let buyer finalize a winning bid and lock the final commercial snapshot.
**Depends on**: Phase 4
**Requirements**: FIN-01, FIN-02, AUD-03
**Success Criteria** (what must be TRUE):
1. Buyer can create finalization record linked to selected bid.
2. Finalization captures immutable totals and item pricing snapshot.
3. Workflow status changes enforce allowed transitions only.
**Plans**: 2 plans

Plans:
- [x] 05-01: Implement finalization endpoint and domain invariants.
- [x] 05-02: Implement finalization item snapshot generation and validation.

### Phase 6: Contract Lifecycle And Signatures
**Goal**: Generate contract from finalization and complete buyer/vendor signing flow.
**Depends on**: Phase 5
**Requirements**: FIN-03, FIN-04, FIN-05, FIN-06
**Success Criteria** (what must be TRUE):
1. Buyer can create contract from finalized result.
2. Buyer and vendor signature actions update legal status and timestamp fields.
3. Vendor decline action is recorded and reflected in contract status.
**Plans**: 3 plans

Plans:
- [x] 06-01: Implement contract creation and contract retrieval APIs.
- [x] 06-02: Implement buyer/vendor sign flow with timestamp and status handling.
- [x] 06-03: Implement vendor decline flow and post-decline state rules.

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation And Access Control | 3/3 | Complete | 2026-04-05 |
| 2. RFP Authoring Core | 3/3 | Complete | 2026-04-05 |
| 3. Vendor Invite And Bid Submission | 3/3 | Complete | 2026-04-05 |
| 4. Buyer Bid Review And Comparison | 2/2 | Complete | 2026-04-05 |
| 5. Finalization Workflow | 2/2 | Complete | 2026-04-05 |
| 6. Contract Lifecycle And Signatures | 3/3 | Complete | 2026-04-05 |
