# Online Transaction Management — feature implementation

**Review the Java in this pull request.** The tests are the specification;
the implementation exists to satisfy them.

*Could a Java developer who has never seen the legacy system own this?*

## Scope of this stage

Business rules only, proven against in-memory doubles. There is
deliberately **no persistence implementation, no Spring configuration and
no endpoint** here: nothing at this stage could execute them, so they would
ship inside a feature labelled complete while being entirely unverified.
The integration stage writes them against the real migrated schema.

The service is a plain class taking its collaborators as constructor
parameters, so the tests construct it directly with fakes and no container
starts. Atomicity is expressed through the unit-of-work contract rather
than `@Transactional`, because a framework annotation does nothing in a
test with no transaction manager — it would read as a guarantee while
proving none.

## Traceability

| Story | Criterion | Test |
|---|---|---|
| Add New Transaction | I am authenticated and the account and card are active | `AddTransactionTest.successfulTransactionCreatesRecordAndUpdatesBalance` |
| Add New Transaction | I need to add a transaction | `AddTransactionTest.lookupByCardNumberRetrievesAccountAndCard` |
| Add New Transaction | the account is not active | `AddTransactionTest.inactiveAccountIsRejected` |
| Add New Transaction | the account and card are validated | `AddTransactionTest.allTransactionDetailFieldsArePersistedCorrectly` |
| Add New Transaction | I enter an amount that is not a valid numeric value with up to 2 decimal places | `AddTransactionTest.invalidAmountFormatIsRejected` |
| Add New Transaction | the transaction is created successfully | `AddTransactionTest.creditTransactionReducesBalanceAndUpdatesCycleCredit` |
| Make Online Payment | I am authenticated and my account has a positive balance | `MakePaymentTest.successfulPaymentReducesBalanceToZero` |
| Make Online Payment | I do not enter an account ID | `MakePaymentTest.emptyAccountIdIsRejected` |
| Make Online Payment | I enter an account ID that does not exist | `MakePaymentTest.nonExistentAccountIsRejected` |
| Make Online Payment | my account balance is zero or negative | `MakePaymentTest.zeroBalanceAccountCannotPay` |
| Make Online Payment | I have not entered 'Y' or 'y' in the confirmation field | `MakePaymentTest.unconfirmedPaymentIsRejected` |
| Make Online Payment | my payment is confirmed and validated | `MakePaymentTest.paymentTransactionRecordHasCorrectDetails` |

12 criteria, 12 tests, all traced.

## Business rules as typed errors

Legacy message text was truncated terminal English and will not survive
the UI redesign, so each rule is a named error code instead. The original
wording is kept in Javadoc so parity stays checkable.

- `ACCOUNT_ID_MISSING`
- `ACCOUNT_NOT_FOUND`
- `ACCOUNT_NOT_ACTIVE`
- `NOTHING_TO_PAY`
- `PAYMENT_NOT_CONFIRMED`
- `INVALID_AMOUNT_FORMAT`
- `CARD_NOT_FOUND`

## What the integration stage owes this feature

Guarantees the business layer states but cannot itself provide, because
it has no database. Carry these into the integration work.

- UnitOfWork.begin() must open a database transaction at SERIALIZABLE or REPEATABLE READ isolation level so that the SELECT MAX(id) in TransactionRepository.findMaxTransactionId() acquires a lock that is held until UnitOfWork.commit(). This prevents two concurrent callers from reading the same max and generating duplicate IDs.
- TransactionRepository.findMaxTransactionId() should use SELECT MAX(id) FROM transactions FOR UPDATE (or equivalent row-level locking) to serialise concurrent ID generation within the transaction opened by UnitOfWork.begin().
- AccountRepository.save() and TransactionRepository.save() must stage their writes within the same database transaction opened by UnitOfWork.begin(), so that the transaction record insert and the account balance update are committed atomically by UnitOfWork.commit().
- UnitOfWork.rollback() must discard all staged writes from both repositories, ensuring no partial state is persisted on failure.
- The transaction ID column is a 16-character zero-padded numeric string. The integration layer must ensure the column type and length match (CHAR(16) or VARCHAR(16)).

## Contracts moved into the main source tree (10)

These declare only interfaces, enums or records and hold no test, so they
are contracts. Under `src/test/java` they would sit on the test classpath
only and be invisible to the implementation.

| Emitted as | Moved to |
|---|---|
| `src/main/java/com/grapeup/carddemo/onlinetransactionmanagement/application/TransactionError.java` | `src/main/java/com/grapeup/carddemo/onlinetransactionmanagement/application/TransactionError.java` |
| `src/main/java/com/grapeup/carddemo/onlinetransactionmanagement/application/AddTransactionInput.java` | `src/main/java/com/grapeup/carddemo/onlinetransactionmanagement/application/AddTransactionInput.java` |
| `src/main/java/com/grapeup/carddemo/onlinetransactionmanagement/application/AddTransactionResult.java` | `src/main/java/com/grapeup/carddemo/onlinetransactionmanagement/application/AddTransactionResult.java` |
| `src/main/java/com/grapeup/carddemo/onlinetransactionmanagement/application/PaymentInput.java` | `src/main/java/com/grapeup/carddemo/onlinetransactionmanagement/application/PaymentInput.java` |
| `src/main/java/com/grapeup/carddemo/onlinetransactionmanagement/application/PaymentResult.java` | `src/main/java/com/grapeup/carddemo/onlinetransactionmanagement/application/PaymentResult.java` |
| `src/main/java/com/grapeup/carddemo/onlinetransactionmanagement/application/AccountRepository.java` | `src/main/java/com/grapeup/carddemo/onlinetransactionmanagement/application/AccountRepository.java` |
| `src/main/java/com/grapeup/carddemo/onlinetransactionmanagement/application/CardCrossReferenceRepository.java` | `src/main/java/com/grapeup/carddemo/onlinetransactionmanagement/application/CardCrossReferenceRepository.java` |
| `src/main/java/com/grapeup/carddemo/onlinetransactionmanagement/application/TransactionRepository.java` | `src/main/java/com/grapeup/carddemo/onlinetransactionmanagement/application/TransactionRepository.java` |
| `src/main/java/com/grapeup/carddemo/onlinetransactionmanagement/application/UnitOfWork.java` | `src/main/java/com/grapeup/carddemo/onlinetransactionmanagement/application/UnitOfWork.java` |
| `src/main/java/com/grapeup/carddemo/onlinetransactionmanagement/application/TransactionUseCase.java` | `src/main/java/com/grapeup/carddemo/onlinetransactionmanagement/application/TransactionUseCase.java` |

## Decisions a reviewer must see

Deliberate divergence from the legacy behaviour, stated rather than
hidden. Silent divergence is the thing a cautious reviewer fears.

- **test**: The AddTransactionInput accepts accountIdOrCardNumber as a string. The service distinguishes account ID (11 chars) from card number (16 chars) by length, matching the legacy COBOL behaviour described in criterion 2.
- **test**: For balance adjustment direction: type code '02' is treated as a credit (reduces balance, increases cycle credit). All other type codes are treated as debits (increase balance, increase cycle debit). This matches the legacy payment type code and the general COBOL pattern where purchases increase the amount owed.
- **test**: The transaction ID is a 16-character zero-padded numeric string. The next ID is generated by parsing the current max as a long, adding 1, and zero-padding back to 16 characters.
- **test**: The AddTransactionInput.cardNumber field is provided separately from accountIdOrCardNumber because when the lookup key is an account ID, the caller still needs to specify which card the transaction is charged to. When the lookup key is a card number, the service can derive it.
- **test**: Amount validation rejects values that are not valid numeric or have more than 2 decimal places. The test uses 'abc.xyz' as a clearly invalid value. An implementation should also reject amounts like '10.123' (3 decimal places).
- **test**: The null-account-ID case in PaymentInput also covers null (not just empty string). The test uses empty string; the service should treat null the same way.
- **test**: PaymentInput.confirmation accepts 'Y' or 'y' as valid confirmation. Any other value (including null or empty) is treated as unconfirmed.
- **implementation**: The input field accountIdOrCardNumber is disambiguated by length: 11 characters is treated as an account ID, 16 characters as a card number. This matches the legacy COBOL behaviour described in criterion 2.
- **implementation**: Type code '02' is treated as a credit transaction (reduces balance, increases cycle credit). All other type codes are treated as debit transactions (increase balance, increase cycle debit). This matches the legacy pattern where purchases increase the amount owed on the account.
- **implementation**: Amount validation uses a regex that accepts digits with an optional decimal point followed by 1 or 2 digits. Values like '10.123' (3 decimal places), negative amounts, or non-numeric strings are rejected with INVALID_AMOUNT_FORMAT.
- **implementation**: For addTransaction, the cardNumber is taken from the input record's cardNumber field when the lookup key is an account ID. If that field is null or blank, the service falls back to looking up a card via CardCrossReferenceRepository.findByAccountId(). This ensures the FK is always populated.
- **implementation**: For makePayment, the card number is always looked up via CardCrossReferenceRepository.findByAccountId() since the payment input only provides an account ID.
- **implementation**: Validation failures (inactive account, invalid amount, missing account ID, etc.) return a failure result before UnitOfWork.begin() is called, ensuring no side effects.
- **implementation**: BigDecimal values are constructed from strings, never from double literals, to preserve exact decimal representation matching COBOL's fixed-point arithmetic.

**Status: PASS** — every criterion is tested against the reviewed domain
types through the real service, no shadow types, no persistence or
composition written where nothing could execute it, no legacy vocabulary,
no screen mechanics asserted, no binary floating point on money.
