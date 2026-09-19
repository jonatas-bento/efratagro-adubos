# Legacy Commercial Import — Review Status

## Import snapshot

Commercial legacy import:

- imported source rows: 788
- review rows detected at import time: 44
- currently resolved review rows: 2
- currently open review rows: 42

`LegacyImportBatch.ReviewRows = 44` is intentionally preserved as the
historical import-time snapshot.

The current review queue is represented by
`LegacyImportRow.RequiresReview = true`.

## Automatically resolved using documentary evidence

Two malformed-date rows were resolved because another row from the same
historical transaction independently confirmed the transaction date.

Results:

- 2 new legacy `SaleItem` records
- no new `Sale`
- no new `Customer`
- no new `LegacySaleMetadata`
- no inventory changes
- no receivable changes
- no payment changes

Post-resolution invariants:

- sales: 524
- sale items: 748
- inventory movements: 67
- receivables: 3
- payments: 1
- legacy sale metadata: 522
- legacy import rows: 889
- open commercial reviews: 42
- commercial rows linked to sale items: 746
- inventory ledger total: 19183

The resolver is idempotent:

- candidates: 0
- already resolved: 2
- created items: 0
- review rows: 42 -> 42

## Remaining review queue

### Customer conflicts

- 11 historical transactions
- 33 source rows

The source contains different customer spellings inside transactions
that otherwise share transaction context.

No customer spelling was selected automatically because there is not
enough independent source evidence to establish a canonical identity.

One transaction contains substantially different customer identities
and may require historical transaction splitting rather than name
canonicalization.

### Missing or malformed field values

There are 9 remaining rows:

- 2 missing quantities
- 2 malformed sale dates
- 5 missing unit prices

No values will be reconstructed from:

- nearby rows
- similar product prices
- spelling similarity
- majority occurrence
- apparently obvious date corrections

These rows require documentary evidence or explicit confirmation from
the data owner.

## Safety rules

Legacy commercial review resolution must never create or mutate:

- `InventoryMovement`
- `Receivable`
- `Payment`

Historical commercial corrections must not change physical stock or
financial balances.

Raw source evidence remains immutable.

Resolved rows preserve:

- original `ReviewReason`
- `SaleItemId`
- `ResolvedAtUtc`
- `ResolutionNote`

## Human review material

Operational review workpapers are intentionally kept outside source
control because they contain real customer and commercial data.

Local workpapers:

- `artifacts/customer-conflict-review.tsv`
- `artifacts/field-value-review.tsv`
- `artifacts/legacy-commercial-review-status.md`

Future confirmed resolutions must be executed through an auditable,
idempotent resolver with dry-run validation before persistence.
