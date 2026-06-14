using OlapOverHttp.Host.Data;
using OlapOverHttp.Host.Postgres.Infrastructure;

namespace OlapOverHttp.Host.Postgres;

public sealed class PostingsRepository(IPostgresConnectionFactory connectionFactory) : IPostingRepository
{
    public async IAsyncEnumerable<Postingtem> Get(long sellerId, DateOnly periodStart, DateOnly periodEnd, CancellationToken token = default)
    {
        var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(token);

        var query =
            $"""
            WITH filtered_postings AS (
                SELECT
                    id,
                    posting_id,
                    posting_created_at,
                    posting_delivered_at,
                    posting_name,
                    seller_fx_rate,
                    seller_currency,
                    posting_source,
                    payment_method,
                    shipping_country,
                    shipping_city
                FROM posting.postings
                WHERE seller_id = {sellerId}
                    AND posting_created_at BETWEEN '{periodStart:yyyy-MM-dd} 00:00:00' AND '{periodEnd:yyyy-MM-dd} 23:59:59'
            ),
            filtered_posting_with_items AS (
                SELECT
                    fp.posting_id,
                    fp.posting_created_at,
                    fp.posting_delivered_at,
                    fp.posting_name,
                    fp.seller_fx_rate,
                    fp.seller_currency,
                    fp.posting_source,
                    fp.payment_method,
                    fp.shipping_country,
                    fp.shipping_city,
                    i.item_id,
                    i.item_quantity,
                    i.marketplace_item_price,
                    i.seller_item_price
                FROM filtered_postings fp
                JOIN posting.items i ON fp.id = i.posting_entry_id
            )
            SELECT
                posting_id,
                posting_created_at,
                any_value(posting_delivered_at),
                item_id,
                SUM(item_quantity),
                SUM(item_quantity * marketplace_item_price),
                SUM(item_quantity * seller_item_price),
                any_value(seller_fx_rate),  -- could be in group by, but doesn't have to be since there is only 1 currency per seller and posting created_at
                any_value(seller_currency),
                posting_source,
                payment_method,
                shipping_country,
                shipping_city
            FROM filtered_posting_with_items
            GROUP BY posting_id, posting_created_at, item_id, posting_source, payment_method, shipping_country, shipping_city
            """;


    }
}
