# GTAS VPP — Architecture Decision Record index

- Baseline date: 2026-07-15 (Asia/Ho_Chi_Minh)
- Status: all decisions `D-001..D-012` are accepted for the thesis release.
- Decision register: [`03-DECISIONS-REQUIRED.md`](../planning/03-DECISIONS-REQUIRED.md)
- Execution plan: [`04-MASTER-IMPLEMENTATION-PLAN.md`](../planning/04-MASTER-IMPLEMENTATION-PLAN.md)

The ADR is the normative decision. The master plan owns execution order and task status. “Direct tasks” are decision gates; downstream tasks consume the decision but are not independently blocked once the direct contract is accepted.

| Decision | ADR | Direct tasks | Downstream / cutline consumers |
|---|---|---|---|
| D-001 | [ADR-001](ADR-001-app-owned-identity-gtas-menu-cutover.md) | AUTH-003, AUTH-006 | AUTH-004, UI-004, DOC-002 |
| D-002 | [ADR-002](ADR-002-single-company-v1.md) | AUTH-001, AUTH-002, PER-001 | SET-002, REPORT-001 |
| D-003 | [ADR-003](ADR-003-per-user-request-whole-company-basket.md) | PER-001, REQ-001 | REPORT-001, UI-003 |
| D-004 | [ADR-004](ADR-004-flat-rbac-separation-of-duties.md) | AUTH-001, AUTH-002, SUP-001, SET-002, UI-004, UI-005 | QA-003 |
| D-005 | [ADR-005](ADR-005-supplement-quota-approval.md) | PER-001, SUP-001, UI-005 | REPORT-001 |
| D-006 | [ADR-006](ADR-006-whole-basket-primary-supplier.md) | PRICE-001, SET-001 | SET-002, REPORT-001, REPORT-002, UI-006 |
| D-007 | [ADR-007](ADR-007-net-vat-price-snapshot.md) | PRICE-001, SET-001, REPORT-001 | SET-002, REPORT-002, DOC-002 |
| D-008 | [ADR-008](ADR-008-registration-pending-approval-recovery.md) | AUTH-005, UI-007 | NOTIF-001 activation event, QA-003 |
| D-009 | [ADR-009](ADR-009-independent-rebrand-data-anonymization.md) | UI-002, DOC-002 | DOC-003, REL-001 |
| D-010 | [ADR-010](ADR-010-in-app-email-csv-excel.md) | REPORT-003, NOTIF-002, NOTIF-003 | DOC-002; REPORT-004 deferred |
| D-011 | [ADR-011](ADR-011-production-demo-seed-secret-response.md) | SEC-001, DEP-002 | REL-001; SEC-002/DEP-001 remain local-capable |
| D-012 | [ADR-012](ADR-012-a-plus-release-cutline-2026-08-15.md) | DOC-002, REL-001 | A+ mandatory cutline and checkpoint gates |

No ADR authorizes access to DigitalOcean, a real database, credentials, or production. External execution still requires the authority stated by the relevant task card.
