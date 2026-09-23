using ITB.CQRS;
using ITB.CQRS.Models;
using Xunit;

namespace ITB.CQRS.UnitTests.Tests;

/// <summary>
/// <see cref="QueryableExtensions.ApplyPageFilter{T}"/>, which every filtered list query pages through.
/// </summary>
/// <remarks>
/// <para>
/// The paging arrives straight off the query string, so these are the values a caller can actually send
/// rather than the ones a client would. The bounds exist for the same reason — the offset is
/// <c>size × index</c> and reaches Postgres as <c>OFFSET n</c>, which refuses a negative — and both are
/// asserted here because a grid that skipped the guard would answer 500 rather than an empty page.
/// </para>
/// <para>
/// LINQ-to-Objects is more forgiving than Postgres and would not have produced the original 500 on its
/// own (<c>Skip(-n)</c> skips nothing, <c>Take(-n)</c> yields nothing), so these pin the clamping
/// behaviour rather than reproducing the error it prevents. The 500 itself is covered where it can be —
// against a real database, in the integration suites.
/// </para>
/// </remarks>
public class PageFilterTests
{
    private static readonly IQueryable<int> Rows = Enumerable.Range(1, 50).AsQueryable();

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_negative_page_size_falls_back_to_the_default(int itemsCount)
    {
        var paged = Rows.ApplyPageFilter(Filter(itemsCount, index: 0)).ToList();

        // The default is larger than the row set, so "everything" is what a caller asking for nonsense gets.
        Assert.Equal(Rows.ToList(), paged);
    }

    [Fact]
    public void An_absent_page_size_falls_back_to_the_default()
    {
        Assert.Equal(Rows.ToList(), Rows.ApplyPageFilter(new FilterBase()).ToList());
    }

    /// <summary>
    /// Zero rows is a request, not a mistake: the count is taken before the paging, so a caller that wants
    /// only the total asks for no rows and reads it off the envelope. Folded into the default, that made
    /// the cheapest read in the API the most expensive one — 10 000 rows over the wire for a caller that
    /// asked for none.
    /// </summary>
    [Fact]
    public void A_page_size_of_zero_is_an_empty_page_rather_than_the_default()
    {
        var paged = Rows.ApplyPageFilter(Filter(itemsCount: 0, index: 0)).ToList();

        Assert.Empty(paged);
    }

    /// <summary>
    /// A large page size is honoured rather than capped: <c>LIMIT</c> is answered with however many rows
    /// exist, so there is nothing to protect against here — and a cap would silently shorten a page
    /// nobody was told about.
    /// </summary>
    [Fact]
    public void A_large_page_size_is_honoured()
    {
        Assert.Equal(Rows.ToList(), Rows.ApplyPageFilter(Filter(int.MaxValue, index: 0)).ToList());
    }

    /// <summary>
    /// The regression. <c>size × index</c> is computed as an <see cref="int"/>, so the product wraps
    /// negative on its own — <c>itemsCount=100000&amp;index=30000</c> reached Postgres as
    /// <c>OFFSET -1294967296</c>, which it refuses. Widening the multiplication and saturating means the
    /// worst a caller can ask for is a page past the last row, which is an empty page.
    /// </summary>
    [Theory]
    [InlineData(100_000, 30_000)]
    [InlineData(int.MaxValue, int.MaxValue)]
    [InlineData(100_000, int.MaxValue)]
    public void A_page_far_past_the_end_is_empty_rather_than_a_negative_offset(int itemsCount, int index)
    {
        var paged = Rows.ApplyPageFilter(Filter(itemsCount, index)).ToList();

        Assert.Empty(paged);
    }

    [Fact]
    public void A_negative_page_index_reads_the_first_page()
    {
        var paged = Rows.ApplyPageFilter(Filter(itemsCount: 10, index: -3)).ToList();

        Assert.Equal(Enumerable.Range(1, 10), paged);
    }

    [Fact]
    public void An_ordinary_page_is_skipped_and_taken_as_asked()
    {
        var paged = Rows.ApplyPageFilter(Filter(itemsCount: 15, index: 2)).ToList();

        Assert.Equal(Enumerable.Range(31, 15), paged);
    }

    [Fact]
    public void A_null_paging_block_reads_the_default_page_rather_than_throwing()
    {
        // `Paging` is settable and these queries are model-bound from the request body, so a caller
        // posting `"paging": null` replaces what the constructor put there. Dereferenced, that answered
        // 500 to what is only a request for the default page.
        var filter = new FilterBase { Paging = null! };

        Assert.Equal(Rows.ToList(), Rows.ApplyPageFilter(filter).ToList());
    }

    /// <summary>
    /// The filter belongs to the caller — a model-bound query object that outlives this call — so the
    /// resolved page size stays local. Stamping the default onto it made a read helper's side effect into
    /// something the next reader of that object would have to know about.
    /// </summary>
    [Fact]
    public void The_callers_filter_is_left_as_it_was()
    {
        var filter = Filter(itemsCount: -1, index: -3);

        _ = Rows.ApplyPageFilter(filter).ToList();

        Assert.Equal(-1, filter.Paging.ItemsCount);
        Assert.Equal(-3, filter.Paging.Index);
    }

    [Fact]
    public void A_null_paging_block_is_not_replaced_on_the_callers_filter()
    {
        var filter = new FilterBase { Paging = null! };

        _ = Rows.ApplyPageFilter(filter).ToList();

        Assert.Null(filter.Paging);
    }

    private static FilterBase Filter(int itemsCount, int index) =>
        new() { Paging = new Paging { ItemsCount = itemsCount, Index = index } };
}
