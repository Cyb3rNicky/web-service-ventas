using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WebServiceVentas.Data;
using WebServiceVentas.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using WebServiceVentas;

var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<VentasDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

builder.Services.AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

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

// ===== CORS (para pruebas: ABIERTO como política por defecto) =====
// Esto añade una política por defecto que permite cualquier origen/método/header.
// Es ideal para pruebas y asegura que TODAS las rutas tengan CORS sin tener que recordar el nombre.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p => p
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

// Auth
var jwtConfig = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddBearerToken(IdentityConstants.BearerScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
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


builder.Services.AddAuthorization(options =>
{
    // Admin exclusivo
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("admin"));

    // Gerente exclusivo
    options.AddPolicy("GerenteOnly", policy =>
        policy.RequireRole("gerente"));

    // Admin o Gerente
    options.AddPolicy("AdminOrGerente", policy =>
        policy.RequireRole("admin", "gerente"));

    // Vendedor o Admin
    options.AddPolicy("VendedorOrAdmin", policy =>
        policy.RequireRole("vendedor", "admin"));

    // Inventario o Admin
    options.AddPolicy("InventarioOrAdmin", policy =>
        policy.RequireRole("inventario", "admin"));

    // Asistente o Admin
    options.AddPolicy("AsistenteOrAdmin", policy =>
        policy.RequireRole("asistente", "admin"));

    // Cualquier usuario autenticado
    options.AddPolicy("Authenticated", policy =>
        policy.RequireAuthenticatedUser());

        // Admin, gerente o vendedor
    options.AddPolicy("AdminGerenteVendedor", policy =>
        policy.RequireRole("admin", "gerente", "vendedor"));

    // Admin, inventario o asistente
    options.AddPolicy("AdminInventarioAsistente", policy =>
        policy.RequireRole("admin", "inventario", "asistente"));
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

    // 🔹 Inicializar roles
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
    await RoleSeeder.SeedRolesAsync(roleManager);

    // 🔹 CREAR USUARIO ADMIN POR DEFECTO SI NO EXISTE
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
    var adminUser = await userManager.FindByNameAsync("admin");
    
    if (adminUser == null)
    {
        var user = new Usuario
        {
            UserName = "admin",
            Email = "admin@ventas.com",
            Nombre = "Administrador",
            Apellido = "Sistema"
        };
        
        var result = await userManager.CreateAsync(user, "AdminPassword123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "admin");
            Console.WriteLine("✅ Usuario admin creado:");
            Console.WriteLine("📧 Usuario: admin");
            Console.WriteLine("🔑 Password: AdminPassword123!");
            Console.WriteLine("⚠️  CAMBIA ESTE PASSWORD INMEDIATAMENTE!");
        }
        else
        {
            Console.WriteLine("❌ Error creando usuario admin:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"- {error.Description}");
            }
        }
    }
    else
    {
        Console.WriteLine("✅ Usuario admin ya existe");
    }
}

// ===== CORS ANTES de Auth =====
// Al no pasar nombre, usa la política por defecto (la abierta).
app.UseCors();

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

