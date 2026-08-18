# T3 — PostgreSQL schema and EF Core model

Feature: **Online Transaction Management**

Copybooks are the semantic source; the relational model is the target.
FILLER, byte offsets and record lengths are deliberately absent — they belong
to the storage engine being retired.

## Tables

| Table | Entity | From | Columns | Prefix stripped | PK |
|---|---|---|---|---|---|
| `carddemo_commarea` | `CarddemoCommarea` | COCOM01Y | 16 | CDEMO | **none inferred** |
| `ccda_screen_title` | `CcdaScreenTitle` | COTTL01Y | 3 | CCDA | **none inferred** |
| `ws_date_time` | `WsDateTime` | CSDAT01Y | 20 | WS | **none inferred** |
| `ccda_common_messages` | `CcdaCommonMessages` | CSMSG01Y | 2 | CCDA | **none inferred** |
| `account` | `Account` | CVACT01Y | 12 | ACCT | id |
| `card_xref` | `CardXref` | CVACT03Y | 3 | XREF | **none inferred** |
| `tran` | `Tran` | CVTRA05Y | 13 | TRAN | id |

## Enum candidates (from 88-level condition names)

- `carddemo_commarea.user_type` — `CDEMO-USRTYP-ADMIN` ('A'), `CDEMO-USRTYP-USER` ('U')
- `carddemo_commarea.pgm_context` — `CDEMO-PGM-ENTER` (0), `CDEMO-PGM-REENTER` (1)

## Foreign-key candidates

A column name appearing in more than one table. Which side owns the
relationship is a domain decision, so nothing is asserted here.

- `acct_id` — card_xref, carddemo_commarea
- `card_num` — card_xref, carddemo_commarea, tran
- `cust_id` — card_xref, carddemo_commarea

## Owned-entity candidates

Columns sharing a second-level prefix often want to become a value object
rather than flat columns (e.g. merchant id/name/city/zip -> Merchant).

- `carddemo_commarea`: **cust** -> cust_id, cust_fname, cust_mname, cust_lname
- `ws_date_time`: **curdate** -> curdate_year, curdate_month, curdate_day, curdate_mm, curdate_dd, curdate_yy
- `ws_date_time`: **curtime** -> curtime_hours, curtime_minute, curtime_second, curtime_milsec, curtime_hh, curtime_mm, curtime_ss
- `ws_date_time`: **timestamp** -> timestamp_dt_yyyy, timestamp_dt_mm, timestamp_dt_dd, timestamp_tm_hh, timestamp_tm_mm, timestamp_tm_ss, timestamp_tm_ms6
- `account`: **curr** -> curr_bal, curr_cyc_credit, curr_cyc_debit
- `tran`: **merchant** -> merchant_id, merchant_name, merchant_city, merchant_zip

## Not decided here

Table naming, entity grouping, which candidate keys are real, and enum member
names are domain decisions. They belong to the design step, which is
human-approved before any feature code is generated.