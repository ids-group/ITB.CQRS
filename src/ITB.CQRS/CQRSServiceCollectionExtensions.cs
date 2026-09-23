using System.Reflection;
using FluentValidation;
using ITB.CQRS.Abstraction;
using ITB.CQRS.Decorators;
using ITB.CQRS.Models;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace ITB.CQRS;

public static class CQRSServiceCollectionExtensions
{
    public static IServiceCollection AddCQRS(this IServiceCollection services, Assembly[] assemblies, Action<CQRSOptions> setupAction = null)
    {
        var mapsterConfig = TypeAdapterConfig.GlobalSettings;
        mapsterConfig.Scan(assemblies);
        services.AddSingleton(mapsterConfig);
        services.AddScoped<IMapper, ServiceMapper>();

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime());

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.AssignableTo(typeof(IAccessFilter<>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime());

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.AssignableTo(typeof(IPermissionValidator<>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime());

        services.AddScoped<IHandlerDispatcher, HandlerDispatcher>();

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.AssignableTo(typeof(IHandler<,>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime());

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.AssignableTo(typeof(IHandler<>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime());


        services.TryDecorate(typeof(IHandler<,>), typeof(TransactionHandlerDecorator<,>));
        services.Decorate(typeof(IHandler<,>), typeof(ValidationHandlerDecorator<,>));
        services.Decorate(typeof(IHandler<,>), typeof(PermissionValidationHandlerDecorator<,>));
        services.Decorate(typeof(IHandler<,>), typeof(ErrorHandlerDecorator<,>));

        services.TryDecorate(typeof(IHandler<>), typeof(TransactionHandlerDecorator<>));
        services.TryDecorate(typeof(IHandler<>), typeof(ValidationHandlerDecorator<>));
        services.TryDecorate(typeof(IHandler<>), typeof(PermissionValidationHandlerDecorator<>));
        services.TryDecorate(typeof(IHandler<>), typeof(ErrorHandlerDecorator<>));

        if (setupAction != null)
        {
            services.Configure(setupAction);
        }

        return services;
    }
}
