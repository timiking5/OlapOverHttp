using Microsoft.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace OlapOverHttp.Host.Extensions;

public static class HttpExtensions
{
    private static readonly Regex IllegalCharacters =
        new($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]", RegexOptions.Compiled);

    public static void SetReportResponseHeaders(this HttpResponse response, string reportName)
    {
        var fileName = IllegalCharacters.Replace(reportName, "_");
        var attachment = new ContentDispositionHeaderValue("attachment") { FileNameStar = fileName };
        response.GetTypedHeaders().ContentDisposition = attachment;
        response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    }
}
