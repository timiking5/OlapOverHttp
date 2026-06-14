namespace OlapOverHttp.Host.Excel;

public sealed class PageRowCounter(int maxRowOnPage)
{
    public int RowNumber { get; private set; }

    public int PageNumber { get; private set; }

    public void AddNewRow(out bool startNewPage)
    {
        if (RowNumber % maxRowOnPage == 0)
        {
            RowNumber++;
            PageNumber++;
            startNewPage = true;

            return;
        }

        RowNumber++;
        startNewPage = false;
    }
}
