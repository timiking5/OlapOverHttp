namespace OlapOverHttp.Postgres.Filler;

internal sealed record ClickHousePostingRow(
    long PostingId,
    string PostingName,
    long SellerId,
    long[] ItemIds,
    int[] ItemQuantities,
    decimal[] MarketplaceItemPrices,
    decimal[] SellerItemPrices,
    sbyte SellerCurrency,
    decimal SellerFxRate,
    DateTime PostingCreatedAt,
    DateTime PostingDeliveredAt,
    string PostingSource,
    decimal TotalAmount,
    string PaymentMethod,
    string ShippingCountry,
    string ShippingCity,
    sbyte Version);
