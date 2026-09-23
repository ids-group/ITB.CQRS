namespace ITB.CQRS.Models;

public class FilterBase
{
    public FilterBase()
    {
        Paging = new Paging();
    }

    public string SearchText { get; set; }
    public Paging Paging { get; set; }
    public Sorting Sorting { get; set; }
}

public class Paging
{
    public int Index { get; set; }
    public int? ItemsCount { get; set; }
}

public enum SortOrder
{
    Asc,
    Desc
}

public class Sorting
{
    public string Path { get; set; }
    public SortOrder Order { get; set; } = SortOrder.Asc;
}
