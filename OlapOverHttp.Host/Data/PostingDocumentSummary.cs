namespace OlapOverHttp.Host.Data;

public sealed record PostingDocumentSummary(
    long PostingId,
    string PostingName,
    decimal TotalAmount,
    string PostingStatus,
    long DocumentCount,
    string[] DocumentTypes,
    bool HasInvoice,
    bool HasCreditNote);
