using FindTime.Configurations;
using FindTime.Data;
using FindTime.Hubs;
using FindTime.Json;
using FindTime.Models;
using FindTime.Services.Implementations;
using FindTime.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;



var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
    options.JsonSerializerOptions.Converters.Add(new UtcNullableDateTimeConverter());
});
builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.Converters.Add(new UtcDateTimeConverter());
    options.PayloadSerializerOptions.Converters.Add(new UtcNullableDateTimeConverter());
});
builder.Services.AddIdentityConfig();
builder.Services.AddConnectionString(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddCookiesConfig(builder.Environment);
builder.Services.AddCorsConfig();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.
        WithTitle("FindTime API")
        .WithTheme(ScalarTheme.Moon)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);

    });
    Console.WriteLine("dev started");
}

app.UseHttpsRedirection();
app.UseCors("AllowVueApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.Run();