using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ITB.Specification;

public static class SpecificationServiceCollectionExtensions
{
    public static IServiceCollection AddSpecificationFactory(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        services.TryAddTransient<ISpecificationFactory, SpecificationFactory>();
        return services;
    }
}
