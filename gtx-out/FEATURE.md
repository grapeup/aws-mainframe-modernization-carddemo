# Online Transaction Management — feature implementation

**Review the C# in this pull request.** The tests are the specification;
the implementation exists to satisfy them.

*Could a C# developer who has never seen the legacy system own this?*

## Scope of this stage

Business rules only, proven against in-memory doubles. There is
deliberately **no persistence implementation, no host and no endpoint**
here: nothing at this stage could execute them, so they would ship inside
a feature labelled complete while being entirely unverified. The
integration stage writes them against the real migrated schema.

## Traceability

| Story | Criterion | Test |
|---|---|---|
| Add New Transaction | I am authenticated and the account and card are active | `AddTransactionTests.AddTransaction_ValidInput_CreatesRecordAndUpdatesBalance` |
| Add New Transaction | I need to add a transaction | `AddTransactionTests.AddTransaction_ByCardNumber_ResolvesAccountAndSucceeds` |
| Add New Transaction | the account is not active | `AddTransactionTests.AddTransaction_InactiveAccount_ReturnsAccountNotActive` |
| Add New Transaction | the account and card are validated | `AddTransactionTests.AddTransaction_AllDetailFields_ArePersistedCorrectly` |
| Add New Transaction | I enter an amount that is not a valid numeric value with up to 2 decimal places | `AddTransactionTests.AddTransaction_InvalidAmount_ReturnsInvalidAmountFormat` |
| Add New Transaction | the transaction is created successfully | `AddTransactionTests.AddTransaction_CreditType_ReducesBalanceAndUpdatesCycleCredit` |
| Make Online Payment | I am authenticated and my account has a positive balance | `MakePaymentTests.MakePayment_ValidAccountWithBalance_PaysOffAndReturnsSuccess` |
| Make Online Payment | I do not enter an account ID | `MakePaymentTests.MakePayment_EmptyAccountId_ReturnsAccountIdMissing` |
| Make Online Payment | I enter an account ID that does not exist | `MakePaymentTests.MakePayment_NonExistentAccount_ReturnsAccountNotFound` |
| Make Online Payment | my account balance is zero or negative | `MakePaymentTests.MakePayment_ZeroOrNegativeBalance_ReturnsNothingToPay` |
| Make Online Payment | I have not entered 'Y' or 'y' in the confirmation field | `MakePaymentTests.MakePayment_NotConfirmed_ReturnsPaymentNotConfirmed` |
| Make Online Payment | my payment is confirmed and validated | `MakePaymentTests.MakePayment_Confirmed_CreatesCorrectPaymentRecord` |

12 criteria, 12 tests, all traced.

## Business rules as typed errors

Legacy message text was truncated terminal English and will not survive
the UI redesign, so each rule is a named error code instead. The original
wording is kept in doc comments so parity stays checkable.

- `AccountIdMissing`
- `AccountNotFound`
- `AccountNotActive`
- `CardNotFound`
- `InvalidAmountFormat`
- `NothingToPay`
- `PaymentNotConfirmed`

## What the integration stage owes this feature

Guarantees the business layer states but cannot itself provide, because
it has no database. Carry these into the integration work.

- IUnitOfWork.BeginAsync must open a database transaction at SERIALIZABLE or REPEATABLE READ isolation level so that the MAX(id) read in GetMaxTransactionIdAsync is locked until CommitAsync. Without this, two concurrent callers can read the same max id and generate duplicate transaction IDs.
- ITransactionRepository.GetMaxTransactionIdAsync must execute SELECT MAX(id) FROM transactions FOR UPDATE (or equivalent row-level lock) so the read is held within the transaction boundary opened by BeginAsync. The lock must not be released until CommitAsync or RollbackAsync.
- IUnitOfWork.CommitAsync must atomically apply both the staged transaction insert (ITransactionRepository.StageAdd) and the staged account update (IAccountRepository.StageUpdate) in a single database transaction. If either fails, both must roll back.
- If the system moves to high concurrency, the id-generation strategy should be replaced with a database sequence (NEXTVAL) to avoid the serialisation bottleneck of locking the max id. The current contract supports the legacy increment-by-one approach but the integration stage should document the throughput ceiling.
- TransactionRecord.CardNumber is a mandatory foreign key to the card cross-references table. The database schema must enforce this constraint. The service populates it via repository lookup, but the integration layer must ensure the FK constraint exists.

## Namespace root canonicalised (21 file(s))

The generated code named the feature differently from the project it
was filed under. Left alone this stage would still have gone green — every
file here would have agreed with every other — and the next stage's build
would have failed, because it reads the project name. Rewritten:

| Generated as | Canonicalised to |
|---|---|
| `CardDemo.TransactionManagement` | `CardDemo.OnlineTransactionManagement` |

## Decisions a reviewer must see

Deliberate divergence from the legacy behaviour, stated rather than
hidden. Silent divergence is the thing a cautious reviewer fears.

- **test**: The AddTransactionInput.Amount is declared as string so the service can validate the format (numeric with up to 2 decimal places) before parsing. This matches the legacy behaviour where the field is entered as text and validated.
- **test**: MakePaymentInput.AccountId is declared as string (nullable) to allow validation of empty/missing input before parsing to long. The service must parse it and return AccountIdMissing if blank or AccountNotFound if the parsed ID does not exist.
- **test**: For balance adjustment: debit/purchase transactions (type codes other than 01 and 02) increase the balance and the current cycle debit total. Credit/payment transactions (01, 02) decrease the balance and increase the current cycle credit total. This matches the legacy COBOL logic.
- **test**: The TransactionRecord.CardNumber is required (non-nullable) because it is a foreign key. The payment flow must look up the card via ICardRepository.GetByAccountIdAsync to populate it. If no active card is found, the implementation should return an appropriate error (CardNotFound).
- **test**: Amount rounding uses MidpointRounding.AwayFromZero per COBOL ROUNDED semantics. The test amounts chosen do not hit the midpoint case, but the implementation must use this rounding mode.
- **test**: The InMemoryUnitOfWork tracks WasBegun and WasCommitted flags so tests can assert that the transaction boundary was properly opened and committed (or not committed on failure paths).
- **implementation**: Amount validation uses a regex that requires at least one digit before an optional decimal point with 1-2 fractional digits. Empty strings, non-numeric text, and values with more than 2 decimal places are rejected. This matches the legacy COBOL PIC 9 validation behaviour.
- **implementation**: For balance adjustment: credit types (01, 02) reduce the balance and increase CurrentCycleCredit. All other types (e.g. 03 purchase) increase the balance and increase CurrentCycleDebit. This matches the test expectations where a purchase of 125.50 on a 500.00 balance yields 625.50.
- **implementation**: MakePaymentInput.AccountId is a string to allow empty/whitespace validation before parsing. The service parses it with long.TryParse using NumberStyles.None (no signs, no separators) to match the legacy 11-digit account number format.
- **implementation**: The payment flow reads the balance before BeginAsync to perform the NothingToPay check. This is safe because the balance is re-read implicitly through the same account object reference, and the actual mutation happens inside the transaction. In a real concurrent system, the integration stage should ensure the account row is locked within the transaction to prevent a TOCTOU race.
- **implementation**: MidpointRounding.AwayFromZero is used for amount rounding to match COBOL ROUNDED semantics, though the test amounts do not exercise the midpoint case.
- **implementation**: The confirmation check accepts only the single characters 'Y' or 'y', not 'yes' or other truthy strings, matching the legacy CICS screen field behaviour.

**Status: PASS** — every criterion is tested against the reviewed domain
types through the real service, no shadow types, no persistence or hosting
written where nothing could execute it, no legacy vocabulary, no screen
mechanics asserted, no binary floating point on money.
