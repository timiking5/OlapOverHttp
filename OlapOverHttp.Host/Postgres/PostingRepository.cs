using System.Runtime.CompilerServices;
using Dapper;
using OlapOverHttp.Host.Data;
using OlapOverHttp.Host.Postgres.Infrastructure;

namespace OlapOverHttp.Host.Postgres;

public sealed class PostingRepository(IPostgresConnectionFactory connectionFactory) : IPostingRepository
{
    private const string Query =
        """
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
            WHERE seller_id = @sellerId
                AND posting_created_at BETWEEN @periodStart AND @periodEnd
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
            any_value(posting_name) AS PostingName,
            posting_created_at AS PostingDate,
            any_value(posting_delivered_at) AS DeliveryDate,
            item_id AS ItemId,
            SUM(item_quantity)::int AS ItemQuantity,
            SUM(item_quantity * marketplace_item_price) AS ItemTotal,
            posting_source AS PostingSource,
            payment_method AS PaymentMethod,
            shipping_city AS ShippingCity,
            shipping_country AS ShippingCountry
        FROM filtered_posting_with_items
        GROUP BY posting_id, posting_created_at, item_id, posting_source, payment_method, shipping_country, shipping_city
        """;

    public async IAsyncEnumerable<Postingtem> Get(
        long sellerId,
        DateOnly periodStart,
        DateOnly periodEnd,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(token);

        var parameters = new
        {
            sellerId,
            periodStart = periodStart.ToDateTime(TimeOnly.MinValue),
            periodEnd = periodEnd.ToDateTime(new TimeOnly(23, 59, 59)),
        };

        var command = new CommandDefinition(Query, parameters, cancellationToken: token);
        await using var reader = await connection.ExecuteReaderAsync(command);
        var parseRow = reader.GetRowParser<Postingtem>();

        while (await reader.ReadAsync(token))
            yield return parseRow(reader);
    }
}
