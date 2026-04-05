# Requirements: RFP Procurement And Contract Workflow

**Defined:** 2026-04-04
**Core Value:** Enable a complete and auditable Source-to-Contract workflow from RFP creation to signed contract with clear vendor competition.

## Source Of Truth

- Bootstrap/config reference: `BASE_OLD_PROJECT.md`
- Domain/schema reference: `DATABASE.md`
- Diagram references: `RfpDiagram.png`, `RfpDiagram.drawio.xml`, `RfpDiagram.decoded.xml`

## v1 Requirements

### Company And Employee Management

- [x] **COMP-01**: Admin can create company profile with legal and contact information.
- [x] **COMP-02**: Admin can update company profile and status.
- [x] **EMP-01**: Admin can create employee under a company.
- [x] **EMP-02**: Admin can list and retrieve employee details.
- [x] **EMP-03**: Admin can update employee status and profile fields.

### Master Data

- [x] **MSTR-01**: System stores and serves category hierarchy for RFP classification.
- [x] **MSTR-02**: System stores and serves administrative units for address input.

### RFP Management

- [x] **RFP-01**: Buyer can create RFP with title, description, budget range, deadline, category.
- [x] **RFP-02**: Buyer can create RFP items with quantity and unit.
- [x] **RFP-03**: Buyer can create item specs with typed values (text, number, boolean).
- [x] **RFP-04**: Buyer can upload and manage RFP attachments.
- [x] **RFP-05**: Buyer can list RFPs and view RFP detail.

### Vendor Participation

- [x] **VEND-01**: Buyer can invite vendor companies to an RFP.
- [x] **VEND-02**: System tracks invite and response timestamps per vendor.

### Bid Submission And Comparison

- [x] **BID-01**: Vendor can submit bid for an invited RFP.
- [x] **BID-02**: Vendor can submit bid items mapped to RFP items.
- [x] **BID-03**: Vendor can submit bid item specs with typed values.
- [x] **BID-04**: Buyer can retrieve bids by RFP.
- [x] **BID-05**: Buyer can compare bid details against original RFP item/spec requirements.

### Finalization And Contract

- [x] **FIN-01**: Buyer can finalize one winning bid for an RFP.
- [x] **FIN-02**: System stores finalized line items, prices, totals, and currency snapshot.
- [x] **FIN-03**: Buyer can create contract from finalized bid.
- [x] **FIN-04**: Buyer can sign contract.
- [x] **FIN-05**: Vendor can sign or decline contract.
- [x] **FIN-06**: System records contract signing timestamps and status transitions.

### Security, Audit, And Integrity

- [x] **AUD-01**: System records create/update actor and timestamps for all transactional entities.
- [x] **AUD-02**: API authorization enforces role boundaries (admin, buyer, vendor).
- [x] **AUD-03**: Status transitions follow allowed workflow order.
- [x] **AUD-04**: Referential integrity is enforced across all parent-child entities.

## v2 Requirements

### Workflow Enhancements

- **WF-01**: Contract negotiation cycle with revision history.
- **WF-02**: Multi-round bidding per vendor.

### Collaboration And Notifications

- **NTF-01**: In-app and email notifications for invite, bid, finalize, and sign events.
- **NTF-02**: Escalation reminders for deadline and pending signature.

### Reporting

- **RPT-01**: Dashboard for conversion funnel (RFP to signed contract).
- **RPT-02**: Procurement analytics by category, vendor, and cycle time.

## Out of Scope

| Feature | Reason |
|---------|--------|
| Real-time chat and negotiation thread | Not required for first source-to-contract release |
| Advanced BI dashboards | Focus v1 on transaction workflow correctness |
| Mobile-native app | Web/API-first delivery for faster validation |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| COMP-01 | Phase 1 | Complete |
| COMP-02 | Phase 1 | Complete |
| EMP-01 | Phase 1 | Complete |
| EMP-02 | Phase 1 | Complete |
| EMP-03 | Phase 1 | Complete |
| MSTR-01 | Phase 1 | Complete |
| MSTR-02 | Phase 1 | Complete |
| RFP-01 | Phase 2 | Complete |
| RFP-02 | Phase 2 | Complete |
| RFP-03 | Phase 2 | Complete |
| RFP-04 | Phase 2 | Complete |
| RFP-05 | Phase 2 | Complete |
| VEND-01 | Phase 3 | Complete |
| VEND-02 | Phase 3 | Complete |
| BID-01 | Phase 3 | Complete |
| BID-02 | Phase 3 | Complete |
| BID-03 | Phase 3 | Complete |
| BID-04 | Phase 4 | Complete |
| BID-05 | Phase 4 | Complete |
| FIN-01 | Phase 5 | Complete |
| FIN-02 | Phase 5 | Complete |
| FIN-03 | Phase 6 | Complete |
| FIN-04 | Phase 6 | Complete |
| FIN-05 | Phase 6 | Complete |
| FIN-06 | Phase 6 | Complete |
| AUD-01 | Phase 1 | Complete |
| AUD-02 | Phase 1 | Complete |
| AUD-03 | Phase 5 | Complete |
| AUD-04 | Phase 2 | Complete |

**Coverage:**
- v1 requirements: 29 total
- Mapped to phases: 29
- Unmapped: 0
- Complete: 29
- Pending: 0

---
*Requirements defined: 2026-04-04*
*Last updated: 2026-04-05 after completing Phase 6 contract lifecycle and signatures*
