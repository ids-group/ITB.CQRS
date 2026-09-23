using System.Linq.Expressions;

namespace ITB.Specification;

public class NotSpecification<T>(Specification<T> left) : Specification<T>
{
    private readonly Specification<T> _left = left ?? throw new ArgumentNullException(nameof(left));

    public override Expression<Func<T, bool>> Predicate => Not(_left.Predicate);

    private static Expression<Func<T, bool>> Not(Expression<Func<T, bool>> left)
    {
        if (left == null)
            throw new ArgumentNullException(nameof(left));

        var notExpression = Expression.Not(left.Body);
        return Expression.Lambda<Func<T, bool>>(notExpression, left.Parameters.Single());
    }
}
