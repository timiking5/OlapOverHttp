namespace OlapOverHttp.Host.Data;

public sealed class PostingRepositoryFactory(
    [FromKeyedServices("longterm")] IPostingRepository longTermRepository,
    [FromKeyedServices("shortterm")] IPostingRepository shortTermRepository)
{
    public IPostingRepository GetRepository(DateOnly queryDate)
    {
        return shortTermRepository;
    }
}
