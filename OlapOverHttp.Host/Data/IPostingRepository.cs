namespace OlapOverHttp.Host.Data;

public interface IPostingRepository
{
    IAsyncEnumerable<Postingtem> Get(
        long sellerId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken token = default);
}
