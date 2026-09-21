using Microsoft.EntityFrameworkCore;
using RobotArm.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Robot Arm API";
        document.Info.Version = "v1";

        return Task.CompletedTask;
    });
});

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // OpenAPI JSON
    app.MapOpenApi();

    // Swagger UI
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Robot Arm API v1");
    });
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();