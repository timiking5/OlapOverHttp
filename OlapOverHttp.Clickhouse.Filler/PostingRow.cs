namespace OlapOverHttp.Clickhouse.Filler;

internal sealed record PostingRow(
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
