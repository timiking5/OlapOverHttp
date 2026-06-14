using Npgsql;

namespace OlapOverHttp.Postgres.Filler;

internal sealed class PostgresPostingWriter(string connectionString) : IAsyncDisposable
{
    private const string CreatePostingsStagingTableSql =
        """
        CREATE TEMP TABLE IF NOT EXISTS postings_staging (
            posting_id bigint NOT NULL,
            posting_name text NOT NULL,
            seller_id bigint NOT NULL,
            seller_fx_rate numeric(18, 6) NOT NULL,
            seller_currency bigint NOT NULL,
            posting_created_at timestamptz NOT NULL,
            posting_delivered_at timestamptz NOT NULL,
            posting_source text NOT NULL,
            total_amount numeric(18, 4) NOT NULL,
            payment_method text NOT NULL,
            shipping_country text NOT NULL,
            shipping_city text NOT NULL,
            version integer NOT NULL
        ) ON COMMIT DELETE ROWS
        """;

    private const string CreateItemsStagingTableSql =
        """
        CREATE TEMP TABLE IF NOT EXISTS items_staging (
            posting_id bigint NOT NULL,
            seller_id bigint NOT NULL,
            item_id bigint NOT NULL,
            version integer NOT NULL,
            item_quantity integer NOT NULL,
            marketplace_item_price numeric(18, 4) NOT NULL,
            seller_item_price numeric(18, 4) NOT NULL
        ) ON COMMIT DELETE ROWS
        """;

    private const string CopyPostingsSql =
        """
        COPY postings_staging (
            posting_id,
            posting_name,
            seller_id,
            seller_fx_rate,
            seller_currency,
            posting_created_at,
            posting_delivered_at,
            posting_source,
            total_amount,
            payment_method,
            shipping_country,
            shipping_city,
            version)
        FROM STDIN (FORMAT BINARY)
        """;

    private const string CopyItemsSql =
        """
        COPY items_staging (
            posting_id,
            seller_id,
            item_id,
            version,
            item_quantity,
            marketplace_item_price,
            seller_item_price)
        FROM STDIN (FORMAT BINARY)
        """;

    private const string TransferPostingsSql =
        """
        INSERT INTO "posting".postings (
            posting_id,
            posting_name,
            seller_id,
            seller_fx_rate,
            seller_currency,
            posting_created_at,
            posting_delivered_at,
            posting_source,
            total_amount,
            payment_method,
            shipping_country,
            shipping_city)
        SELECT
            posting_id,
            posting_name,
            seller_id,
            seller_fx_rate,
            seller_currency,
            posting_created_at,
            posting_delivered_at,
            posting_source,
            total_amount,
            payment_method,
            shipping_country,
            shipping_city
        FROM (
            SELECT DISTINCT ON (posting_id, seller_id)
                posting_id,
                posting_name,
                seller_id,
                seller_fx_rate,
                seller_currency,
                posting_created_at,
                posting_delivered_at,
                posting_source,
                total_amount,
                payment_method,
                shipping_country,
                shipping_city,
                version
            FROM postings_staging
            ORDER BY posting_id, seller_id, version DESC
        ) AS deduped_postings
        ON CONFLICT (posting_id, seller_id) DO UPDATE SET
            posting_name = EXCLUDED.posting_name,
            seller_fx_rate = EXCLUDED.seller_fx_rate,
            seller_currency = EXCLUDED.seller_currency,
            posting_created_at = EXCLUDED.posting_created_at,
            posting_delivered_at = EXCLUDED.posting_delivered_at,
            posting_source = EXCLUDED.posting_source,
            total_amount = EXCLUDED.total_amount,
            payment_method = EXCLUDED.payment_method,
            shipping_country = EXCLUDED.shipping_country,
            shipping_city = EXCLUDED.shipping_city
        """;

    private const string TransferItemsSql =
        """
        INSERT INTO "posting".items (
            posting_entry_id,
            item_id,
            version,
            item_quantity,
            marketplace_item_price,
            seller_item_price)
        SELECT
            p.id,
            s.item_id,
            s.version,
            s.item_quantity,
            s.marketplace_item_price,
            s.seller_item_price
        FROM (
            SELECT DISTINCT ON (posting_id, seller_id, item_id)
                posting_id,
                seller_id,
                item_id,
                version,
                item_quantity,
                marketplace_item_price,
                seller_item_price
            FROM items_staging
            ORDER BY posting_id, seller_id, item_id, version DESC
        ) s
        INNER JOIN "posting".postings p
            ON p.posting_id = s.posting_id
            AND p.seller_id = s.seller_id
        ON CONFLICT (posting_entry_id, item_id) DO UPDATE SET
            version = EXCLUDED.version,
            item_quantity = EXCLUDED.item_quantity,
            marketplace_item_price = EXCLUDED.marketplace_item_price,
            seller_item_price = EXCLUDED.seller_item_price
        """;

    private NpgsqlConnection? _connection;
    private bool _stagingTablesEnsured;

    public async Task<(int Postings, int Items)> SyncBatch(
        IReadOnlyCollection<ClickHousePostingRow> batch,
        CancellationToken cancellationToken = default)
    {
        if (batch.Count == 0)
            return (0, 0);

        var connection = await GetConnectionAsync(cancellationToken);
        await EnsureStagingTablesAsync(connection, cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await BinaryCopyPostingsAsync(connection, batch, cancellationToken);
        var itemCount = await BinaryCopyItemsAsync(connection, batch, cancellationToken);

        await ExecuteNonQueryAsync(connection, TransferPostingsSql, cancellationToken);
        await ExecuteNonQueryAsync(connection, TransferItemsSql, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return (batch.Count, itemCount);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    private async Task<NpgsqlConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is null)
        {
            _connection = new NpgsqlConnection(connectionString);
            await _connection.OpenAsync(cancellationToken);
        }

        return _connection;
    }

    private async Task EnsureStagingTablesAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        if (_stagingTablesEnsured)
            return;

        await ExecuteNonQueryAsync(connection, CreatePostingsStagingTableSql, cancellationToken);
        await ExecuteNonQueryAsync(connection, CreateItemsStagingTableSql, cancellationToken);
        _stagingTablesEnsured = true;
    }

    private static async Task BinaryCopyPostingsAsync(
        NpgsqlConnection connection,
        IReadOnlyCollection<ClickHousePostingRow> postings,
        CancellationToken cancellationToken)
    {
        await using var importer = await connection.BeginBinaryImportAsync(CopyPostingsSql, cancellationToken);

        foreach (var posting in postings)
        {
            await importer.StartRowAsync(cancellationToken);
            await importer.WriteAsync(posting.PostingId, cancellationToken);
            await importer.WriteAsync(posting.PostingName, cancellationToken);
            await importer.WriteAsync(posting.SellerId, cancellationToken);
            await importer.WriteAsync(posting.SellerFxRate, cancellationToken);
            await importer.WriteAsync((long)posting.SellerCurrency, cancellationToken);
            await importer.WriteAsync(
                DateTime.SpecifyKind(posting.PostingCreatedAt, DateTimeKind.Utc),
                cancellationToken);
            await importer.WriteAsync(
                DateTime.SpecifyKind(posting.PostingDeliveredAt, DateTimeKind.Utc),
                cancellationToken);
            await importer.WriteAsync(posting.PostingSource, cancellationToken);
            await importer.WriteAsync(posting.TotalAmount, cancellationToken);
            await importer.WriteAsync(posting.PaymentMethod, cancellationToken);
            await importer.WriteAsync(posting.ShippingCountry, cancellationToken);
            await importer.WriteAsync(posting.ShippingCity, cancellationToken);
            await importer.WriteAsync((int)posting.Version, cancellationToken);
        }

        await importer.CompleteAsync(cancellationToken);
    }

    private static async Task<int> BinaryCopyItemsAsync(
        NpgsqlConnection connection,
        IReadOnlyCollection<ClickHousePostingRow> postings,
        CancellationToken cancellationToken)
    {
        var itemCount = 0;

        await using var importer = await connection.BeginBinaryImportAsync(CopyItemsSql, cancellationToken);

        foreach (var posting in postings)
        {
            itemCount += posting.ItemIds.Length;
            for (var index = 0; index < posting.ItemIds.Length; index++)
            {
                await importer.StartRowAsync(cancellationToken);
                await importer.WriteAsync(posting.PostingId, cancellationToken);
                await importer.WriteAsync(posting.SellerId, cancellationToken);
                await importer.WriteAsync(posting.ItemIds[index], cancellationToken);
                await importer.WriteAsync((int)posting.Version, cancellationToken);
                await importer.WriteAsync(posting.ItemQuantities[index], cancellationToken);
                await importer.WriteAsync(posting.MarketplaceItemPrices[index], cancellationToken);
                await importer.WriteAsync(posting.SellerItemPrices[index], cancellationToken);
            }
        }

        await importer.CompleteAsync(cancellationToken);

        return itemCount;
    }

    private static async Task ExecuteNonQueryAsync(
        NpgsqlConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
