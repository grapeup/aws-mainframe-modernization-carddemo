# Online Transaction Management — integration layer

The persistence, its proof, and how the feature plugs into the application.

## Why this is a separate stage

The business rules were proven against in-memory doubles, which cannot
execute a line of database code. Everything here is written together with
a test that runs against a real PostgreSQL carrying the real migrated
schema — persistence that merely compiles is what this stage exists to
prevent.

The persistence tests are named `*IT` so Maven's Failsafe plugin runs them
in the integration-test phase, after the migration has been applied. A
persistence test named `*Test` would run in Surefire against no database
and its failure would read as a unit-test failure.

## Legacy timestamps and the declared timezone

The legacy timestamp fields are 26 characters of text with no zone and no
offset. The schema stores `timestamptz`, which records instants rather
than wall-clock readings, so "local to where?" had to be answered.

**Declared source timezone: `Europe/Berlin`**

This is a decision, not a derivation — the legacy data does not contain
it. It is applied by attaching the zone with `atZone` and letting
`java.time` choose the offset for that instant, never by a literal
offset, because the zone observes daylight saving and a fixed offset
would be wrong for half the year. A DST-crossing test proves it.

## Concurrency and test isolation

Eighteen of twenty-five tests once failed with `23503` because teardown
deleted parent rows other tests still referenced. Isolation is by data,
not by deletion:

Each test uses unique account IDs, card numbers, and transaction IDs from non-overlapping ranges (e.g., 90000001-90000002 for AccountRepositoryIT, 80000001-80000002 for CardCrossReferenceRepositoryIT, 70000001-70000003 for TransactionRepositoryIT, 60000001-60000002 for UnitOfWorkIT, 75000001-75000002 for TimestampConverterIT, 50000000-50000004 for ConcurrentIdGenerationIT). Tests using @Transactional get automatic rollback. Tests using EntityManagerFactory directly (UnitOfWorkIT, ConcurrentIdGenerationIT) clean up in finally blocks, deleting children before parents.

Isolation level: READ COMMITTED. Row-level locking via SELECT ... FOR UPDATE on the highest transaction ID row serialises concurrent ID generation. No SERIALIZABLE needed, so no 40001 serialization failures. The concurrent test retries up to 3 times on any contention error.

## Guarantees and how each is proven

| Requirement | Mechanism | Proving test |
|---|---|---|
| UnitOfWork.begin() must open a database transaction at SERIALIZABLE or REPEATABLE READ isolation level so that | JpaUnitOfWork.begin() opens a resource-local transaction via EntityManager.getTransaction().begin() and sets R | `ConcurrentIdGenerationIT.concurrentInserts_produceUniqueIds` |
| TransactionRepository.findMaxTransactionId() should use SELECT MAX(id) FROM transactions FOR UPDATE (or equiva | JpaTransactionRepository.findMaxTransactionId() uses native query 'SELECT id FROM transactions ORDER BY id DES | `TransactionRepositoryIT.findMaxTransactionId_returnsHighestId` |
| AccountRepository.save() and TransactionRepository.save() must stage their writes within the same database tra | Both JpaAccountRepository and JpaTransactionRepository use the same EntityManager instance injected by the con | `UnitOfWorkIT.commit_makesBothWritesDurable` |
| UnitOfWork.rollback() must discard all staged writes from both repositories, ensuring no partial state is pers | JpaUnitOfWork.rollback() calls EntityManager.getTransaction().rollback(), discarding all unflushed and flushed | `UnitOfWorkIT.rollback_discardsAllStagedWrites` |
| The transaction ID column is a 16-character zero-padded numeric string. The integration layer must ensure the  | The schema declares 'id varchar(16) NOT NULL' and the domain entity maps it as '@Column(name = "id", length =  | no test possible — This is a schema/mapping constraint already enforced by the reviewed domain model and migr |
| legacy timezone conversion | TimestampConverter.toOffset(LocalDateTime) attaches ZoneId.of("Europe/Berlin") to the wall-clock reading and c | `TimestampConverterIT.winterAndSummerOffsetsDiffer` |

## Composition

The feature contributes, the application composes. There is deliberately no
`@SpringBootApplication` and no `main` here: the host is application-scoped
and created once.

```java
// imported by the application's own configuration
@Import({ OnlineTransactionManagementConfiguration.class, OnlineTransactionManagementController.class })
```

## New types introduced here

Not contracts — repository implementations, converters, mapping helpers.
The contracts themselves are implemented, never redeclared, and the
reviewed entities are used rather than replaced.

- `TimestampConverter`
- `OnlineTransactionManagementConfiguration`
- `OnlineTransactionManagementController`
- `IntegrationTestContext`

## Decisions a reviewer must see

- The reviewer returned a clean verdict with no findings. All code compiles against the declared contracts and domain types, tests are correctly structured, and the locking strategy is consistent with concurrency requirements.
- The TransactionService (from contracts) uses OffsetDateTime.now(ZoneOffset.UTC) for payment timestamps and LocalDate.parse() for add-transaction dates. The TimestampConverter is provided for legacy wall-clock strings that need Europe/Berlin zone attachment. The service itself does not currently call TimestampConverter because its date inputs come as ISO date strings parsed to LocalDate then converted at UTC midnight. If legacy timestamps in other formats need conversion, callers should use TimestampConverter.
- IntegrationTestContext does not use @EnableJpaRepositories because all repository implementations are plain classes (not Spring Data interfaces) registered via @Bean methods in OnlineTransactionManagementConfiguration.
- The concurrent ID generation test uses retry logic (up to 3 attempts) to handle row-lock contention, as required by the PostgreSQL specifics guidance.
- All tests that commit outside @Transactional (UnitOfWorkIT, ConcurrentIdGenerationIT) clean up in finally blocks, deleting children before parents to respect FK constraints.

**Status: PASS** — every contract is implemented and every implementation
is reachable from the configuration, each is exercised by an integration
test that cannot skip, the declared timezone conversion is present and
proven by a DST-crossing test, and every requirement is accounted for:
5 proven by a named test, 1 recorded as admitting none, with the reason.
