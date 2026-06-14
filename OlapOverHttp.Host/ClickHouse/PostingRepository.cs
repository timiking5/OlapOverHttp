using System.Runtime.CompilerServices;
using Octonica.ClickHouseClient;
using OlapOverHttp.Host.ClickHouse.Infrastructure;
using OlapOverHttp.Host.Data;

namespace OlapOverHttp.Host.ClickHouse;

public sealed class PostingRepository(IClickHouseConnectionFactory connectionFactory) : IPostingRepository
{
    public async IAsyncEnumerable<Postingtem> Get(
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
                    posting_id,
                    posting_name,
                    posting_created_at,
                    posting_delivered_at,
                    item_ids,
                    item_quantities,
                    marketplace_item_prices,
                    posting_source,
                    payment_method,
                    shipping_city,
                    shipping_country
                FROM posting.postings
                WHERE seller_id = {sellerId}
                    AND posting_created_at BETWEEN '{periodStart:yyyy-MM-dd} 00:00:00' AND '{periodEnd:yyyy-MM-dd} 23:59:59')
            SELECT
                posting_id,
                any(posting_name),
                any(posting_created_at),
                any(posting_delivered_at),
                item_id,
                SUM(item_quantity),
                SUM(item_quantity * marketplace_item_price),
                posting_source,
                payment_method,
                shipping_city,
                shipping_country
            FROM filtered_rows
            ARRAY JOIN
                item_ids as item_id,
                item_quantities as item_quantity,
                marketplace_item_prices as marketplace_item_price
            GROUP BY posting_id, item_id, shipping_city, shipping_country
            """);

        // command.Parameters.AddWithValue("sellerId", sellerId);
        // command.Parameters.AddWithValue("periodStart", periodStart);
        // command.Parameters.AddWithValue("periodEnd", periodEnd);

        await using var reader = await command.ExecuteReaderAsync(token);

        while (await reader.ReadAsync(token))
        {
            yield return new(
                PostingName: reader.GetString(1),
                PostingDate: reader.GetDateTimeOffset(2).DateTime,
                DeliveryDate: reader.GetDateTimeOffset(3).DateTime,
                ItemId: reader.GetInt64(4),
                ItemQuantity: reader.GetInt32(5),
                ItemTotal: reader.GetDecimal(6),
                PostingSource: reader.GetString(7),
                PaymentMethod: reader.GetString(8),
                ShippingCity: reader.GetString(9),
                ShippingCountry: reader.GetString(10));
        }
    }
}
