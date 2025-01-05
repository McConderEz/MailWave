using MailKit;
using MailWave.Mail.Domain.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IMailService = MailWave.Mail.Application.MailService.IMailService;
using MailService = MailWave.Mail.Infrastructure.Services.MailService;

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
      services.AddScoped<IMailService,MailService>();
      
      return services;
   }
}