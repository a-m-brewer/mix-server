using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Services;
using MixServer.Domain.Sessions.Accessors;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Tracklists.Factories;
using MixServer.Domain.Users.Services;
using MixServer.Infrastructure.EF;
using MixServer.Infrastructure.Files.Services;
using MixServer.Infrastructure.Queueing.Repositories;
using MixServer.Infrastructure.Queueing.Services;
using MixServer.Infrastructure.Sessions.Accessors;
using MixServer.Infrastructure.Sessions.Services;
using MixServer.Infrastructure.Tracklist.Factories;
using MixServer.Infrastructure.Users.Repository;
using MixServer.Infrastructure.Users.Services;

namespace MixServer.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMixServerInfrastructureServices(this IServiceCollection services, string dbConnectionString)
    {
        services.AddTransient<FileExtensionContentTypeProvider>();
        services.AddTransient<IMimeTypeService, MimeTypeService>();

        services.AddDbContext<MixServerDbContext>(options =>
        {
            options.UseSqlite(dbConnectionString, builder => builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery));
        });
        services.AddScoped<IUnitOfWork, EfUnitOfWork<MixServerDbContext>>();

        services.AddScoped<ICurrentUserRepository, CurrentUserRepository>();
        services.AddTransient<IUserAuthenticationService, IdentityUserAuthenticationService>();
        services.AddTransient<IIdentityUserAuthenticationService, IdentityUserAuthenticationService>();
        services.AddTransient<IFirstUserInitializationService, FirstUserInitializationService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IStreamKeyService>(i => i.GetRequiredService<IJwtService>());
        services.AddTransient<IUserRoleService, IdentityUserUserRoleService>();
        services.AddTransient<IIdentityUserRoleService, IdentityUserUserRoleService>();
        services.AddTransient<IPasswordGeneratorService, PasswordGeneratorService>();

        services.AddTransient<ISessionService, SessionService>();
        services.AddSingleton<PlaybackTrackingService>();
        services.AddSingleton<IPlaybackTrackingService>(sp => sp.GetRequiredService<PlaybackTrackingService>());
        services.AddTransient<IPlaybackTrackingAccessor, PlaybackTrackingAccessor>();
        
        services.Scan(s => s.FromAssemblyOf<IRateLimiter>()
            .AddClasses(c => c.AssignableTo<IRateLimiter>())
            .AsImplementedInterfaces()
            .WithSingletonLifetime()
        );

        services.AddTransient<IQueueService, QueueService>();
        services.AddSingleton<IQueueRepository, QueueRepository>();

        services.AddTransient<IFileService, FileService>();

        services.AddTransient<IDeviceService, DeviceService>();
        services.AddTransient<IDeviceDetectionService, DeviceDetectionService>();
        services.AddSingleton<IDeviceTrackingService, DeviceTrackingService>();
        services.AddTransient<IRequestedPlaybackDeviceAccessor, RequestedPlaybackDeviceAccessor>();

        services.AddTransient<ITagBuilderFactory, TagLibSharpTagBuilderFactory>();
        
        return services;
    }
}