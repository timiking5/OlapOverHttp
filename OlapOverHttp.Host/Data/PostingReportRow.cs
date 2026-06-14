namespace OlapOverHttp.Host.Data;

public sealed record PostingReportRow(
    DateOnly PostingDate,
    DateOnly DeliveryDate,
    long ItemId,
    int ItemQuantity,
    decimal MarketplaceItemTotal,
    decimal SellerItemTotal,
    decimal SellerFxRate,
    sbyte SellerCurrency,
    string PaymentMethod,
    string ShippingCountry,
    string ShippingCity);
