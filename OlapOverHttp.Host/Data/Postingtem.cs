namespace OlapOverHttp.Host.Data;

public sealed record Postingtem(
    string PostingName,
    DateTime PostingDate,
    DateTime DeliveryDate,
    long ItemId,
    int ItemQuantity,
    decimal ItemTotal,
    string ShippingCity,
    string ShippingCountry);
