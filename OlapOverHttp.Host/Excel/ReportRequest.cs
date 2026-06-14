namespace OlapOverHttp.Host.Excel;

public sealed record ReportRequest(
    long SellerId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd);
