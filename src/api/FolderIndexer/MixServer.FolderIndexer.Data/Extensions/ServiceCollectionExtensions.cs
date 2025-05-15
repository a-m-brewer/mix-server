using Microsoft.Extensions.DependencyInjection;
using MixServer.FolderIndexer.Data.EF;
using MixServer.FolderIndexer.Data.EF.Repositories;
using MixServer.FolderIndexer.Domain;
using MixServer.FolderIndexer.Domain.Repositories;

namespace MixServer.FolderIndexer.Data.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFolderIndexerData<TContext>(this IServiceCollection services)
        where TContext : class, IFileIndexerDbContext
    {
        services.AddScoped<IFileIndexerDbContext>(sp => sp.GetRequiredService<TContext>());
        
        services.AddTransient<IFileSystemInfoRepository, EfFileSystemInfoRepository>();
        services.AddTransient<IFileSystemRootRepository, EfFileSystemRootRepository>();
        
        services.AddTransient<IFileIndexerUnitOfWork, EfFileIndexerUnitOfWork>();
        
        return services;
    }
}