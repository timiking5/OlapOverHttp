using System.Runtime.CompilerServices;
using Octonica.ClickHouseClient;
using OlapOverHttp.Host.ClickHouse;

namespace OlapOverHttp.Host.Data;

public sealed class PostingRepository(IClickHouseConnectionFactory connectionFactory) : IPostingRepository
{
    public async IAsyncEnumerable<PostingReportRow> GetReportRows(
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

    public async IAsyncEnumerable<PostingDocumentSummary> GetDocumentSummary(
        long sellerId,
        DateOnly periodStart,
        DateOnly periodEnd,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(token);

        await using var command = connection.CreateCommand(
            $"""
            SELECT
                p.posting_id,
                p.posting_name,
                p.total_amount,
                p.posting_status,
                count(d.document_id) AS document_count,
                groupUniqArray(d.document_type) AS document_types,
                has(groupUniqArray(d.document_type), 'invoice') AS has_invoice,
                has(groupUniqArray(d.document_type), 'credit_note') AS has_credit_note
            FROM posting.postings p
            LEFT JOIN posting.posting_documents d
                ON p.posting_id = d.posting_id
                AND d.is_cancelled = false
            WHERE p.seller_id = {sellerId}
                AND p.posting_created_at BETWEEN '{periodStart:yyyy-MM-dd} 00:00:00' AND '{periodEnd:yyyy-MM-dd} 23:59:59'
            GROUP BY p.posting_id, p.posting_name, p.total_amount, p.posting_status
            HAVING document_count = 0 OR has_credit_note = true
            """);

        await using var reader = await command.ExecuteReaderAsync(token);

        while (await reader.ReadAsync(token))
        {
            yield return new(
                PostingId: reader.GetFieldValue<long>(reader.GetOrdinal("posting_id")),
                PostingName: reader.GetString(reader.GetOrdinal("posting_name")),
                TotalAmount: reader.GetDecimal(reader.GetOrdinal("total_amount")),
                PostingStatus: reader.GetString(reader.GetOrdinal("posting_status")),
                DocumentCount: reader.GetFieldValue<long>(reader.GetOrdinal("document_count")),
                DocumentTypes: reader.GetFieldValue<string[]>(reader.GetOrdinal("document_types")),
                HasInvoice: reader.GetBoolean(reader.GetOrdinal("has_invoice")),
                HasCreditNote: reader.GetBoolean(reader.GetOrdinal("has_credit_note")));
        }
    }

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
                ShippingCity: reader.GetString(7),
                ShippingCountry: reader.GetString(8));
        }
    }
}
