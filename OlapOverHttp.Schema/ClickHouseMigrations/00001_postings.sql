-- +goose Up
-- +goose NO TRANSACTION
CREATE TABLE IF NOT EXISTS postings
(
    posting_id              Int64,
    posting_name            String,
    seller_id               Int64,
    item_ids                Array(Int64),
    item_quantities         Array(Int32),
    marketplace_item_prices Array(Decimal(18, 6)),
    seller_item_prices      Array(Decimal(18, 6)),
    seller_currency         Int8,
    seller_fx_rate          Decimal(18, 8),
    posting_created_at      DateTime,
    posting_delivered_at    DateTime,
    posting_source          LowCardinality(String),
    total_amount            Decimal(18, 6),
    payment_method          LowCardinality(String),
    shipping_country        LowCardinality(String),
    shipping_city           String,
    version                 Int8
)
ENGINE = ReplacingMergeTree(version)
PARTITION BY toYYYYMM(posting_created_at)
ORDER BY (seller_id, posting_created_at, posting_id);


-- +goose Down
-- +goose NO TRANSACTION
DROP TABLE IF EXISTS postings;
