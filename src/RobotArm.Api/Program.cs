var builder = WebApplication.CreateBuilder(args);

// Controllers toevoegen
builder.Services.AddControllers();

// OpenAPI / Swagger documentatie
builder.Services.AddOpenApi();

var app = builder.Build();

// Alleen tijdens development OpenAPI beschikbaar maken
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Zorgt ervoor dat endpoints uit Controllers worden gevonden
app.MapControllers();

app.Run();