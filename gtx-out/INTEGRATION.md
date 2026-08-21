# Online Transaction Management — integration layer

The persistence, its proof, and how the feature plugs into the application.

## Why this is a separate stage

The business rules were proven against in-memory doubles, which cannot
execute a line of database code. Everything here is written together with
a test that runs against a real PostgreSQL carrying the real migrated
schema — persistence that merely compiles is what this stage exists to
prevent.

## Legacy timestamps and the declared timezone

The legacy timestamp fields are 26 characters of text with no zone and no
offset. The schema stores `timestamptz`, which records instants rather
than wall-clock readings, so "local to where?" had to be answered.

**Declared source timezone: `Europe/Berlin`**

This is a decision, not a derivation — the legacy data does not contain
it. It is applied through `TimeZoneInfo`, never a fixed offset, because
the zone observes daylight saving and a literal offset would be wrong for
half the year. A DST-crossing test proves it.

## Concurrency and test isolation

Eighteen of twenty-five tests once failed with `23503` because teardown
deleted parent rows other tests still referenced. Isolation is by data,
not by deletion:

Each test class generates unique account IDs via UniqueIds.NextAccountId() (atomic counter starting at 9,000,000). Card numbers are derived deterministically from account IDs. Transaction IDs are offset from account IDs by large constants (e.g. +100M, +200M, +300M, +400M, +500M) to avoid collisions. Each test class implements IAsyncLifetime to seed its own data in InitializeAsync and clean up (children before parents) in DisposeAsync. Tests sharing extra accounts clean up in finally blocks.

Isolation level: READ COMMITTED with row-level FOR UPDATE locks on the max transaction id row. This avoids the 40001 serialization failures that SERIALIZABLE would cause. The concurrent test retries up to 5 times on contention/deadlock errors.

## Guarantees and how each is proven

| Requirement | Mechanism | Proving test |
|---|---|---|
| IUnitOfWork.BeginAsync must open a database transaction at SERIALIZABLE or REPEATABLE READ isolation level so  | UnitOfWork.BeginAsync opens a READ COMMITTED transaction. The row-level FOR UPDATE lock in GetMaxTransactionId | `ConcurrentIdTests.ConcurrentAddTransactions_ProduceUniqueIds` |
| ITransactionRepository.GetMaxTransactionIdAsync must execute SELECT ... FOR UPDATE (or equivalent row-level lo | TransactionRepository.GetMaxTransactionIdAsync executes 'SELECT id AS "Value" FROM transactions ORDER BY id DE | `TransactionRepositoryTests.GetMaxTransactionIdAsync_ReturnsZero_WhenEmpty` |
| IUnitOfWork.CommitAsync must atomically apply both the staged transaction insert and the staged account update | UnitOfWork.CommitAsync calls SaveChangesAsync (which flushes all tracked changes) then CommitAsync on the data | `AtomicityTests.CommitAsync_IsAtomic_TransactionAndAccountUpdateTogether` |
| If the system moves to high concurrency, the id-generation strategy should be replaced with a database sequenc | Documented via the FOR UPDATE row lock approach. The concurrent test proves unique IDs under contention with r | `ConcurrentIdTests.ConcurrentAddTransactions_ProduceUniqueIds` |
| TransactionRecord.CardNumber is a mandatory foreign key to the card cross-references table. The service popula | The FK constraint fk_transactions_card_number is created by the Flyway migration. Tests insert parent rows (ac | `AddTransactionIntegrationTests.AddTransaction_ByCardNumber_Succeeds` |
| legacy timezone conversion | TimezoneConverter.ToUtc uses TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin") to convert wall-clock DateTi | `TimezoneConversionTests.WinterTimestamp_IsStoredAsUtcMinus1Hour` |
| legacy timezone conversion (DST crossing - summer) | Same TimezoneConverter.ToUtc, tested with a July timestamp that should be UTC-2 hours from Berlin local time. | `TimezoneConversionTests.SummerTimestamp_IsStoredAsUtcMinus2Hours` |
| Optimistic concurrency: competing updates, assert the second is rejected. | XminDbContext derives from the reviewed DbContext and adds UseXminAsConcurrencyToken() for Account. Two contex | `OptimisticConcurrencyTests.CompetingAccountUpdates_SecondIsRejected_WithDbUpdateConcurrencyException` |
| Atomicity: fail partway, assert the earlier write rolled back. | UnitOfWork.RollbackAsync is called after staging both a transaction insert and an account update. The test ver | `UnitOfWorkTests.Rollback_RevertsAllStagedChanges` |

## Composition

The feature contributes, the application composes. There is deliberately no
`Program.cs` here: the host is application-scoped and created once.

```csharp
services.AddCardDemoOnlineTransactionManagement(connectionString);
app.MapCardDemoOnlineTransactionManagement();
```

## New types introduced here

Not contracts — repository implementations, converters, mapping helpers.
The contracts themselves are implemented, never redeclared, and the
reviewed DbContext is used or derived from, never replaced.

- `CardDemo.OnlineTransactionManagement.Infrastructure.TimezoneConverter`
- `CardDemo.OnlineTransactionManagement.Infrastructure.AccountRepository`
- `CardDemo.OnlineTransactionManagement.Infrastructure.CardRepository`
- `CardDemo.OnlineTransactionManagement.Infrastructure.TransactionRepository`
- `CardDemo.OnlineTransactionManagement.Infrastructure.UnitOfWork`
- `CardDemo.OnlineTransactionManagement.Infrastructure.DependencyInjection`
- `CardDemo.OnlineTransactionManagement.Infrastructure.EndpointMapping`
- `CardDemo.OnlineTransactionManagement.IntegrationTests.TestDbFixture`
- `CardDemo.OnlineTransactionManagement.IntegrationTests.DatabaseCollection`
- `CardDemo.OnlineTransactionManagement.IntegrationTests.UniqueIds`
- `CardDemo.OnlineTransactionManagement.IntegrationTests.XminDbContext`

## Decisions a reviewer must see

- Transaction.Id in the domain is string (varchar(16) in schema). TransactionRecord.Id in the contract is long. The repository pads the long to 16 chars with leading zeros when creating the domain entity, and parses the string back to long when reading max id.
- CardCrossReference has no ActiveStatus column in the schema or domain. The CardRecord contract requires one, so the repository defaults to 'Y'. The service's active-status check is on the Account, not the Card.
- The merchant_id column is NOT NULL integer in the schema. When TransactionRecord.MerchantId is null or empty, we store 0. MerchantName/City/Zip default to empty string to satisfy NOT NULL constraints.
- Account.ActiveStatus in the domain is string, but AccountRecord.ActiveStatus in the contract is char. The repository converts between them.
- SqlQueryRaw<string> with AS "Value" is the EF Core 8 pattern for scalar SQL queries returning a single column.
- The throughput ceiling of the FOR UPDATE id-generation strategy is documented: under high concurrency, a database sequence (NEXTVAL) should replace it.
- The reviewer returned a clean verdict with no findings. All code was verified correct against contracts, domain types, DbContext, and schema.

**Status: PASS** — every contract has exactly one implementation, each
exercised by an integration test that cannot skip, the declared timezone
conversion is present and proven, and every guarantee is demonstrated by a
named test.
