using ITB.CQRS.Models;

namespace ITB.CQRS;

public static class QueryableExtensions
{
    // Page size used when the caller asks for none. Large enough to mean "everything" in practice.
    private const int DefaultItemsCount = 10000;

    // Only a negative page size falls back to the default. A negative one reached Postgres as LIMIT -1,
    // which is an error there, so any list endpoint answered 500 to ?paging.itemsCount=-1.
    //
    // Zero is deliberately left alone: LIMIT 0 is valid, and the handler counts rows before it pages, so
    // asking for zero rows is the cheapest way to read just the total. Large sizes are not capped either;
    // a page ceiling is the consuming application's decision.
    public static IQueryable<T> ApplyPageFilter<T>(this IQueryable<T> query, FilterBase filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        // Paging is a settable property on a model-bound body, so a caller posting "paging": null wipes what
        // the constructor put there. Read through a local and null-check below, otherwise a plain request for
        // the default page throws out of the query and answers 500.
        var paging = filter.Paging;

        var pageSize = paging?.ItemsCount is { } requested && requested >= 0 ? requested : DefaultItemsCount;

        // Cast to long before multiplying, not after: int times int is computed as an int, so a large page
        // index has already overflowed into a negative OFFSET by the time a long would see it.
        var skipAmount = (long)pageSize * Math.Max(paging?.Index ?? 0, 0);

        return query.Skip((int)Math.Min(skipAmount, int.MaxValue)).Take(pageSize);
    }
}
