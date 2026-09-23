using System.Reflection;
using ITB.Domain.Interfaces;
using ITB.Repository.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ITB.Repository.EntityFrameworkCore;

public static class RepositoryServiceCollectionExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        services.TryAddTransient(typeof(IRepository<>), typeof(Repository<>));
        services.TryAddTransient(typeof(IReadRepository<>), typeof(ReadRepository<>));
        services.TryAddTransient<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static IServiceCollection AddQueryableFilters(this IServiceCollection services, params Assembly[] assemblies)
    {
        if (assemblies != null && assemblies.Length > 0)
        {
            var allTypes = assemblies
                .Where(a => !a.IsDynamic)
                .Distinct()
                .SelectMany(a => a.DefinedTypes)
                .ToArray();

            foreach (var type in allTypes
                         .Where(t => t.IsClass
                                     && !t.IsAbstract
                                     && t.AsType().ImplementsGenericInterface(typeof(IQueryableFilter<>))))
            {
                var @interface = type.ImplementedInterfaces.First(i => i.IsGenericType(typeof(IQueryableFilter<>)));
                services.AddTransient(@interface, type.AsType());
            }
        }

        return services;
    }

    internal static bool ImplementsGenericInterface(this Type type, Type interfaceType)
        => type.IsGenericType(interfaceType) || type.GetTypeInfo().ImplementedInterfaces.Any(@interface => @interface.IsGenericType(interfaceType));

    internal static bool IsGenericType(this Type type, Type genericType)
        => type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == genericType;
}
