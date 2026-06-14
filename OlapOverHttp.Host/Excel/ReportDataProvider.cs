using OlapOverHttp.Host.ClickHouse.Infrastructure;
using System.Runtime.CompilerServices;

namespace OlapOverHttp.Host.Excel;

public sealed class ReportDataProvider(IClickHouseConnectionFactory connectionFactory)
{
    public async IAsyncEnumerable<PostingReportRow> Get(
        long sellerId,
        DateOnly periodStart,
        DateOnly periodEnd,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(token);

        await using var command = connection.CreateCommand(
            $"""
            WITH filtered_rows AS (
                SELECT
                    toDate(posting_created_at) as posting_date,
                    toDate(posting_delivered_at) as delivery_date,
                    item_ids,
                    item_quantities,
                    marketplace_item_prices,
                    seller_item_prices,
                    seller_fx_rate,
                    seller_currency,
                    payment_method,
                    shipping_country,
                    shipping_city
                FROM posting.postings
                WHERE seller_id = {sellerId}
                    AND posting_created_at BETWEEN '{periodStart:yyyy-MM-dd} 00:00:00' AND '{periodEnd:yyyy-MM-dd} 23:59:59'
            )
            SELECT
                posting_date,
                any(delivery_date),  -- probably should be in the group by
                item_id,
                SUM(item_quantity),
                SUM(item_quantity * marketplace_item_price),
                SUM(item_quantity * seller_item_price),
                seller_fx_rate,
                any(seller_currency),
                payment_method,
                shipping_country,
                shipping_city
            FROM filtered_rows
            ARRAY JOIN
                item_ids as item_id,
                item_quantities as item_quantity,
                marketplace_item_prices as marketplace_item_price,
                seller_item_prices as seller_item_price
            GROUP BY posting_date, item_id, payment_method, seller_fx_rate, shipping_country, shipping_city
            """);

        // command.Parameters.AddWithValue("sellerId", sellerId);
        // command.Parameters.AddWithValue("periodStart", periodStart);
        // command.Parameters.AddWithValue("periodEnd", periodEnd);

        await using var reader = await command.ExecuteReaderAsync(token);

        while (await reader.ReadAsync(token))
        {
            yield return new(
                PostingDate: reader.GetFieldValue<DateOnly>(0),
                DeliveryDate: reader.GetFieldValue<DateOnly>(1),
                ItemId: reader.GetInt64(2),
                ItemQuantity: (int)reader.GetInt64(3),
                MarketplaceItemTotal: reader.GetDecimal(4),
                SellerItemTotal: reader.GetDecimal(5),
                SellerFxRate: reader.GetDecimal(6),
                SellerCurrency: reader.GetSByte(7),
                PaymentMethod: reader.GetString(8),
                ShippingCountry: reader.GetString(9),
                ShippingCity: reader.GetString(10));
        }
    }
}
