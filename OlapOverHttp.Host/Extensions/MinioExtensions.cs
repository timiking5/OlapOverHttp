using OlapOverHttp.Host.Excel;

namespace OlapOverHttp.Host.Extensions;

public static class MinioExtensions
{
    extension(ReportRequest request)
    {
        public string GetObjectName()
            => $"{request.SellerId}/{request.PeriodStart:yyyy-MM-dd}-{request.PeriodEnd:yyyy-MM-dd}";
    }
}
