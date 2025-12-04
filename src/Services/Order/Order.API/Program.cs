using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Order.API.Saga;
using Order.API.utils;

var builder = WebApplication.CreateBuilder(args);
//builder.Services.AddHostedService<OutboxWorker>();

Console.WriteLine("Key: " + builder.Configuration["Jwt:Key"]);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
builder.Services.AddConsulServiceDiscovery(builder.Configuration);
builder.Services.AddHostedService<RabbitMQConsumer>();
//builder.Services.AddHostedService<CompensationConsumer>();
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(option =>
{
    var path = builder.Configuration.GetConnectionString("DefaultConnection");
    option.UseSqlite(path);
});

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddAuthorization();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.MapControllers();


app.Run();

