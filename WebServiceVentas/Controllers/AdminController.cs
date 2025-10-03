using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly VentasDbContext _context;

        public AdminController(UserManager<Usuario> userManager, VentasDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: api/admin/usuarios
        [HttpGet("usuarios")]
        public async Task<IActionResult> GetUsuarios()
        {
            var usuarios = await _userManager.Users
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.Nombre,
                    u.Apellido
                    // 🔹 PhoneNumber eliminado
                })
                .ToListAsync();

            return Ok(new { data = usuarios });
        }

        // GET: api/admin/usuarios/{id}
        [HttpGet("usuarios/{id:int}")]
        public async Task<IActionResult> GetUsuarioPorId(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            var roles = await _userManager.GetRolesAsync(user);

            var usuarioDto = new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.Nombre,
                user.Apellido,
                // 🔹 PhoneNumber eliminado
                Roles = roles
            };

            return Ok(new { data = usuarioDto });
        }

        // PUT: api/admin/usuarios/{id}
        [HttpPut("usuarios/{id:int}")]
        public async Task<IActionResult> ActualizarUsuario(int id, [FromBody] AdminUpdateUserRequest request)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            // Actualizar propiedades
            if (!string.IsNullOrEmpty(request.Nombre))
                user.Nombre = request.Nombre;

            if (!string.IsNullOrEmpty(request.Apellido))
                user.Apellido = request.Apellido;

            if (!string.IsNullOrEmpty(request.Email))
                user.Email = request.Email;

            // 🔹 PhoneNumber eliminado del update

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return BadRequest(new { message = "Error al actualizar el usuario", errors });
            }

            return Ok(new { 
                message = "Usuario actualizado correctamente", 
                data = new { 
                    user.Id, 
                    user.UserName, 
                    user.Email, 
                    user.Nombre, 
                    user.Apellido
                    // 🔹 PhoneNumber eliminado
                } 
            });
        }

        // DELETE: api/admin/usuarios/{id}
        [HttpDelete("usuarios/{id:int}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Usuario eliminado correctamente" });
        }
    }

    // 🔹 PhoneNumber eliminado del request también
    public class AdminUpdateUserRequest
    {
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? Email { get; set; }
        // 🔹 PhoneNumber eliminado
    }
}