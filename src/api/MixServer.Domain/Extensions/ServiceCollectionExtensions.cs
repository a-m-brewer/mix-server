using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.FileExplorer.Services.Caching;
using MixServer.Domain.FileExplorer.Services.Indexing;
using MixServer.Domain.Persistence;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Sessions.Validators;
using MixServer.Domain.Streams.Caches;
using MixServer.Domain.Streams.Services;
using MixServer.Domain.Tracklists.Services;
using MixServer.Domain.Users.Validators;
using MixServer.Domain.Utilities;

namespace MixServer.Domain.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMixServerDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IUserValidator, UserValidator>();
        // services.AddSingleton<IFolderCacheService, FolderCacheService>();
        services.AddTransient<IFolderCacheService, IndexFolderCacheService>();
        services.AddSingleton<IFileNotificationService, FileNotificationService>();
        services.AddTransient<FileExplorerConverter, FileExplorerConverter>();
        services.AddSingleton<IRootFileExplorerFolder, RootFileExplorerFolder>();
        services.AddTransient<ISessionHydrationService, SessionHydrationService>();
        services.AddTransient<ITracklistTagService, TracklistTagService>();
        services.AddSingleton<ITranscodeCache, TranscodeCache>();
        services.AddTransient<ITranscodeService, TranscodeService>();
        services.AddTransient<ICanPlayOnDeviceValidator, CanPlayOnDeviceValidator>();
        services.AddSingleton<IMediaInfoCache, MediaInfoCache>();

        services.AddDomainUtilities();
        
        return services;
    }
    
    private static IServiceCollection AddDomainUtilities(this IServiceCollection services)
    {
        services.AddTransient<IDateTimeProvider, DateTimeProvider>();
        services.AddTransient<IReadWriteLock, ReadWriteLock>();

        return services;
    }
}