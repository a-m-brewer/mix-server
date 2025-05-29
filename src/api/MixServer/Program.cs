#region Builder

using System.Diagnostics;
using System.Reflection;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.FileProviders;
using MixServer;
using MixServer.SignalR;
using MixServer.Application;
using MixServer.Auth;
using MixServer.Auth.Requirements.IsInRole;
using MixServer.Auth.Requirements.PasswordReset;
using MixServer.Domain.Callbacks;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Extensions;
using MixServer.Domain.FileExplorer.Services.Caching;
using MixServer.Domain.FileExplorer.Settings;
using MixServer.Domain.Settings;
using MixServer.Domain.Streams.Models;
using MixServer.Domain.Users.Enums;
using MixServer.Domain.Users.Services;
using MixServer.Infrastructure;
using MixServer.Infrastructure.EF;
using MixServer.Infrastructure.EF.Entities;
using MixServer.Infrastructure.Extensions;
using MixServer.Infrastructure.Server.Settings;
using MixServer.Infrastructure.Users.Services;
using MixServer.Infrastructure.Users.Settings;
using MixServer.NSwag;
using MixServer.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

var runningFromNSwag = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "nswag";

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider((context, options) =>
{
    options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
    options.ValidateOnBuild = true;
});

// Fluent Validation
builder.Services
    .AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters()
    .AddValidatorsFromAssemblyContaining<Program>()
    .AddValidatorsFromAssemblyContaining<ApplicationScanType>()
    .AddValidatorsFromAssemblyContaining<InfrastructureScanType>();

// Problem Details
builder.Services.AddProblemDetails(setup =>
{
    setup.IncludeExceptionDetails = (_, _) => false;
    setup.Map<ValidationException>(exception =>
    {
        var errors = exception.Errors
            .GroupBy(g => g.PropertyName)
            .ToDictionary(
                k => k.Key,
                failures => failures.Select(s => s.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Detail = exception.Message,
            Instance = exception.HelpLink,
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Error",
            Type = $"https://httpstatuses.com/{StatusCodes.Status400BadRequest}"
        };
    });

    setup.Map<InvalidRequestException>(exception => new ValidationProblemDetails(exception.Errors)
    {
        Detail = exception.Message,
        Instance = exception.HelpLink,
        Status = StatusCodes.Status400BadRequest,
        Title = "Validation Error",
        Type = $"https://httpstatuses.com/{StatusCodes.Status400BadRequest}"
    });

    setup.Map<NotFoundException>(exception =>
    {
        var errors = new Dictionary<string, string[]>
        {
            { "message", [exception.Message] }
        };

        return new ValidationProblemDetails(errors)
        {
            Detail = exception.Message,
            Instance = exception.HelpLink,
            Status = StatusCodes.Status404NotFound,
            Title = "Item not Found",
            Type = $"https://httpstatuses.com/{StatusCodes.Status404NotFound}"
        };
    });
    setup.Map<ConflictException>(exception =>
    {
        var errors = new Dictionary<string, string[]>
        {
            { "message", [exception.Message] }
        };

        return new ValidationProblemDetails(errors)
        {
            Detail = exception.Message,
            Instance = exception.HelpLink,
            Status = StatusCodes.Status409Conflict,
            Title = "Item already exists",
            Type = $"https://httpstatuses.com/{StatusCodes.Status409Conflict}"
        };
    });
    setup.MapToStatusCode<NotSupportedException>(StatusCodes.Status400BadRequest);
    setup.MapToStatusCode<UnauthorizedRequestException>(StatusCodes.Status401Unauthorized);
    setup.Map<ForbiddenRequestException>(exception =>
    {
        var errors = new Dictionary<string, string[]>
        {
            { exception.Property ?? exception.GetType().Name, [exception.Message] }
        };

        return new ValidationProblemDetails(errors)
        {
            Detail = exception.Message,
            Instance = exception.HelpLink,
            Status = StatusCodes.Status403Forbidden,
            Title = "Validation Error",
            Type = $"https://httpstatuses.com/{StatusCodes.Status403Forbidden}"
        };
    });
});

// Application Services
builder.Services
    .AddMixServerDomainServices()
    .AddMixServerInfrastructureServices(builder.Configuration.GetConnectionString("DefaultConnection")!);

builder.Services
    .AddTransient<IBootstrapper, Bootstrapper>();

builder.Services.AddHostedService<FolderMediaMetadataSubscriber>();
builder.Services.AddHostedService<UpdateMediaMetadataBackgroundService>();
builder.Services.AddHostedService<RemoveMediaMetadataBackgroundService>();
builder.Services.AddHostedService<TranscodeBackgroundService>();
builder.Services.AddHostedService<DeviceDetectionBackgroundService>();

builder.Services.AddTransient<AbsolutePathMigrationService>();

// API Controllers
builder.Services.AddControllers()
    .AddNewtonsoftJson(options => { options.SerializerSettings.Converters.Add(new StringEnumConverter()); });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes =
        ResponseCompressionDefaults.MimeTypes.Concat(["text/plain", "application/json" ]);
});

// Configuration
const string environmentVariablePrefix = "MIX_SERVER_";
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(c =>
    {
        c.Prefix = environmentVariablePrefix;
    });

builder.Services.Configure<RootFolderSettings>(builder.Configuration.GetSection(ConfigSection.RootFolder));

var hostSettingsSection = builder.Configuration.GetSection(ConfigSection.HostSettings);
var hostSettings = hostSettingsSection.Get<HostSettings>() ?? new HostSettings();
builder.Services.Configure<HostSettings>(hostSettingsSection);

var jwtSettings = builder.Configuration.GetSection(ConfigSection.Jwt).Get<JwtSettings>() ?? new JwtSettings();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(ConfigSection.Jwt));

builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

builder.Services.Configure<InitialUserSettings>(builder.Configuration.GetSection(ConfigSection.InitialUser));

builder.Services.Configure<FolderCacheSettings>(builder.Configuration.GetSection(ConfigSection.FolderCache));

builder.Services.Configure<CacheFolderSettings>(builder.Configuration.GetSection(ConfigSection.CacheSettings));

builder.Services.Configure<FfmpegSettings>(builder.Configuration.GetSection(ConfigSection.Ffmpeg));

// NSwag
builder.Services.AddSwaggerDocument(settings =>
{
    settings.DocumentProcessors.Add(
        new NSwagSignalRTypesDocumentProcessor<SignalRCallbackHub, ISignalRCallbackClient>());
});

// SignalR
builder.Services.AddSignalR()
    .AddNewtonsoftJsonProtocol(options =>
    {
        options.PayloadSerializerSettings.Converters.Add(new StringEnumConverter(namingStrategy: new DefaultNamingStrategy(),
            allowIntegerValues: false));
        options.PayloadSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
    });
builder.Services.AddTransient<ICallbackService, SignalRCallbackService>();
builder.Services.AddSingleton<ISignalRUserManager, SignalRUserManager>();
builder.Services.AddSingleton<IConnectionManager>(sp => sp.GetRequiredService<ISignalRUserManager>());

// CORS
const string mixServerCorsPolicy = nameof(mixServerCorsPolicy);
builder.Services.AddCors(c =>
{
    c.AddPolicy(mixServerCorsPolicy, options =>
    {
        options.WithOrigins(hostSettings.ValidUrlsSplit)
            .AllowCredentials()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Identity
builder.Services
    .AddIdentity<DbUser, IdentityRole>()
    .AddEntityFrameworkStores<MixServerDbContext>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = JwtService.GetTokenValidationParameters(hostSettings, jwtSettings);

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];

                // If the request is for our hub...

                var path = context.HttpContext.Request.Path;

                var hasAccessToken = !string.IsNullOrEmpty(accessToken);
                var isSignalRHub = path.StartsWithSegments("/callbacks");
                    
                if (hasAccessToken && isSignalRHub)
                {
                    // Read the token out of the query string
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services
    .AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PasswordResetRequirement())
            .Build();
        
        options.AddPolicy(Policies.PasswordReset, policyBuilder =>
        {
            policyBuilder.RequireAuthenticatedUser();
        });
        
        options.AddPolicy(Policies.IsAdminOrOwner, policyBuilder =>
            policyBuilder
                .RequireAuthenticatedUser()
                .AddRequirements(
                    new PasswordResetRequirement(),
                    new IsInRoleRequirement(new List<Role> { Role.Administrator, Role.Owner })));
    })
    .AddSingleton<IAuthorizationHandler, IsInRoleAuthorizationHandler>()
    .AddSingleton<IAuthorizationHandler, PasswordResetRequirementAuthorizationHandler>();

#endregion

#region App

var app = builder.Build();

app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var sw = Stopwatch.StartNew();

    try
    {
        await next();
    }
    finally
    {
        sw.Stop();
        var elapsedMilliseconds = sw.ElapsedMilliseconds;
        logger.LogInformation(
            "Request {Method} {Path} completed in {ElapsedMilliseconds} ms with status code {StatusCode}",
            context.Request.Method,
            context.Request.Path,
            elapsedMilliseconds,
            context.Response.StatusCode);
    }
});

// Response Compression
app.UseResponseCompression();

// Proxy Settings
app.UseForwardedHeaders();

// Problem Detains
app.UseProblemDetails();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}

app.UseRouting();

// CORS
app.UseCors(mixServerCorsPolicy);

// Host the Angular App
if (!runningFromNSwag)
{
    var entryAssemblyLocationPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ??
                                    throw new InvalidOperationException("Assembly location returned null");
    var fileProvider = new PhysicalFileProvider(Path.Combine(entryAssemblyLocationPath, @"wwwroot"));

    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.HasValue
            ? context.Request.Path.Value
            : string.Empty;
        
        if (path.StartsWith("/api") ||
            path.StartsWith("/callbacks") ||
            path.StartsWith("/swagger") ||
            Path.HasExtension(path))
        {
            await next();
            return;
        }

        context.Request.Path = "/index.html";
        await next();
    });
    
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = fileProvider
    });

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = fileProvider
    });
    
    app.UseWhen(context => context.Request.Path.StartsWithSegments("/swagger"),
        appBuilder =>
        {
            appBuilder.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = fileProvider
            });
        });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SignalRCallbackHub>("/callbacks");

if (!runningFromNSwag)
{
    if (string.IsNullOrWhiteSpace(jwtSettings.SecurityKey))
    {
        app.Services.GetRequiredService<ILogger<Program>>()
            .LogCritical($"Missing security key please specify in appsettings.json ({ConfigSection.Jwt}.{nameof(jwtSettings.SecurityKey)}) or {environmentVariablePrefix}{ConfigSection.Jwt}__{nameof(jwtSettings.SecurityKey)}");
        Environment.Exit(1);
        return;
    }
    
    using var scope = app.Services.CreateScope();

    var bootstrapper = scope.ServiceProvider.GetRequiredService<IBootstrapper>();
    await bootstrapper.GoAsync();
}

app.Run();

# endregion