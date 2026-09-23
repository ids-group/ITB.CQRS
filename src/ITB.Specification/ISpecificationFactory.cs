namespace ITB.Specification;

public interface ISpecificationFactory
{
    Specification<T> Create<T>();
}

public class SpecificationFactory : ISpecificationFactory
{
    public Specification<T> Create<T>()
    {
        return new DefaultSpecification<T>();
    }
}
