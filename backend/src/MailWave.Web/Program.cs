using Hangfire;
using MailWave.Web;
using MailWave.Web.Extensions;
using MailWave.Web.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.ConfigureWeb(builder.Configuration);

var app = builder.Build();

app.UseExceptionMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(config =>
{
    config.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
});

app.UseHangfireServer();
app.UseHangfireDashboard();

app.MapControllers();

app.Run();