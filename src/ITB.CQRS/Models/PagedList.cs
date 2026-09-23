namespace ITB.CQRS.Models;

public class PagedList<T>
{
    public List<T> Items { get; set; } = new();
    public int Count { get; set; }

    public PagedList()
    {
    }

    public PagedList(List<T> items, int count)
    {
        Items = items;
        Count = count;
    }
}
