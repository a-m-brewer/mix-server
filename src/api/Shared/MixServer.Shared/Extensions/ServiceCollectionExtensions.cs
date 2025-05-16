using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MixServer.Shared.Interfaces;

namespace MixServer.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMixServerSharedServices(this IServiceCollection services)
    {
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
        
        // Add Updaters
        services.Scan(s => s.FromApplicationDependencies(InApplicationNamespace)
            .AddClasses(c => c.AssignableTo<IUpdater>())
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

        bool InApplicationNamespace(Assembly assembly)
        {
            var assemblyName = assembly.GetName().Name;

            return !string.IsNullOrWhiteSpace(assemblyName) &&
                   assemblyName.StartsWith(nameof(MixServer));
        }
    }
}