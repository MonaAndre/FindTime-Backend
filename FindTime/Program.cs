using FindTime.Configurations;
using FindTime.Data;
using FindTime.Models;
using FindTime.Services.Implementations;
using FindTime.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;



var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
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
app.Run();