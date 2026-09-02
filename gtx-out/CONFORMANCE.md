# Conformance report

Types are computed from the legacy field definitions. Keys, dates and field
liveness are derived from the source code, with a citation each. The design
step may name and group things; it may **not** change a type, invent a key,
promote a field to a date without proof, or drop a live field.

Nothing here asks you to confirm domain internals. Every item is either a
derived fact or an architectural decision with the evidence attached.

**Status: PASS with type corrections applied**

## Fixed-width fields stored as varchar (2)

These legacy fields are fixed width and space padded. They are modelled as
`varchar(n)` of the same width rather than `char(n)`.

This is a DECISION, and the one place this stage deliberately does not
mirror the legacy storage shape. The feature is what has to survive the
move; the storage format is an ETL concern and data converts between the
two. In exchange the entity stays plain JPA — the faithful alternative
needs `@JdbcTypeCode(SqlTypes.CHAR)` on every such field, because Hibernate
compares JDBC type codes when it validates the mapping and rejects a String
field over a `char` column outright.

**One consequence for whoever loads the legacy data:** PostgreSQL `char`
comparison ignores trailing spaces and `varchar` does not, so a legacy
extract must be TRIMMED on the way in, or `'Y '` will not equal `'Y'`.
Values written by the new code are already unpadded.

| Field | Legacy storage | Modelled as |
|---|---|---|
| `ACCT-ACTIVE-STATUS` | `char(1)` | `varchar(1)` |
| `TRAN-TYPE-CD` | `char(2)` | `varchar(2)` |

## Types corrected (10)

| Entity | Property | Source | Design proposed | Enforced |
|---|---|---|---|---|
| Account | `currentBalance` | `ACCT-CURR-BAL` | `numeric(11,2)` / `BigDecimal` | **`numeric(12,2)` / `BigDecimal`** |
| Account | `creditLimit` | `ACCT-CREDIT-LIMIT` | `numeric(11,2)` / `BigDecimal` | **`numeric(12,2)` / `BigDecimal`** |
| Account | `cashCreditLimit` | `ACCT-CASH-CREDIT-LIMIT` | `numeric(11,2)` / `BigDecimal` | **`numeric(12,2)` / `BigDecimal`** |
| Account | `currentCycleCredit` | `ACCT-CURR-CYC-CREDIT` | `numeric(11,2)` / `BigDecimal` | **`numeric(12,2)` / `BigDecimal`** |
| Account | `currentCycleDebit` | `ACCT-CURR-CYC-DEBIT` | `numeric(11,2)` / `BigDecimal` | **`numeric(12,2)` / `BigDecimal`** |
| CardCrossReference | `customerId` | `XREF-CUST-ID` | `bigint` / `Long` | **`integer` / `Integer`** |
| CardCrossReference | `accountId` | `XREF-ACCT-ID` | `(none)` / `Long` | **`bigint` / `Long`** |
| Transaction | `categoryCode` | `TRAN-CAT-CD` | `integer` / `Integer` | **`smallint` / `Short`** |
| Transaction.Merchant | `id` | `TRAN-MERCHANT-ID` | `varchar(15)` / `String` | **`integer` / `Integer`** |
| Transaction | `cardNumber` | `TRAN-CARD-NUM` | `(none)` / `String` | **`varchar(16)` / `String`** |

## Date promotions accepted (5)

Each is proven a date by the code, so the promotion stands. The format is
derived from the offsets the legacy program splits the field at.

| Entity | Property | Source | Stored as | Modelled as | Format | Evidence |
|---|---|---|---|---|---|---|
| Account | `openDate` | `ACCT-OPEN-DATE` | `varchar(10)` | `date` | `yyyy-MM-dd` | COACTUPC.cbl:3413 split at fixed offsets into DAY/MON/YEAR -> format yyyy-MM-dd |
| Account | `expirationDate` | `ACCT-EXPIRAION-DATE` | `varchar(10)` | `date` | `yyyy-MM-dd` | COACTUPC.cbl:3413 split at fixed offsets into DAY/MON/YEAR -> format yyyy-MM-dd |
| Account | `reissueDate` | `ACCT-REISSUE-DATE` | `varchar(10)` | `date` | `yyyy-MM-dd` | COACTUPC.cbl:3413 split at fixed offsets into DAY/MON/YEAR -> format yyyy-MM-dd |
| Transaction | `originatedAt` | `TRAN-ORIG-TS` | `varchar(26)` | `timestamptz` | `(timestamp)` | COTRN01C.cbl:143 MOVE TRAN-ORIG-TS TO TORIGDTI (carries validated TORIGDTI) | COTRN02C.cbl:402 MOVE TORIGDTI TO TRAN-ORIG-TS (carries validated TORIGDTI) |
| Transaction | `processedAt` | `TRAN-PROC-TS` | `varchar(26)` | `timestamptz` | `(timestamp)` | COTRN01C.cbl:143 MOVE TRAN-PROC-TS TO TPROCDTI (carries validated TPROCDTI) | COTRN02C.cbl:402 MOVE TPROCDTI TO TRAN-PROC-TS (carries validated TPROCDTI) |

## Legacy vocabulary stripped from documentation (7)

A Java developer maintaining this must never need to know what a legacy
picture clause is, so these comments were sanitised before emission. The
useful half of each sentence is kept. Listed because it signals the design
prompt still leaks storage detail into prose.

| Where | Original comment |
|---|---|
| `Account.id` | PIC 9(11) - numeric account identifier, fits in bigint |
| `CardCrossReference.cardNumber` | PIC X(16) - card number is the natural key for this cross-reference |
| `CardCrossReference.customerId` | PIC 9(09) - references a customer record (customer entity is outside this feature scope) |
| `CardCrossReference.accountId` | Each card is issued against exactly one account. The cross-reference table owns this FK because it is the many side (multiple cards per account). Proven FK from XREF-ACCT-ID -> ACCOUNT-RECORD.ACCT-ID. |
| `Transaction.id` | PIC X(16) - generated by incrementing the last transaction ID. Kept as String because the legacy system treats it as a character field. |
| `Transaction.categoryCode` | Numeric category (e.g. 2 for bill payment). PIC 9(04) - genuinely numeric, no leading-zero significance in acceptance criteria. |
| `Transaction.cardNumber` | Each transaction is charged to a specific card. The proven FK is TRAN-CARD-NUM -> XREF-CARD-NUM. Account is reached via the multi-hop route through card_cross_references, NOT by a direct FK. |
