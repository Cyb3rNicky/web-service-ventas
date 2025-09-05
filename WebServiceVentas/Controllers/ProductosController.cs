using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace VentasApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "VendedorOrAdmin")] // Aplicar política general
    public class ProductosController : ControllerBase
    {
        private readonly VentasDbContext _context;

        public ProductosController(VentasDbContext context)
        {
            _context = context;
        }

        [HttpGet]  //--todos los autenticados
        [Authorize(Policy = "Authenticated")]
        public async Task<ActionResult<IEnumerable<Producto>>> GetProductos()
        {
            var productos = await _context.Productos.AsNoTracking().ToListAsync();
            return Ok(new { data = productos });
        }

        [HttpGet("{id:int}")] //--todos los autenticados
        [Authorize(Policy = "Authenticated")]
        public async Task<ActionResult<Producto>> GetProductoPorId(int id)
        {
            var producto = await _context.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null) return NotFound();
            return Ok(producto);
        }

        [HttpGet("nombre/{nombre}")]  //--todos los autenticados
        [Authorize(Policy = "Authenticated")]
        public async Task<IActionResult> GetProductoPorNombre(string nombre)
        {
            var producto = await _context.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.Nombre == nombre);
            if (producto == null) return NotFound();
            return Ok(producto);
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")] //--solo admin
        public async Task<ActionResult<Producto>> PostProducto([FromBody] Producto producto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProductoPorId), new { id = producto.Id }, producto);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "AdminOnly")] //--solo admin
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

        [HttpPut("nombre/{nombre}")]
        [Authorize(Policy = "AdminOnly")] //--solo admin
        public async Task<IActionResult> PutProductoPorNombre(string nombre, [FromBody] Producto producto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!string.Equals(nombre, producto.Nombre, StringComparison.Ordinal))
                return BadRequest("El nombre de la URL no coincide con el del producto.");

            var existente = await _context.Productos.FirstOrDefaultAsync(p => p.Nombre == nombre);
            if (existente == null) return NotFound();

            existente.Precio = producto.Precio;
            existente.Cantidad = producto.Cantidad;
            existente.Descripcion = producto.Descripcion;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "AdminOnly")] //--solo admin
        public async Task<IActionResult> DeleteProductoPorId(int id)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null) return NotFound();

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("nombre/{nombre}")]
        [Authorize(Policy = "AdminOnly")] //--solo admin
        public async Task<IActionResult> DeleteProductoPorNombre(string nombre)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Nombre == nombre);
            if (producto == null) return NotFound();

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
