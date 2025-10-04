using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Authenticated")] // 🔹 CORREGIDO: Todos los usuarios autenticados pueden acceder
public class ProductosController : ControllerBase
{
    private readonly VentasDbContext _context;

    public ProductosController(VentasDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = "Authenticated")] // ✅ CORRECTO: Cualquier usuario autenticado puede ver
    public async Task<IActionResult> GetProductos(CancellationToken ct)
    {
        try
        {
            var productos = await _context.Productos
                .AsNoTracking()
                .Select(p => new
                {
                    p.Id,
                    p.Nombre,
                    p.Precio,
                    p.Cantidad,
                    p.Descripcion,
                    Estado = p.Cantidad > 0 ? "Disponible" : "Agotado"
                })
                .OrderBy(p => p.Nombre)
                .ToListAsync(ct);

            return Ok(new { 
                data = productos,
                total = productos.Count,
                disponibles = productos.Count(p => p.Cantidad > 0),
                agotados = productos.Count(p => p.Cantidad == 0)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Authenticated")] // ✅ CORRECTO: Cualquier usuario autenticado puede ver
    public async Task<IActionResult> GetProductoPorId(int id, CancellationToken ct)
    {
        try
        {
            var producto = await _context.Productos
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    p.Id,
                    p.Nombre,
                    p.Precio,
                    p.Cantidad,
                    p.Descripcion,
                    Estado = p.Cantidad > 0 ? "Disponible" : "Agotado"
                })
                .FirstOrDefaultAsync(ct);

            if (producto == null) 
                return NotFound(new { message = "Producto no encontrado" });
                
            return Ok(new { data = producto });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("nombre/{nombre}")]
    [Authorize(Policy = "Authenticated")] // ✅ CORRECTO: Cualquier usuario autenticado puede buscar
    public async Task<IActionResult> GetProductoPorNombre(string nombre, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrEmpty(nombre) || nombre.Length < 2)
                return BadRequest(new { message = "El nombre debe tener al menos 2 caracteres" });

            var productos = await _context.Productos
                .AsNoTracking()
                .Where(p => p.Nombre.Contains(nombre))
                .Select(p => new
                {
                    p.Id,
                    p.Nombre,
                    p.Precio,
                    p.Cantidad,
                    p.Descripcion,
                    Estado = p.Cantidad > 0 ? "Disponible" : "Agotado"
                })
                .OrderBy(p => p.Nombre)
                .ToListAsync(ct);

            return Ok(new { 
                data = productos,
                total = productos.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("disponibles")]
    [Authorize(Policy = "Authenticated")] // ✅ CORRECTO: Cualquier usuario autenticado puede ver
    public async Task<IActionResult> GetProductosDisponibles(CancellationToken ct)
    {
        try
        {
            var productos = await _context.Productos
                .AsNoTracking()
                .Where(p => p.Cantidad > 0)
                .Select(p => new
                {
                    p.Id,
                    p.Nombre,
                    p.Precio,
                    p.Cantidad,
                    p.Descripcion
                })
                .OrderBy(p => p.Nombre)
                .ToListAsync(ct);

            return Ok(new { 
                data = productos,
                total = productos.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Policy = "InventarioOrAdmin")] // 🔹 CORREGIDO: Solo inventario y admin pueden crear
    public async Task<IActionResult> PostProducto([FromBody] Producto producto, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid) 
                return BadRequest(new { message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });

            // Validar si ya existe un producto con el mismo nombre
            var productoExistente = await _context.Productos
                .FirstOrDefaultAsync(p => p.Nombre == producto.Nombre, ct);
                
            if (productoExistente != null)
                return BadRequest(new { message = "Ya existe un producto con este nombre" });

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetProductoPorId), new { id = producto.Id }, new { 
                message = "Producto creado exitosamente",
                data = new { 
                    producto.Id, 
                    producto.Nombre, 
                    producto.Precio, 
                    producto.Cantidad,
                    producto.Descripcion 
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "InventarioOrAdmin")] // 🔹 CORREGIDO: Solo inventario y admin pueden actualizar
    public async Task<IActionResult> PutProductoPorId(int id, [FromBody] Producto producto, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid) 
                return BadRequest(new { message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });
                
            if (id != producto.Id) 
                return BadRequest(new { message = "El ID no coincide" });

            var existente = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (existente == null) 
                return NotFound(new { message = "Producto no encontrado" });

            // Validar si ya existe otro producto con el mismo nombre
            var nombreExistente = await _context.Productos
                .FirstOrDefaultAsync(p => p.Nombre == producto.Nombre && p.Id != id, ct);
                
            if (nombreExistente != null)
                return BadRequest(new { message = "Ya existe otro producto con este nombre" });

            existente.Nombre = producto.Nombre;
            existente.Precio = producto.Precio;
            existente.Cantidad = producto.Cantidad;
            existente.Descripcion = producto.Descripcion;

            await _context.SaveChangesAsync(ct);
            
            return Ok(new { 
                message = "Producto actualizado exitosamente",
                data = new { 
                    existente.Id, 
                    existente.Nombre, 
                    existente.Precio, 
                    existente.Cantidad,
                    existente.Descripcion 
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPut("nombre/{nombre}")]
    [Authorize(Policy = "InventarioOrAdmin")] // 🔹 CORREGIDO: Solo inventario y admin pueden actualizar
    public async Task<IActionResult> PutProductoPorNombre(string nombre, [FromBody] Producto producto, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid) 
                return BadRequest(new { message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });
                
            if (!string.Equals(nombre, producto.Nombre, StringComparison.Ordinal))
                return BadRequest(new { message = "El nombre de la URL no coincide con el del producto" });

            var existente = await _context.Productos.FirstOrDefaultAsync(p => p.Nombre == nombre, ct);
            if (existente == null) 
                return NotFound(new { message = "Producto no encontrado" });

            existente.Precio = producto.Precio;
            existente.Cantidad = producto.Cantidad;
            existente.Descripcion = producto.Descripcion;

            await _context.SaveChangesAsync(ct);
            
            return Ok(new { 
                message = "Producto actualizado exitosamente",
                data = new { 
                    existente.Id, 
                    existente.Nombre, 
                    existente.Precio, 
                    existente.Cantidad,
                    existente.Descripcion 
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPut("{id:int}/stock")]
    [Authorize(Policy = "InventarioOrAdmin")] // 🔹 CORREGIDO: Solo inventario y admin pueden actualizar stock
    public async Task<IActionResult> ActualizarStock(int id, [FromBody] ActualizarStockRequest request, CancellationToken ct)
    {
        try
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (producto == null) 
                return NotFound(new { message = "Producto no encontrado" });

            if (request.Cantidad < 0)
                return BadRequest(new { message = "La cantidad no puede ser negativa" });

            producto.Cantidad = request.Cantidad;

            await _context.SaveChangesAsync(ct);
            
            return Ok(new { 
                message = "Stock actualizado exitosamente",
                data = new { 
                    producto.Id, 
                    producto.Nombre, 
                    producto.Cantidad,
                    Estado = producto.Cantidad > 0 ? "Disponible" : "Agotado"
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")] // 🔹 CORREGIDO: Solo admin puede eliminar
    public async Task<IActionResult> DeleteProductoPorId(int id, CancellationToken ct)
    {
        try
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (producto == null) 
                return NotFound(new { message = "Producto no encontrado" });

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync(ct);
            
            return Ok(new { message = "Producto eliminado exitosamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }
}

public class ActualizarStockRequest
{
    public int Cantidad { get; set; }
}