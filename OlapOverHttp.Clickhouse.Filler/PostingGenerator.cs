namespace OlapOverHttp.Clickhouse.Filler;

internal static class PostingGenerator
{
    private static readonly string[] PostingSources = ["web", "mobile", "api", "marketplace"];
    private static readonly string[] PaymentMethods = ["card", "bank_transfer", "wallet", "cod"];
    private static readonly string[] ShippingCountries = ["US", "DE", "FR", "GB", "ES", "IT", "PL", "NL"];
    private static readonly string[] ShippingCities =
    [
        "Berlin", "Paris", "London", "Madrid", "Rome", "Warsaw", "Amsterdam", "New York", "Chicago", "Los Angeles"
    ];
    private static readonly sbyte[] SellerCurrencies = [1, 2, 3, 4];

    internal static PostingRow Generate(
        long postingId,
        IReadOnlyList<long> sellerIds,
        DateTime periodStart,
        DateTime periodEnd,
        int monthIndex,
        int monthsInPeriod)
    {
        var sellerId = sellerIds[Random.Shared.Next(sellerIds.Count)];

        var postingName = $"posting-{postingId}";
        var (monthStart, monthTicks) = GetMonthBounds(periodStart, periodEnd, monthIndex, monthsInPeriod);
        var createdAt = monthStart.AddTicks(Random.Shared.NextInt64(monthTicks));
        var deliveredAt = createdAt.AddDays(Random.Shared.Next(1, 15));
        var postingSource = PostingSources[Random.Shared.Next(PostingSources.Length)];
        var paymentMethod = PaymentMethods[Random.Shared.Next(PaymentMethods.Length)];
        var shippingCountry = ShippingCountries[Random.Shared.Next(ShippingCountries.Length)];
        var shippingCity = ShippingCities[Random.Shared.Next(ShippingCities.Length)];
        var totalAmount = Random.Shared.Next(500, 500_001) / 100m;
        var sellerCurrency = SellerCurrencies[Random.Shared.Next(SellerCurrencies.Length)];
        var sellerFxRate = Random.Shared.Next(80, 121) / 100m;

        var itemCount = Random.Shared.Next(1, 6);
        var itemIds = new long[itemCount];
        var itemQuantities = new int[itemCount];
        var marketplaceItemPrices = new decimal[itemCount];
        var sellerItemPrices = new decimal[itemCount];

        for (var itemIndex = 0; itemIndex < itemCount; itemIndex++)
        {
            itemIds[itemIndex] = Random.Shared.NextInt64(1, 10_000_000);
            itemQuantities[itemIndex] = Random.Shared.Next(1, 11);
            marketplaceItemPrices[itemIndex] = Random.Shared.Next(100, 20_001) / 100m;
            sellerItemPrices[itemIndex] = Math.Round(marketplaceItemPrices[itemIndex] * sellerFxRate, 6);
        }

        return new PostingRow(
            PostingId: postingId,
            PostingName: postingName,
            SellerId: sellerId,
            ItemIds: itemIds,
            ItemQuantities: itemQuantities,
            MarketplaceItemPrices: marketplaceItemPrices,
            SellerItemPrices: sellerItemPrices,
            SellerCurrency: sellerCurrency,
            SellerFxRate: sellerFxRate,
            PostingCreatedAt: createdAt,
            PostingDeliveredAt: deliveredAt,
            PostingSource: postingSource,
            TotalAmount: totalAmount,
            PaymentMethod: paymentMethod,
            ShippingCountry: shippingCountry,
            ShippingCity: shippingCity,
            Version: 0);
    }

    private static (DateTime MonthStart, long MonthTicks) GetMonthBounds(
        DateTime periodStart,
        DateTime periodEnd,
        int monthIndex,
        int monthsInPeriod)
    {
        var monthStart = periodStart.AddMonths(monthIndex);
        var monthEnd = monthIndex == monthsInPeriod - 1
            ? periodEnd
            : periodStart.AddMonths(monthIndex + 1);

        return (monthStart, (monthEnd - monthStart).Ticks);
    }
}
