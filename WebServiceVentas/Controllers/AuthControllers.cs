using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;

        public AuthController(
            UserManager<Usuario> userManager, 
            SignInManager<Usuario> signInManager, 
            RoleManager<IdentityRole<int>> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // ================= Registro =================
        [HttpPost("register")]
public async Task<IActionResult> Register([FromBody] RegisterModel model)
{
    // Crear usuario
    var user = new Usuario
    {
        UserName = model.UserName,
        Email = model.Email,
        Nombre = model.Nombre,
        Apellido = model.Apellido
    };

    var result = await _userManager.CreateAsync(user, model.Password);
    if (!result.Succeeded)
        return BadRequest(result.Errors);

    // Crear rol si no existe
    if (!string.IsNullOrEmpty(model.Role))
    {
        if (!await _roleManager.RoleExistsAsync(model.Role))
        {
            var role = new IdentityRole<int> { Name = model.Role };
            await _roleManager.CreateAsync(role);
        }

        // Asignar rol al usuario
        await _userManager.AddToRoleAsync(user, model.Role);
    }

    return Ok(new
    {
        user.Id,
        user.UserName,
        user.Email,
        user.Nombre,
        user.Apellido,
        Role = model.Role
    });
}



        // ================= Login =================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, false, false);
            if (!result.Succeeded)
                return Unauthorized(new { message = "Usuario o contraseña incorrectos" });

            var user = await _userManager.FindByNameAsync(model.UserName);
            var roles = await _userManager.GetRolesAsync(user);

            // Generar token JWT con roles
            var jwtConfig = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetSection("Jwt");
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim("nombre", user.Nombre ?? ""),
                new Claim("apellido", user.Apellido ?? "")
            };

            // Agregar roles al token
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtConfig["Issuer"],
                audience: jwtConfig["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                user = new { user.Id, user.UserName, user.Email, user.Nombre, user.Apellido, Roles = roles }
            });
        }
    }

    // ================= Modelos =================
    public class RegisterModel
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } // <--- Nuevo campo para rol
    }

    public class LoginModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
