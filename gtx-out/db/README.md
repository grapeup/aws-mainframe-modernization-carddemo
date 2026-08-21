# Database migrations

The schema is delivered as **Flyway versioned migrations**, not as
a `schema.sql` snapshot.

A snapshot is the wrong shape for this programme. Features move to
C# one at a time over months, each adding tables and columns to a
database that stays live and shared with the legacy system
throughout. That needs an ordered, checksummed history which can
be replayed on any environment and audited afterwards -- not a
file rewritten on every generation.

## Why Flyway and not EF Core Migrations

| | Flyway | EF Core Migrations |
|---|---|---|
| Migration content | plain SQL | generated C# |
| Reviewable by a DBA | yes | no |
| Couples schema to the C# model | no | yes |
| Usable while the legacy system still owns the data | yes | awkward |

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
