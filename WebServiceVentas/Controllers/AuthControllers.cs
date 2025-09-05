using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
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
        [Authorize(Policy = "AdminOnly")] // SOLO ADMINS PUEDEN REGISTRAR
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            // Validar que solo admins puedan crear otros admins
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserRoles = await _userManager.GetRolesAsync(currentUser);

            var roleToAssign = !string.IsNullOrEmpty(model.Role) ? model.Role.ToLower() : "vendedor";

            // Si intenta crear un admin, verificar que el usuario actual sea admin
            if (roleToAssign == "admin" && !currentUserRoles.Contains("admin"))
            {
                return Forbid("No tienes permisos para crear usuarios admin");
            }

            // Validar roles permitidos
            var allowedRoles = new[] { "admin", "vendedor" };
            if (!allowedRoles.Contains(roleToAssign))
            {
                return BadRequest(new { message = "Rol inválido. Solo se permiten 'admin' o 'vendedor'" });
            }

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

            await _userManager.AddToRoleAsync(user, roleToAssign);

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.Nombre,
                user.Apellido,
                Role = roleToAssign
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


        // ================= Cambiar Contraseña =================
        [HttpPost("cambiar-password")]
        [Authorize] // Solo usuarios autenticados
        public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            // Verificar que la contraseña actual sea correcta
            var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, model.PasswordActual);
            if (!isCurrentPasswordValid)
            {
                return BadRequest(new { message = "La contraseña actual es incorrecta" });
            }

            // Cambiar la contraseña
            var result = await _userManager.ChangePasswordAsync(user, model.PasswordActual, model.PasswordNueva);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return BadRequest(new { message = "Error al cambiar la contraseña", errors });
            }

            return Ok(new { message = "Contraseña cambiada exitosamente" });
        }

        // ================= Resetear Contraseña (Solo admin) =================
        [HttpPost("resetear-password/{userId:int}")]
        [Authorize(Policy = "AdminOnly")] // Solo admin puede resetear passwords
        public async Task<IActionResult> ResetearPassword(int userId, [FromBody] ResetearPasswordModel model)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            // Generar token de reset
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Resetear la contraseña
            var result = await _userManager.ResetPasswordAsync(user, token, model.PasswordNueva);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return BadRequest(new { message = "Error al resetear la contraseña", errors });
            }

            return Ok(new { message = "Contraseña reseteada exitosamente" });
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
            public string Role { get; set; } // <--- Nuevo campo para rol  "admin" o "vendedor"
        }

    public class LoginModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}

public class CambiarPasswordModel
{
    [Required(ErrorMessage = "La contraseña actual es requerida")]
    public string PasswordActual { get; set; }

    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public string PasswordNueva { get; set; }

    [Compare("PasswordNueva", ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmarPasswordNueva { get; set; }
}

public class ResetearPasswordModel
{
    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public string PasswordNueva { get; set; }

    [Compare("PasswordNueva", ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmarPasswordNueva { get; set; }
}
