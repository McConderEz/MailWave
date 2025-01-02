using MailWave.Accounts.Application.Managers;
using MailWave.Accounts.Application.Providers;
using MailWave.Accounts.Application.Repositories;
using MailWave.Accounts.Infrastructure.Managers;
using MailWave.Accounts.Infrastructure.Repositories;
using MailWave.Core.Common;
using MailWave.Core.Options;
using MailWave.Framework;
using MailWave.SharedKernel.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDatabaseSettings = MailWave.Accounts.Infrastructure.Options.MongoDatabaseSettings;

namespace MailWave.Accounts.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAccountInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddDbContext(configuration)
            .AddJwtAuthentication(configuration)
            .AddProviders(configuration)
            .AddRepositories();

        return services;
    }


    private static IServiceCollection AddProviders(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IRefreshSessionManager, RefreshSessionManager>();
        services.AddTransient<IDateTimeProvider, DateTimeProvider>();

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }

    private static IServiceCollection AddDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoDatabaseSettings>(
            configuration.GetSection(MongoDatabaseSettings.Mongo) ?? throw new ApplicationException());
        
        services.AddScoped<AccountDbContext>();

        return services;
    }
    
    private static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTransient<ITokenProvider, JwtTokenProvider>();

        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.JWT) ?? throw new ApplicationException());
        
        services.Configure<RefreshSessionOptions>(
            configuration.GetSection(RefreshSessionOptions.REFRESH_SESSION) ?? throw new ApplicationException());

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var jwtOptions = configuration.GetSection(JwtOptions.JWT).Get<JwtOptions>()
                                 ?? throw new ApplicationException("missing jwt options");

                options.TokenValidationParameters =
                    TokenValidationParametersFactory.CreateWithLifeTime(jwtOptions);
            });

        return services;
    }
}