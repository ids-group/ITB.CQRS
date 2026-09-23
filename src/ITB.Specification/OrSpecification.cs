using System.Linq.Expressions;

namespace ITB.Specification;

public class OrSpecification<T>(params Specification<T>[] specifications) : CompositeSpecification<T>(specifications)
{
    public override Expression<Func<T, bool>> Predicate
    {
        get
        {
            var firstSpecification = Specifications.First();

            if (Specifications.Length == 1)
                return firstSpecification.Predicate;

            return Specifications.Skip(1).Aggregate(firstSpecification.Predicate,
                (current, specification) => Or(current, specification.Predicate));
        }
    }

    private Expression<Func<T, bool>> Or(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        if (left == null)
            throw new ArgumentNullException(nameof(left));
        if (right == null)
            throw new ArgumentNullException(nameof(right));

        var visitor = new SwapVisitor(left.Parameters[0], right.Parameters[0]);
        var binaryExpression = Expression.OrElse(visitor.Visit(left.Body), right.Body);
        return Expression.Lambda<Func<T, bool>>(binaryExpression, right.Parameters);
    }
}
