using MailWave.Mail.Domain.Shared;
using MailWave.Mail.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MailWave.Mail.Infrastructure;

public static class DependencyInjection
{
   public static IServiceCollection AddMailInfrastructure(this IServiceCollection services,
      IConfiguration configuration)
   {
      services
         .AddServices()
         .AddValidators();
      
      return services;
   }

   private static IServiceCollection AddValidators(this IServiceCollection services)
   {
      services.AddTransient<EmailValidator>();

      return services;
   }
   
   private static IServiceCollection AddServices(this IServiceCollection services)
   {
      services.AddScoped<MailService>();

      return services;
   }
}