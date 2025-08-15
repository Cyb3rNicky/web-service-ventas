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

        // GET: /api/Productos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Producto>>> GetProductos()
        {
            return await _context.Productos.AsNoTracking().ToListAsync();
        }

        // GET: /api/Productos/id/5
        [HttpGet("id/{id}")]
        public async Task<ActionResult<Producto>> GetProductoPorId(int id)
        {
            var producto = await _context.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null) return NotFound();
            return producto;
        }

        // GET: /api/Productos/nombre/Computadora
        [HttpGet("nombre/{nombre}")]
        public async Task<ActionResult<Producto>> GetProductoPorNombre(string nombre)
        {
            var producto = await _context.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.Nombre == nombre);
            if (producto == null) return NotFound();
            return producto;
        }

        // POST: /api/Productos
        [HttpPost]
        public async Task<ActionResult<Producto>> PostProducto([FromBody] Producto producto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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

            return CreatedAtAction(nameof(GetProductoPorId), new { id = producto.Id }, producto);
        }

        // PUT: /api/Productos/id/5
        [HttpPut("id/{id}")]
        public async Task<IActionResult> PutProductoPorId(int id, [FromBody] Producto producto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != producto.Id)
                return BadRequest("El Id de la URL no coincide con el del producto.");

            var existente = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
            if (existente == null) return NotFound();

            existente.Nombre = producto.Nombre;
            existente.Precio = producto.Precio;
            existente.Cantidad = producto.Cantidad;
            existente.Descripcion = producto.Descripcion;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Productos.AnyAsync(p => p.Id == id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        // PUT: /api/Productos/nombre/Computadora
        [HttpPut("nombre/{nombre}")]
        public async Task<IActionResult> PutProductoPorNombre(string nombre, [FromBody] Producto producto)
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

        // DELETE: /api/Productos/id/5
        [HttpDelete("id/{id}")]
        public async Task<IActionResult> DeleteProductoPorId(int id)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null) return NotFound();

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: /api/Productos/nombre/Computadora
        [HttpDelete("nombre/{nombre}")]
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

