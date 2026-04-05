# RFP Procurement And Contract Workflow

## What This Is

A multi-company procurement platform where a buyer company creates RFPs, invites vendors, receives bids, finalizes a winning bid, and generates a contract for digital signing. The system supports role-based operations for admin, buyer, and vendor actors, with full traceability from RFP item specs to bid item specs and signed contract.

## Core Value

Enable a complete and auditable Source-to-Contract workflow from RFP creation to signed contract with clear vendor competition.

## Initialization References

- Primary bootstrap config: `BASE_OLD_PROJECT.md` (section: "Cau hinh tao du an moi (RFP)")
- Business/domain source: `DATABASE.md`
- Diagram sources: `RfpDiagram.png`, `RfpDiagram.drawio.xml`, `RfpDiagram.decoded.xml`

## Requirements

### Validated

(None yet - ship to validate)

### Active

- [ ] Company and employee management for buyer and vendor organizations
- [ ] End-to-end RFP workflow: create, invite vendors, receive bids, finalize, contract
- [ ] Structured item/spec comparison between RFP requirements and vendor bid proposals

### Out of Scope

- Real-time chat/negotiation module - not required for initial source-to-contract flow
- Advanced analytics/dashboarding - can be added after workflow stabilization

## Context

The domain model includes organizations, employees, RFP, RFP items/specs, bid items/specs, finalization, vendor confirmation, and contracts with signing states. API directions are already outlined for App and Admin contexts, with explicit endpoints for create/get/invite/finalize/sign/decline actions. The data model currently mixes int and objectId identifiers, indicating a need for ID strategy alignment during implementation.

## Constraints

- **Domain Workflow**: Must preserve the sequence Create RFP -> Invite -> Bid -> Finalize -> Contract -> Sign - this is the core business process.
- **Data Integrity**: Child records (items/specs/bid items/finalize items) must maintain strict FK consistency - financial and legal correctness depends on it.
- **Role Separation**: Buyer/Admin/Vendor API capabilities must stay separated - avoids unauthorized state changes.
- **Auditability**: Status and timestamp fields must be maintained across all core entities - required for dispute handling and tracking.
- **Implementation Baseline**: New work must follow bootstrap settings in BASE_OLD_PROJECT.md and active GSD config in .planning/config.json.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Use RFP as central aggregate root for procurement flow | Most entities reference RfpId directly or transitively | - Pending |
| Keep structured spec model (ValueText/ValueNumber/ValueBoolean) for both RFP and bid | Supports normalized comparison without schema explosion | - Pending |
| Model contract signing state in dedicated fields on contract | Signing lifecycle is legally important and needs explicit timestamps | - Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition**:
1. Requirements invalidated? -> Move to Out of Scope with reason.
2. Requirements validated? -> Move to Validated with phase reference.
3. New requirements emerged? -> Add to Active.
4. Decisions to log? -> Add to Key Decisions.
5. "What This Is" still accurate? -> Update if drifted.

**After each milestone**:
1. Full review of all sections.
2. Core Value check - still the right priority?
3. Audit Out of Scope - reasons still valid?
4. Update Context with current state.

---
*Last updated: 2026-04-04 after initialization from DATABASE.md*
