using Microsoft.EntityFrameworkCore;
using Product.API.Services;
using Product.Application.Mapping;
using Product.API.Persistence;

var builder = WebApplication.CreateBuilder(args);
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


builder.Services.AddControllers();
//builder.Services.AddOpenApi();

// AutoMapper
builder.Services.AddAutoMapper(typeof(AppProfile));

// Service add di
builder.Services.AddScoped<ProductService>();

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(option =>
{
    var path = builder.Configuration.GetConnectionString("DefaultConnection");
    option.UseSqlite(path);
});

var app = builder.Build();

app.UseCors("AllowAll");

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Swagger UI Active
/*
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Rest API v1");
        options.RoutePrefix = string.Empty; // http://localhost:5235
    });
}
*/
app.MapControllers();
//app.UseHttpsRedirection();
app.Run();