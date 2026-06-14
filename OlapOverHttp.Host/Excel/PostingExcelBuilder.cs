using Gooseberry.ExcelStreaming;
using OlapOverHttp.Host.Data;
using System.Collections.Frozen;

namespace OlapOverHttp.Host.Excel;

public sealed class PostingExcelBuilder(IPostingRepository postingRepository)
{
    private static readonly FrozenDictionary<sbyte, string> CurrencyMapping = new Dictionary<sbyte, string>
    {
        { 1, "EUR" },
        { 2, "GBP" },
        { 3, "USD" },
        { 4, "AED" },
    }.ToFrozenDictionary();

    private const int MaxRowsOnPage = 1_000_000;

    private static readonly IReadOnlyCollection<Column> Columns =
    [
        new(20.00m),
        new(20.00m),
        new(20.00m),
        new(20.00m),
        new(20.00m),
        new(20.00m),
        new(20.00m),
        new(20.00m),
        new(20.00m),
        new(20.00m),
        new(20.00m),
        new(20.00m),
        new(20.00m)
    ];

    private static readonly IReadOnlyCollection<string> ColumnTitles =
    [
        "№",
        "Дата доставки",
        "Дата заказа",
        "СКУ",
        "Количество",
        "Стоимость в валюте маркетплейса",
        "Валюта маркетплейса",
        "Стоимость в валюте селлера",
        "Валюта селлера",
        "Курс обмена из валюты селлера в валюта маркетплейса на момент заказа",
        "Способ оплаты",
        "Страна доставки",
        "Город доставки"
    ];

    public async Task Build(
        long sellerId,
        DateOnly periodStart,
        DateOnly periodEnd,
        Stream stream,
        CancellationToken token)
    {
        await using var writer = new ExcelWriter(stream, token: token);

        var rows = postingRepository.GetReportRows(sellerId, periodStart, periodEnd, token);

        await AddRows(writer, rows, periodStart, periodEnd, token);

        await writer.Complete();
        await stream.FlushAsync(token);
    }

    private async Task AddRows(
        ExcelWriter writer,
        IAsyncEnumerable<PostingReportRow> postings,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken token)
    {
        var pageRowCounter = new PageRowCounter(MaxRowsOnPage);
        await foreach (var posting in postings.WithCancellation(token))
        {
            pageRowCounter.AddNewRow(out var startNewPage);
            if (startNewPage)
                await StartNewPage(writer, periodStart, periodEnd, pageRowCounter.PageNumber);

            await writer.StartRow(20m);

            writer.AddCell(pageRowCounter.RowNumber);
            writer.AddCell(posting.PostingDate);
            writer.AddCell(posting.DeliveryDate);
            writer.AddCell(posting.ItemId);
            writer.AddCell(posting.ItemQuantity);
            writer.AddCell(posting.MarketplaceItemTotal);
            writer.AddCell("EUR");
            writer.AddCell(posting.SellerItemTotal);
            writer.AddCell(CurrencyMapping[posting.SellerCurrency]);
            writer.AddCell(posting.SellerFxRate);
            writer.AddCell(posting.PaymentMethod);
            writer.AddCell(posting.ShippingCountry);
            writer.AddCell(posting.ShippingCity);
        }
    }

    private async Task StartNewPage(
        ExcelWriter writer,
        DateOnly periodStart,
        DateOnly periodEnd,
        int pageNumber)
    {
        var pageName = pageNumber == 1
            ? "Постинги"
            : $"Постинги {pageNumber}";

        await writer.StartSheet(
            pageName,
            new SheetConfiguration(
                Columns,
                ShowGridLines: false));

        await AddHeaders(writer);
    }

    private async Task AddHeaders(ExcelWriter writer)
    {
        await writer.StartRow(40m);

        foreach (var columnTitle in ColumnTitles)
            writer.AddCell(columnTitle);
    }
}
