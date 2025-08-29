using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace VentasApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private readonly VentasDbContext _context;

        public ProductosController(VentasDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,User")]
        public async Task<ActionResult<IEnumerable<Producto>>> GetProductos()
        {
            return await _context.Productos.AsNoTracking().ToListAsync();
        }

        [HttpGet("id/{id}")]
        [Authorize(Roles = "Admin,User")]
        public async Task<ActionResult<Producto>> GetProductoPorId(int id)
        {
            var producto = await _context.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null) return NotFound();
            return producto;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Producto>> PostProducto([FromBody] Producto producto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProductoPorId), new { id = producto.Id }, producto);
        }

        [HttpPut("id/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutProductoPorId(int id, [FromBody] Producto producto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id != producto.Id) return BadRequest("El Id no coincide.");

            var existente = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
            if (existente == null) return NotFound();

            existente.Nombre = producto.Nombre;
            existente.Precio = producto.Precio;
            existente.Cantidad = producto.Cantidad;
            existente.Descripcion = producto.Descripcion;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("id/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProductoPorId(int id)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null) return NotFound();

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
