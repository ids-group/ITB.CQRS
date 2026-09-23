using System.Linq.Expressions;

namespace ITB.Specification;

public class ExpressionSpecification<T>(Expression<Func<T, bool>> predicate) : Specification<T>
{
    public override Expression<Func<T, bool>> Predicate { get; } = predicate;
}
