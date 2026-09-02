# Database migrations

The schema is delivered as **Flyway versioned migrations**, not as
a `schema.sql` snapshot.

A snapshot is the wrong shape for this programme. Features move to
Java one at a time over months, each adding tables and columns to a
database that stays live and shared with the legacy system
throughout. That needs an ordered, checksummed history which can
be replayed on any environment and audited afterwards -- not a
file rewritten on every generation.

## Why Flyway and not Hibernate schema generation

`spring.jpa.hibernate.ddl-auto` is not a migration tool. It can
create a schema that matches the entities, but it cannot express an
ordered change history, it cannot be reviewed before it runs, and
it has no answer for data that already exists.

| | Flyway | Hibernate ddl-auto |
|---|---|---|
| Migration content | plain SQL | implicit, generated at startup |
| Reviewable by a DBA before it runs | yes | no |
| Ordered, checksummed history | yes | none |
| Couples the schema to the entity classes | no | yes |
| Usable while the legacy system still owns the data | yes | no |

This code is reviewed by people with a bad experience of
unreadable generated output. A migration a DBA can read line by
line is worth more than one a framework writes for us.

## Applying

```bash
flyway -configFiles=flyway.conf migrate
```

CI applies this migration to a real PostgreSQL instance on every
push, so the DDL is proven to execute rather than merely
generated.

## Adding the next feature

Bump the `Schema Version (INPUT)` node and run the workflow again.
It emits `V002__<feature>.sql` beside this one. Never edit an
applied migration -- Flyway refuses to run if its checksum
changes.
