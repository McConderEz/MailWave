using MailWave.Accounts.Infrastructure;
using MailWave.Web.Extensions;

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
            .AddAccountInfrastructure(configuration);

        return services;
    }
}