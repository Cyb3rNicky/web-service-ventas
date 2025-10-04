using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers;

[ApiController]
[Route("api/vehiculos")]
[Authorize(Policy = "Authenticated")] // 🔹 CORREGIDO: Todos los usuarios autenticados pueden acceder
public class VehiculosController : ControllerBase
{
    private readonly VentasDbContext _context;

    public VehiculosController(VentasDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = "Authenticated")] // ✅ CORRECTO: Cualquier usuario autenticado puede ver
    public async Task<IActionResult> GetVehiculos(CancellationToken ct)
    {
        try
        {
            var vehiculos = await _context.Vehiculos
                .AsNoTracking()
                .Select(v => new
                {
                    v.Id,
                    v.Marca,
                    v.Modelo,
                    v.Anio,
                    v.Precio,
                    OportunidadesCount = _context.Oportunidades.Count(o => o.VehiculoId == v.Id),
                    OportunidadesActivasCount = _context.Oportunidades.Count(o => o.VehiculoId == v.Id && o.Activa),
                    CotizacionItemsCount = _context.CotizacionItems.Count(ci => ci.VehiculoId == v.Id)
                })
                .OrderBy(v => v.Marca)
                .ThenBy(v => v.Modelo)
                .ToListAsync(ct);

            return Ok(new { 
                data = vehiculos,
                total = vehiculos.Count,
                marcas = vehiculos.Select(v => v.Marca).Distinct().Count()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Authenticated")] // ✅ CORRECTO: Cualquier usuario autenticado puede ver
    public async Task<IActionResult> GetVehiculoPorId(int id, CancellationToken ct)
    {
        try
        {
            var vehiculo = await _context.Vehiculos
                .AsNoTracking()
                .Where(v => v.Id == id)
                .Select(v => new
                {
                    v.Id,
                    v.Marca,
                    v.Modelo,
                    v.Anio,
                    v.Precio,
                    OportunidadesCount = _context.Oportunidades.Count(o => o.VehiculoId == v.Id),
                    OportunidadesActivasCount = _context.Oportunidades.Count(o => o.VehiculoId == v.Id && o.Activa),
                    CotizacionItemsCount = _context.CotizacionItems.Count(ci => ci.VehiculoId == v.Id),
                    Oportunidades = _context.Oportunidades
                        .Where(o => o.VehiculoId == v.Id)
                        .Include(o => o.Cliente)
                        .Include(o => o.Usuario)
                        .Select(o => new
                        {
                            o.Id,
                            Cliente = new { o.Cliente.Nombre },
                            Vendedor = new { o.Usuario.Nombre, o.Usuario.Apellido },
                            o.Activa
                        })
                        .Take(10) // Limitar a 10 oportunidades
                        .ToList()
                })
                .FirstOrDefaultAsync(ct);

            if (vehiculo == null) 
                return NotFound(new { message = "Vehículo no encontrado" });
                
            return Ok(new { data = vehiculo });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("marca/{marca}")]
    [Authorize(Policy = "Authenticated")] // ✅ CORRECTO: Cualquier usuario autenticado puede buscar
    public async Task<IActionResult> GetVehiculosPorMarca(string marca, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrEmpty(marca) || marca.Length < 2)
                return BadRequest(new { message = "La marca debe tener al menos 2 caracteres" });

            var vehiculos = await _context.Vehiculos
                .AsNoTracking()
                .Where(v => v.Marca.ToLower().Contains(marca.ToLower()))
                .Select(v => new
                {
                    v.Id,
                    v.Marca,
                    v.Modelo,
                    v.Anio,
                    v.Precio,
                    OportunidadesCount = _context.Oportunidades.Count(o => o.VehiculoId == v.Id)
                })
                .OrderBy(v => v.Modelo)
                .ThenBy(v => v.Anio)
                .ToListAsync(ct);

            return Ok(new { 
                data = vehiculos,
                total = vehiculos.Count,
                marca = marca
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("search")]
    [Authorize(Policy = "Authenticated")] // ✅ CORRECTO: Cualquier usuario autenticado puede buscar
    public async Task<IActionResult> BuscarVehiculos([FromQuery] string search, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrEmpty(search) || search.Length < 2)
                return BadRequest(new { message = "El término de búsqueda debe tener al menos 2 caracteres" });

            var vehiculos = await _context.Vehiculos
                .AsNoTracking()
                .Where(v => v.Marca.Contains(search) || v.Modelo.Contains(search))
                .Select(v => new
                {
                    v.Id,
                    v.Marca,
                    v.Modelo,
                    v.Anio,
                    v.Precio,
                    OportunidadesCount = _context.Oportunidades.Count(o => o.VehiculoId == v.Id)
                })
                .OrderBy(v => v.Marca)
                .ThenBy(v => v.Modelo)
                .ToListAsync(ct);

            return Ok(new { 
                data = vehiculos,
                total = vehiculos.Count,
                terminoBusqueda = search
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("marcas")]
    [Authorize(Policy = "Authenticated")] // ✅ CORRECTO: Cualquier usuario autenticado puede ver marcas
    public async Task<IActionResult> GetMarcas(CancellationToken ct)
    {
        try
        {
            var marcas = await _context.Vehiculos
                .AsNoTracking()
                .Select(v => v.Marca)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync(ct);

            return Ok(new { 
                data = marcas,
                total = marcas.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Policy = "InventarioOrAdmin")] // 🔹 CORREGIDO: Solo inventario y admin pueden crear
    public async Task<IActionResult> CrearVehiculo([FromBody] Vehiculo vehiculo, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid) 
                return BadRequest(new { message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });

            // Validar si ya existe un vehículo con la misma marca, modelo y año
            var vehiculoExistente = await _context.Vehiculos
                .FirstOrDefaultAsync(v => 
                    v.Marca == vehiculo.Marca && 
                    v.Modelo == vehiculo.Modelo && 
                    v.Anio == vehiculo.Anio, ct);
                
            if (vehiculoExistente != null)
                return BadRequest(new { message = "Ya existe un vehículo con esta marca, modelo y año" });

            _context.Vehiculos.Add(vehiculo);
            await _context.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetVehiculoPorId), new { id = vehiculo.Id }, new { 
                message = "Vehículo creado exitosamente",
                data = new { 
                    vehiculo.Id, 
                    vehiculo.Marca, 
                    vehiculo.Modelo, 
                    vehiculo.Anio, 
                    vehiculo.Precio 
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
    public async Task<IActionResult> ActualizarVehiculo(int id, [FromBody] Vehiculo vehiculo, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid) 
                return BadRequest(new { message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });
                
            if (id != vehiculo.Id) 
                return BadRequest(new { message = "El ID no coincide" });

            var existente = await _context.Vehiculos.FirstOrDefaultAsync(v => v.Id == id, ct);
            if (existente == null) 
                return NotFound(new { message = "Vehículo no encontrado" });

            // Validar si ya existe otro vehículo con la misma marca, modelo y año
            var vehiculoExistente = await _context.Vehiculos
                .FirstOrDefaultAsync(v => 
                    v.Marca == vehiculo.Marca && 
                    v.Modelo == vehiculo.Modelo && 
                    v.Anio == vehiculo.Anio &&
                    v.Id != id, ct);
                
            if (vehiculoExistente != null)
                return BadRequest(new { message = "Ya existe otro vehículo con esta marca, modelo y año" });

            existente.Marca = vehiculo.Marca;
            existente.Modelo = vehiculo.Modelo;
            existente.Anio = vehiculo.Anio;
            existente.Precio = vehiculo.Precio;

            await _context.SaveChangesAsync(ct);
            
            return Ok(new { 
                message = "Vehículo actualizado exitosamente",
                data = new { 
                    existente.Id, 
                    existente.Marca, 
                    existente.Modelo, 
                    existente.Anio, 
                    existente.Precio 
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
    public async Task<IActionResult> EliminarVehiculo(int id, CancellationToken ct)
    {
        try
        {
            var vehiculo = await _context.Vehiculos
                .FirstOrDefaultAsync(v => v.Id == id, ct);

            if (vehiculo == null) 
                return NotFound(new { message = "Vehículo no encontrado" });

            var tieneOportunidades = await _context.Oportunidades
                .AnyAsync(o => o.VehiculoId == id, ct);
            
            var tieneCotizacionItems = await _context.CotizacionItems
                .AnyAsync(ci => ci.VehiculoId == id, ct);

            if (tieneOportunidades || tieneCotizacionItems)
                return BadRequest(new { message = "No se puede eliminar el vehículo porque tiene oportunidades o items de cotización asociados" });

            _context.Vehiculos.Remove(vehiculo);
            await _context.SaveChangesAsync(ct);
            
            return Ok(new { message = "Vehículo eliminado exitosamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }
}