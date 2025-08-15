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
        public async Task<ActionResult<IEnumerable<Producto>>> GetProductos()
        {
            return await _context.Productos.AsNoTracking().ToListAsync();
        }

        [HttpGet("{nombre}")]
        public async Task<ActionResult<Producto>> GetProducto(string nombre)
        {
            var producto = await _context.Productos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Nombre == nombre);

            if (producto == null) return NotFound();
            return producto;
        }

        [HttpPost]
        public async Task<ActionResult<Producto>> PostProducto([FromBody] Producto producto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // No enviar Id en el POST; la BD lo genera
            _context.Productos.Add(producto);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (await _context.Productos.AnyAsync(p => p.Nombre == producto.Nombre))
                    return Conflict($"Ya existe un producto con el nombre '{producto.Nombre}'.");
                throw;
            }

            return CreatedAtAction(nameof(GetProducto), new { nombre = producto.Nombre }, producto);
        }

        [HttpPut("{nombre}")]
        public async Task<IActionResult> PutProducto(string nombre, [FromBody] Producto producto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!string.Equals(nombre, producto.Nombre, StringComparison.Ordinal))
                return BadRequest("El nombre de la URL no coincide con el del producto.");

            var existente = await _context.Productos.FirstOrDefaultAsync(p => p.Nombre == nombre);
            if (existente == null) return NotFound();

            existente.Precio = producto.Precio;
            existente.Cantidad = producto.Cantidad;
            existente.Descripcion = producto.Descripcion;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Productos.AnyAsync(p => p.Nombre == nombre))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{nombre}")]
        public async Task<IActionResult> DeleteProducto(string nombre)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Nombre == nombre);
            if (producto == null) return NotFound();

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
