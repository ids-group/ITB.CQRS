namespace ITB.Specification;

public abstract class CompositeSpecification<T> : Specification<T>
{
    protected readonly Specification<T>[] Specifications;

    protected CompositeSpecification(params Specification<T>[] specifications)
    {
        if (specifications == null || !specifications.Any())
            throw new ArgumentNullException(nameof(specifications));

        foreach (var specification in specifications)
        {
            base.AddInclude(specification.Includes);
            base.AddInclude(specification.IncludeStrings);
        }

        Specifications = specifications;
    }
}
