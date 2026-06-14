using System.Runtime.CompilerServices;
using Octonica.ClickHouseClient;

namespace OlapOverHttp.Postgres.Filler;

internal sealed class ClickHousePostingReader(ClickHouseConnectionSettings settings)
{
    public async IAsyncEnumerable<ClickHousePostingRow> ReadBatchAsync(
        DateTime weekStart,
        DateTime weekEnd,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var connection = new ClickHouseConnection(settings);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand(
            $"""
            SELECT
                posting_id,
                posting_name,
                seller_id,
                item_ids,
                item_quantities,
                marketplace_item_prices,
                seller_item_prices,
                seller_currency,
                seller_fx_rate,
                posting_created_at,
                posting_delivered_at,
                posting_source,
                total_amount,
                payment_method,
                shipping_country,
                shipping_city,
                version
            FROM postings FINAL
            WHERE posting_created_at >= '{weekStart:yyyy-MM-dd HH:mm:ss}'
                AND posting_created_at < '{weekEnd:yyyy-MM-dd HH:mm:ss}'
            """);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            yield return new ClickHousePostingRow(
                PostingId: reader.GetInt64(0),
                PostingName: reader.GetString(1),
                SellerId: reader.GetInt64(2),
                ItemIds: reader.GetFieldValue<long[]>(3),
                ItemQuantities: reader.GetFieldValue<int[]>(4),
                MarketplaceItemPrices: reader.GetFieldValue<decimal[]>(5),
                SellerItemPrices: reader.GetFieldValue<decimal[]>(6),
                SellerCurrency: reader.GetSByte(7),
                SellerFxRate: reader.GetDecimal(8),
                PostingCreatedAt: reader.GetDateTime(9),
                PostingDeliveredAt: reader.GetDateTime(10),
                PostingSource: reader.GetString(11),
                TotalAmount: reader.GetDecimal(12),
                PaymentMethod: reader.GetString(13),
                ShippingCountry: reader.GetString(14),
                ShippingCity: reader.GetString(15),
                Version: reader.GetSByte(16));
        }
    }
}
