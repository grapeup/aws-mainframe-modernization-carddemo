-- Flyway versioned migration V001__online_transaction_management.sql
-- Applied once, never edited: Flyway checksums each migration and will
-- refuse to run if an already-applied file changes. A later feature adds
-- the next version rather than rewriting this one, so the database keeps
-- an ordered, auditable history of the migration as it progressed.

-- PostgreSQL schema for Online Transaction Management.
-- Types, widths, precisions and scales are computed from the legacy field
-- definitions and enforced against them. Names and structure are design
-- decisions. NOT NULL mirrors the declared nullability so model and schema
-- agree.

CREATE TABLE accounts (
    id                         bigint           NOT NULL,
    active_status              varchar(1)       NOT NULL,
    current_balance            numeric(12,2)    NOT NULL,
    credit_limit               numeric(12,2)    NOT NULL,
    cash_credit_limit          numeric(12,2)    NOT NULL,
    open_date                  date             NOT NULL,
    expiration_date            date             NOT NULL,
    reissue_date               date            ,
    current_cycle_credit       numeric(12,2)    NOT NULL,
    current_cycle_debit        numeric(12,2)    NOT NULL,
    address_zip                varchar(10)      NOT NULL,
    group_id                   varchar(10)      NOT NULL,
    CONSTRAINT pk_accounts PRIMARY KEY (id)
);

CREATE TABLE card_cross_references (
    card_number                varchar(16)      NOT NULL,
    customer_id                integer          NOT NULL,
    account_id                 bigint           NOT NULL,
    CONSTRAINT pk_card_cross_references PRIMARY KEY (card_number)
);

CREATE TABLE transactions (
    id                         varchar(16)      NOT NULL,
    type_code                  varchar(2)       NOT NULL,
    category_code              smallint         NOT NULL,
    source                     varchar(10)      NOT NULL,
    description                varchar(100)     NOT NULL,
    amount                     numeric(11,2)    NOT NULL,
    originated_at              timestamptz     ,
    processed_at               timestamptz     ,
    card_number                varchar(16)      NOT NULL,
    merchant_id                integer         ,
    merchant_name              varchar(50)     ,
    merchant_city              varchar(50)     ,
    merchant_zip               varchar(10)     ,
    CONSTRAINT pk_transactions PRIMARY KEY (id)
);

ALTER TABLE card_cross_references ADD CONSTRAINT fk_card_cross_references_account_id FOREIGN KEY (account_id)
    REFERENCES accounts(id);
ALTER TABLE transactions ADD CONSTRAINT fk_transactions_card_number FOREIGN KEY (card_number)
    REFERENCES card_cross_references(card_number);