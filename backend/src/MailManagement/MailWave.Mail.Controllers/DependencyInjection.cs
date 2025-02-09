using MailWave.Mail.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace MailWave.Mail.Controllers;

public static class DependencyInjection
{
    public static IServiceCollection AddMailControllers(this IServiceCollection services)
    {
        services.AddScoped<IMailContract, MailContract>();
        
        return services;
    }
}