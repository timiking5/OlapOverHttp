-- +goose Up
CREATE SCHEMA IF NOT EXISTS "posting";

CREATE TABLE IF NOT EXISTS "posting".postings(
	id                   bigserial PRIMARY KEY,
	posting_id           bigint NOT NULL,
	posting_name         text STORAGE MAIN NOT NULL,
	seller_id            bigint NOT NULL,
	seller_fx_rate       decimal(18, 6) NOT NULL,
	seller_currency      int8 NOT NULL,
	posting_created_at   timestamptz NOT NULL,
	posting_delivered_at timestamptz NOT NULL,
	posting_source       text STORAGE MAIN NOT NULL,
	total_amount         decimal(18, 4) NOT NULL,
	payment_method       text STORAGE MAIN NOT NULL,
	shipping_country     text STORAGE MAIN NOT NULL,
	shipping_city        text STORAGE MAIN NOT NULL);

CREATE UNIQUE INDEX IF NOT EXISTS postings_posting_id_seller_id ON "posting".postings(posting_id, seller_id);

CREATE INDEX IF NOT EXISTS postings_seller_id_posting_created_at_idx ON "posting".postings(seller_id, posting_created_at);

CREATE TABLE IF NOT EXISTS "posting".items(
	posting_entry_id       bigint REFERENCES "posting".postings(id),
	item_id                bigint NOT NULL,
	version                int NOT NULL,
	item_quantity          int NOT NULL,
	marketplace_item_price decimal(18, 4) NOT NULL,
	seller_item_price      decimal(18, 4) NOT NULL,
	PRIMARY KEY (posting_entry_id, item_id)
);

-- +goose Down
DROP TABLE IF EXISTS "posting".items;
DROP TABLE IF EXISTS "posting".postings;
DROP SCHEMA IF EXISTS "posting";
