using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Users.Validators;
using MixServer.Domain.Utilities;

namespace MixServer.Domain.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMixServerDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IUserValidator, UserValidator>();

        services.AddDomainInterfaces();
        services.AddDomainUtilities();
        
        return services;
    }
    
    private static IServiceCollection AddDomainInterfaces(this IServiceCollection services)
    {
        bool InApplicationNamespace(Assembly assembly)
        {
            var assemblyName = assembly.GetName().Name;

            return !string.IsNullOrWhiteSpace(assemblyName) &&
                   assemblyName.StartsWith(nameof(MixServer));
        }
        
        // Handlers
        services.Scan(s => s.FromApplicationDependencies(InApplicationNamespace)
            .AddClasses(c => c.AssignableTo<IHandler>())
            .AsSelfWithInterfaces()
            .WithTransientLifetime());
                
        // Add Converters
        services.Scan(s => s.FromApplicationDependencies(InApplicationNamespace)
            .AddClasses(c => c.AssignableTo<IConverter>())
            .AsImplementedInterfaces()
            .WithTransientLifetime()
        );
        
        // Repositories
        services.Scan(s => s.FromApplicationDependencies(InApplicationNamespace)
            .AddClasses(c => c.AssignableTo<IScopedRepository>())
            .AsImplementedInterfaces()
            .WithScopedLifetime());
        
        services.Scan(s => s.FromApplicationDependencies(InApplicationNamespace)
            .AddClasses(c => c.AssignableTo<ITransientRepository>())
            .AsImplementedInterfaces()
            .WithTransientLifetime());
        
        services.Scan(s => s.FromApplicationDependencies(InApplicationNamespace)
            .AddClasses(c => c.AssignableTo<ISingletonRepository>())
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        return services;
    }
    
    private static IServiceCollection AddDomainUtilities(this IServiceCollection services)
    {
        services.AddTransient<IDateTimeProvider, DateTimeProvider>();
        services.AddTransient<IReadWriteLock, ReadWriteLock>();

        return services;
    }
}