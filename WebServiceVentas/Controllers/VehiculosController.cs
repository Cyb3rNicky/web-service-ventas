using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers;

// DTO para salida (response)
public class VehiculoDto
{
    public int Id { get; set; }
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Anio { get; set; }
    public decimal Precio { get; set; }
}

// DTO para entrada (creación/actualización)
public class CrearVehiculoDto
{
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Anio { get; set; }
    public decimal Precio { get; set; }
}

[ApiController]
[Route("api/vehiculos")]
[Authorize(Policy = "Authenticated")]
public class VehiculosController : ControllerBase
{
    private readonly VentasDbContext _context;

    public VehiculosController(VentasDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = "Authenticated")]
    public async Task<IActionResult> GetVehiculos(CancellationToken ct)
    {
        try
        {
            var vehiculos = await _context.Vehiculos
                .AsNoTracking()
                .Select(v => new VehiculoDto
                {
                    Id = v.Id,
                    Marca = v.Marca,
                    Modelo = v.Modelo,
                    Anio = v.Anio,
                    Precio = v.Precio
                })
                .OrderBy(v => v.Marca)
                .ThenBy(v => v.Modelo)
                .ToListAsync(ct);

            return Ok(vehiculos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Authenticated")]
    public async Task<IActionResult> GetVehiculoPorId(int id, CancellationToken ct)
    {
        try
        {
            var vehiculo = await _context.Vehiculos
                .AsNoTracking()
                .Where(v => v.Id == id)
                .Select(v => new VehiculoDto
                {
                    Id = v.Id,
                    Marca = v.Marca,
                    Modelo = v.Modelo,
                    Anio = v.Anio,
                    Precio = v.Precio
                })
                .FirstOrDefaultAsync(ct);

            if (vehiculo == null)
                return NotFound(new { message = "Vehículo no encontrado" });

            return Ok(vehiculo);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("marca/{marca}")]
    [Authorize(Policy = "Authenticated")]
    public async Task<IActionResult> GetVehiculosPorMarca(string marca, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrEmpty(marca) || marca.Length < 2)
                return BadRequest(new { message = "La marca debe tener al menos 2 caracteres" });

            var vehiculos = await _context.Vehiculos
                .AsNoTracking()
                .Where(v => v.Marca.ToLower().Contains(marca.ToLower()))
                .Select(v => new VehiculoDto
                {
                    Id = v.Id,
                    Marca = v.Marca,
                    Modelo = v.Modelo,
                    Anio = v.Anio,
                    Precio = v.Precio
                })
                .OrderBy(v => v.Modelo)
                .ThenBy(v => v.Anio)
                .ToListAsync(ct);

            return Ok(vehiculos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("search")]
    [Authorize(Policy = "Authenticated")]
    public async Task<IActionResult> BuscarVehiculos([FromQuery] string search, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrEmpty(search) || search.Length < 2)
                return BadRequest(new { message = "El término de búsqueda debe tener al menos 2 caracteres" });

            var vehiculos = await _context.Vehiculos
                .AsNoTracking()
                .Where(v => v.Marca.Contains(search) || v.Modelo.Contains(search))
                .Select(v => new VehiculoDto
                {
                    Id = v.Id,
                    Marca = v.Marca,
                    Modelo = v.Modelo,
                    Anio = v.Anio,
                    Precio = v.Precio
                })
                .OrderBy(v => v.Marca)
                .ThenBy(v => v.Modelo)
                .ToListAsync(ct);

            return Ok(vehiculos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("marcas")]
    [Authorize(Policy = "Authenticated")]
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

            return Ok(marcas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Policy = "InventarioOrAdmin")]
    public async Task<IActionResult> CrearVehiculo([FromBody] CrearVehiculoDto dto, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });

            var vehiculoExistente = await _context.Vehiculos
                .FirstOrDefaultAsync(v =>
                    v.Marca == dto.Marca &&
                    v.Modelo == dto.Modelo &&
                    v.Anio == dto.Anio, ct);

            if (vehiculoExistente != null)
                return BadRequest(new { message = "Ya existe un vehículo con esta marca, modelo y año" });

            var vehiculo = new Vehiculo
            {
                Marca = dto.Marca,
                Modelo = dto.Modelo,
                Anio = dto.Anio,
                Precio = dto.Precio
            };

            _context.Vehiculos.Add(vehiculo);
            await _context.SaveChangesAsync(ct);

            var result = new VehiculoDto
            {
                Id = vehiculo.Id,
                Marca = vehiculo.Marca,
                Modelo = vehiculo.Modelo,
                Anio = vehiculo.Anio,
                Precio = vehiculo.Precio
            };

            return CreatedAtAction(nameof(GetVehiculoPorId), new { id = vehiculo.Id }, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "InventarioOrAdmin")]
    public async Task<IActionResult> ActualizarVehiculo(int id, [FromBody] CrearVehiculoDto dto, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });

            var existente = await _context.Vehiculos.FirstOrDefaultAsync(v => v.Id == id, ct);
            if (existente == null)
                return NotFound(new { message = "Vehículo no encontrado" });

            var vehiculoExistente = await _context.Vehiculos
                .FirstOrDefaultAsync(v =>
                    v.Marca == dto.Marca &&
                    v.Modelo == dto.Modelo &&
                    v.Anio == dto.Anio &&
                    v.Id != id, ct);

            if (vehiculoExistente != null)
                return BadRequest(new { message = "Ya existe otro vehículo con esta marca, modelo y año" });

            existente.Marca = dto.Marca;
            existente.Modelo = dto.Modelo;
            existente.Anio = dto.Anio;
            existente.Precio = dto.Precio;

            await _context.SaveChangesAsync(ct);

            var result = new VehiculoDto
            {
                Id = existente.Id,
                Marca = existente.Marca,
                Modelo = existente.Modelo,
                Anio = existente.Anio,
                Precio = existente.Precio
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
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
