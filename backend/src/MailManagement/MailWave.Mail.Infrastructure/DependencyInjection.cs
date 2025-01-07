using MailWave.Mail.Domain.Shared;
using MailWave.Mail.Infrastructure.Repositories;
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
         .AddValidators()
         .AddDatabase(configuration);
      
      return services;
   }

   private static IServiceCollection AddValidators(this IServiceCollection services)
   {
      services.AddTransient<EmailValidator>();

      return services;
   }

   private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
   {
      services.AddScoped<ApplicationDbContext>();

      services.AddScoped<LetterRepository>();

      services.AddScoped<UnitOfWork>();
      
      return services;
   }
    
   
   private static IServiceCollection AddServices(this IServiceCollection services)
   {
      services.AddScoped<MailService>();
      services.AddScoped<IMailService,MailService>();
      
      return services;
   }
}