using Microsoft.Extensions.DependencyInjection;
using MixServer.FolderIndexer.Api;
using MixServer.FolderIndexer.HostedServices;
using MixServer.FolderIndexer.Interface.Api;
using MixServer.FolderIndexer.Persistence.InMemory;
using MixServer.FolderIndexer.Services;
using MixServer.FolderIndexer.Tags.Extensions;

namespace MixServer.FolderIndexer.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFolderIndexer(this IServiceCollection services)
    {
        services.AddHostedService<FieSystemIndexerPersistenceBackgroundService>();
        services.AddHostedService<FileSystemIndexerBackgroundService>();

        services.AddTransient<IFolderIndexerScannerApi, FolderIndexerScannerApi>();
        services.AddTransient<IFolderIndexerFileSystemApi, FolderIndexerFileSystemApi>();
            
        services.AddTransient<IFileSystemScannerService, FileSystemScannerService>();
        services.AddTransient<IFileSystemPersistenceService, FileSystemPersistenceService>();
        services.AddTransient<IFileSystemRootPersistenceService, FileSystemRootPersistenceService>();
        
        services.AddSingleton<FileSystemIndexerChannelStore>();

        services.AddTransient<FileExtensionContentTypeProvider>();
        services.AddTransient<IFileSystemMetadataPersistenceService, FileSystemMetadataPersistenceService>();

        services.AddFolderIndexerTagsServices();
        
        return services;
    }
}