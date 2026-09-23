using System.Linq.Expressions;

namespace ITB.Specification;

public static class SpecificationExtensions
{
    public static Specification<T> AsSpecification<T>(this Expression<Func<T, bool>> expr)
        => new ExpressionSpecification<T>(expr);
}

public static class SpecificationQueryableExtensions
{
    public static IQueryable<T> Where<T>(this IQueryable<T> query, Specification<T> specification)
    {
        return query.Where(specification.Predicate);
    }
}
