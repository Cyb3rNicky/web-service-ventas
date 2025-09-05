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
        public IActionResult GetUsuarios()
        {
            var usuarios = _userManager.Users
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.Nombre,
                    u.Apellido
                })
                .ToList();

            return Ok(usuarios);
        }


        // DELETE: api/admin/usuarios/{id}
        [HttpDelete("usuarios/{id:int}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            // 🔹 Quitamos la validación de ventas

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Usuario eliminado correctamente" });
        }
    }
}
