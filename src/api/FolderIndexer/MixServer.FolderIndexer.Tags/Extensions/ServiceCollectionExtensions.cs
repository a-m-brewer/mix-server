using Microsoft.Extensions.DependencyInjection;
using MixServer.FolderIndexer.Tags.Factories;
using MixServer.FolderIndexer.Tags.Interface.Interfaces;

namespace MixServer.FolderIndexer.Tags.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFolderIndexerTagsServices(this IServiceCollection services)
    {
        services.AddTransient<ITagBuilderFactory, TagLibSharpTagBuilderFactory>();
        
        return services;
    }
}