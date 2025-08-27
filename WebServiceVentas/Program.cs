using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WebServiceVentas.Data;
using WebServiceVentas.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<VentasDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Render PORT
var portVar = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(portVar))
{
    builder.WebHost.UseUrls($"http://*:{portVar}");
}

// ===== CORS =====
// Usa WithOrigins con tus dominios reales.
// Agrega/quita puertos locales según tu setup (5173/5174).
var allowedOrigins = new[]
{
    "http://localhost:5173",
    "http://localhost:5174",
    "https://TU-SITIO.netlify.app" // <-- cámbialo
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            // Si usarás cookies u otros credenciales del navegador, deja esta línea:
            // .AllowCredentials()
            // Si NO usarás credenciales, puedes omitir AllowCredentials().
    );
});

// Identity
builder.Services.AddIdentity<Usuario, IdentityRole<int>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<VentasDbContext>()
.AddDefaultTokenProviders();

// Auth (una sola configuración)
var jwtConfig = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddBearerToken(IdentityConstants.BearerScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtConfig["Issuer"],
        ValidAudience = jwtConfig["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]))
    };
});

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Migraciones
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VentasDbContext>();
    db.Database.Migrate();
}

// ===== Aplica CORS ANTES de Auth =====
app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Raíz y health
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});
app.MapGet("/healthz", () => Results.Ok("Healthy"));

app.Run();

public partial class Program { }
