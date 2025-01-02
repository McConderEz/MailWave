using MailWave.Accounts.Application;
using MailWave.Accounts.Contracts.Messaging;
using MailWave.Accounts.Infrastructure;
using MailWave.Core.Models;
using MailWave.Mail.Application.Features.Consumers.GetUserCredentialsForMail;
using MailWave.Mail.Controllers;
using MailWave.Mail.Infrastructure;
using MailWave.Web.Extensions;
using MassTransit;

namespace MailWave.Web;

public static class DependencyInjection
{
    public static IServiceCollection ConfigureWeb(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddLogger(configuration)
            .AddSwagger()
            .AddAccountsApplication()
            .AddAccountInfrastructure(configuration)
            .AddMailInfrastructure(configuration)
            .AddMailControllers()
            .AddCore()
            .AddMessageBus(configuration);

        return services;
    }

    private static IServiceCollection AddMessageBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMassTransit(configure =>
        {
            configure.SetKebabCaseEndpointNameFormatter();

            configure.AddConsumer<GotUserCredentialsForMailEventConsumer>();
            
            configure.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(new Uri(configuration["RabbitMQ:Host"]!), h =>
                {
                    h.Username(configuration["RabbitMQ:UserName"]!);
                    h.Password(configuration["RabbitMQ:Password"]!);
                });

                cfg.Durable = true;
                
                cfg.ConfigureEndpoints(context);
            });
        });
        
        return services;
    }
    
    private static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddScoped<MailCredentialsScopedData>();
        
        return services;
    }
}