using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "VendedorOrAdmin")]
public class ProductosController : ControllerBase
{
    private readonly VentasDbContext _context;

    public ProductosController(VentasDbContext context)
    {
        _context = context;
    }

    private IQueryable<Producto> QueryProductos => _context.Set<Producto>().AsQueryable();

    [HttpGet]
    [Authorize(Policy = "Authenticated")]
    public async Task<IActionResult> GetProductos(CancellationToken ct)
    {
        var productos = await QueryProductos.AsNoTracking().ToListAsync(ct);
        return Ok(new { data = productos });
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Authenticated")]
    public async Task<IActionResult> GetProductoPorId(int id, CancellationToken ct)
    {
        var producto = await QueryProductos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (producto == null) return NotFound();
        return Ok(new { data = producto });
    }

    [HttpGet("nombre/{nombre}")]
    [Authorize(Policy = "Authenticated")]
    public async Task<IActionResult> GetProductoPorNombre(string nombre, CancellationToken ct)
    {
        var producto = await QueryProductos.AsNoTracking().FirstOrDefaultAsync(p => p.Nombre == nombre, ct);
        if (producto == null) return NotFound();
        return Ok(new { data = producto });
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> PostProducto([FromBody] Producto producto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        _context.Set<Producto>().Add(producto);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetProductoPorId), new { id = producto.Id }, new { data = producto });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> PutProductoPorId(int id, [FromBody] Producto producto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (id != producto.Id) return BadRequest("El Id no coincide.");

        var existente = await _context.Set<Producto>().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (existente == null) return NotFound();

        existente.Nombre = producto.Nombre;
        existente.Precio = producto.Precio;
        existente.Cantidad = producto.Cantidad;
        existente.Descripcion = producto.Descripcion;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPut("nombre/{nombre}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> PutProductoPorNombre(string nombre, [FromBody] Producto producto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (!string.Equals(nombre, producto.Nombre, StringComparison.Ordinal))
            return BadRequest("El nombre de la URL no coincide con el del producto.");

        var existente = await _context.Set<Producto>().FirstOrDefaultAsync(p => p.Nombre == nombre, ct);
        if (existente == null) return NotFound();

        existente.Precio = producto.Precio;
        existente.Cantidad = producto.Cantidad;
        existente.Descripcion = producto.Descripcion;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteProductoPorId(int id, CancellationToken ct)
    {
        var producto = await _context.Set<Producto>().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (producto == null) return NotFound();

        _context.Set<Producto>().Remove(producto);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }
}