using MailWave.Accounts.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace MailWave.Accounts.Controllers;

public static class DependencyInjection
{
    public static IServiceCollection AddAccountControllers(this IServiceCollection services)
    {
        services.AddScoped<IAccountContract, AccountContract>();

        return services;
    }
}