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
        private readonly RoleManager<IdentityRole<int>> _roleManager; // 🔹 CAMBIADO A IdentityRole<int>
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<Usuario> userManager,
            SignInManager<Usuario> signInManager,
            RoleManager<IdentityRole<int>> roleManager, // 🔹 CAMBIADO A IdentityRole<int>
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        // ================= Registro =================
        [HttpPost("register")]
        [Authorize(Policy = "AdminOrGerente")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            try
            {
                // Validaciones básicas
                if (string.IsNullOrEmpty(model.UserName) || string.IsNullOrEmpty(model.Password))
                    return BadRequest(new { message = "Usuario y contraseña son requeridos" });

                // Verificar si usuario existe
                var existingUser = await _userManager.FindByNameAsync(model.UserName);
                if (existingUser != null)
                    return BadRequest(new { message = "El usuario ya existe" });

                var existingEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingEmail != null)
                    return BadRequest(new { message = "El email ya está registrado" });

                // Validar rol
                var roleToAssign = !string.IsNullOrEmpty(model.Role) ? model.Role.ToLower() : "vendedor";
                var allowedRoles = new[] { "admin", "vendedor" };
                
                if (!allowedRoles.Contains(roleToAssign))
                    return BadRequest(new { message = "Rol inválido. Solo se permiten 'admin' o 'vendedor'" });

                // Verificar si el rol existe, si no crearlo
                if (!await _roleManager.RoleExistsAsync(roleToAssign))
                {
                    await _roleManager.CreateAsync(new IdentityRole<int>(roleToAssign));
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
                {
                    var errors = result.Errors.Select(e => e.Description);
                    return BadRequest(new { message = "Error al crear usuario", errors });
                }

                // Asignar rol
                await _userManager.AddToRoleAsync(user, roleToAssign);

                return Ok(new
                {
                    message = "Usuario registrado exitosamente",
                    data = new {
                        user.Id,
                        user.UserName,
                        user.Email,
                        user.Nombre,
                        user.Apellido,
                        Role = roleToAssign
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // ================= Login =================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.UserName) || string.IsNullOrEmpty(model.Password))
                    return BadRequest(new { message = "Usuario y contraseña son requeridos" });

                var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, false, false);
                
                if (!result.Succeeded)
                    return Unauthorized(new { message = "Usuario o contraseña incorrectos" });

                var user = await _userManager.FindByNameAsync(model.UserName);
                if (user == null)
                    return Unauthorized(new { message = "Usuario no encontrado" });

                var roles = await _userManager.GetRolesAsync(user);

                // Generar token JWT
                var jwtConfig = _configuration.GetSection("Jwt");
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"] ?? ""));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? ""),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                    new Claim("nombre", user.Nombre ?? ""),
                    new Claim("apellido", user.Apellido ?? "")
                };

                // Agregar roles al token
                claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

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
                    user = new { 
                        user.Id, 
                        user.UserName, 
                        user.Email, 
                        user.Nombre, 
                        user.Apellido, 
                        Roles = roles 
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error en el login", error = ex.Message });
            }
        }

        // ================= Cambiar Contraseña =================
        [HttpPost("cambiar-password")]
        [Authorize]
        public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordModel model)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // ================= Resetear Contraseña (Solo admin) =================
        [HttpPost("resetear-password/{userId:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ResetearPassword(int userId, [FromBody] ResetearPasswordModel model)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // ================= Obtener Usuario Actual =================
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetUsuarioActual()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                var roles = await _userManager.GetRolesAsync(user);

                return Ok(new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    user.Nombre,
                    user.Apellido,
                    Roles = roles
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }
    }

    // ================= Modelos =================
    public class RegisterModel
    {
        public required string UserName { get; set; }
        public required string Email { get; set; }
        public required string Nombre { get; set; }
        public required string Apellido { get; set; }
        public required string Password { get; set; }
        public required string Role { get; set; }
    }

    public class LoginModel
    {
        public required string UserName { get; set; }
        public required string Password { get; set; }
    }
}

public class CambiarPasswordModel
{
    [Required(ErrorMessage = "La contraseña actual es requerida")]
    public required string PasswordActual { get; set; }

    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public required string PasswordNueva { get; set; }

    [Compare("PasswordNueva", ErrorMessage = "Las contraseñas no coinciden")]
    public required string ConfirmarPasswordNueva { get; set; }
}

public class ResetearPasswordModel
{
    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public required string PasswordNueva { get; set; }

    [Compare("PasswordNueva", ErrorMessage = "Las contraseñas no coinciden")]
    public required string ConfirmarPasswordNueva { get; set; }
}