using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WebServiceVentas.Data;
using WebServiceVentas.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<VentasDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Ventas API", Version = "v1" });

    // 🔑 Auth con JWT en Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT así: Bearer {tu_token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Render PORT
var portVar = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(portVar))
{
    builder.WebHost.UseUrls($"http://*:{portVar}");
}

// ===== CORS (para pruebas: ABIERTO) =====
// Permite cualquier origen/método/header (no usar con credenciales).
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod()
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
