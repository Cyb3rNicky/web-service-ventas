using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;

var builder = WebApplication.CreateBuilder(args);

// Base de datos
builder.Services.AddDbContext<VentasDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("PostgresConnection")
    ));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Si está en Render (o cualquier hosting que use PORT), usar ese puerto.
// Si no, dejar que use el puerto del launchSettings.json
var portVar = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(portVar))
{
    builder.WebHost.UseUrls($"http://*:{portVar}");
}

var app = builder.Build();

// Swagger siempre habilitado (puedes condicionar si quieres solo en dev)
app.UseSwagger();
app.UseSwaggerUI();

// Migrar DB automáticamente
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VentasDbContext>();
    db.Database.Migrate();
}

app.UseAuthorization();
app.MapControllers();

app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

app.MapGet("/healthz", () => Results.Ok("Healthy"));

app.Run();

// Necesario para pruebas
public partial class Program { }
