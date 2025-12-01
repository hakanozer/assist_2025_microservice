using Microsoft.EntityFrameworkCore;
using Product.Infrastructure.Persistece;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddOpenApi();

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(option =>
{
    var path = builder.Configuration.GetConnectionString("DefaultConnection");
    option.UseSqlite(path);
});

var app = builder.Build();

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

app.UseHttpsRedirection();
app.Run();