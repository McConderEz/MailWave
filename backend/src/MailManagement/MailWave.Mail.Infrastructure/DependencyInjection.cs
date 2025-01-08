﻿using MailWave.Mail.Domain.Shared;
using MailWave.Mail.Infrastructure.BackgroundServices;
using MailWave.Mail.Infrastructure.Dispatchers;
using MailWave.Mail.Infrastructure.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
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
         .AddDatabase(configuration)
         .AddRedisCache(configuration)
         .AddDispatchers()
         .AddBackgroundServices();
      
      return services;
   }

   private static IServiceCollection AddBackgroundServices(this IServiceCollection services)
   {
      services.AddHostedService<CleanupInactiveClientsBackgroundService>();

      return services;
   }

   private static IServiceCollection AddDispatchers(this IServiceCollection services)
   {
      services.AddSingleton<MailClientDispatcher>();

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

   private static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
   {
      services.AddStackExchangeRedisCache(options =>
      {
         options.Configuration = configuration.GetConnectionString("Redis");
      });
      
      #pragma warning disable EXTEXP0018
      services.AddHybridCache(options =>
      {
         options.MaximumPayloadBytes = 1024 * 1024 * 10; 
         options.MaximumKeyLength = 512;
         
         options.DefaultEntryOptions = new HybridCacheEntryOptions
         {
            Expiration = TimeSpan.FromMinutes(3),
            LocalCacheExpiration = TimeSpan.FromMinutes(3)
         };
      });
      #pragma warning restore EXTEXP0018
      return services;
   }
   
   private static IServiceCollection AddServices(this IServiceCollection services)
   {
      services.AddScoped<MailService>();
      services.AddScoped<IMailService,MailService>();
      
      return services;
   }
}