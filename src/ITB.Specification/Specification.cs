using System.Linq.Expressions;

namespace ITB.Specification;

public abstract class Specification<T>
{
    public abstract Expression<Func<T, bool>> Predicate { get; }

    public bool IsSatisfiedBy(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var predicate = Predicate.Compile();
        return predicate(entity);
    }

    public Specification<T> And(Specification<T> specification)
    {
        return new AndSpecification<T>(this, specification);
    }

    public Specification<T> And(Expression<Func<T, bool>> right)
    {
        return new AndSpecification<T>(this, right.AsSpecification());
    }

    public Specification<T> Or(Specification<T> specification)
    {
        return new OrSpecification<T>(this, specification);
    }

    public Specification<T> Or(Expression<Func<T, bool>> right)
    {
        return new OrSpecification<T>(this, right.AsSpecification());
    }

    public Specification<T> Not()
    {
        return new NotSpecification<T>(this);
    }

    public static implicit operator Expression<Func<T, bool>>(Specification<T> spec) => spec.Predicate;
    public static implicit operator Func<T, bool>(Specification<T> spec) => spec.IsSatisfiedBy;

    public static bool operator false(Specification<T> spec) => false;
    public static bool operator true(Specification<T> spec) => false;

    public static Specification<T> operator &(Specification<T> spec1, Specification<T> spec2)
        => spec1.And(spec2);

    public static Specification<T> operator |(Specification<T> spec1, Specification<T> spec2)
        => spec1.Or(spec2);

    public static Specification<T> operator !(Specification<T> spec)
        => spec.Not();

    private readonly List<Expression<Func<T, object>>> _includes = new();
    private readonly List<string> _includeStrings = new();

    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes;
    public IReadOnlyList<string> IncludeStrings => _includeStrings;

    public virtual void AddInclude(IEnumerable<Expression<Func<T, object>>> includeExpressions)
    {
        _includes.AddRange(includeExpressions);
    }

    public virtual void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        _includes.Add(includeExpression);
    }

    public virtual void AddInclude(IEnumerable<string> includeStrings)
    {
        _includeStrings.AddRange(includeStrings);
    }

    public virtual void AddInclude(string includeString)
    {
        _includeStrings.Add(includeString);
    }
}
