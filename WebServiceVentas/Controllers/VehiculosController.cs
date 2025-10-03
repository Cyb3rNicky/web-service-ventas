using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers;

[ApiController]
[Route("api/vehiculos")]
[Authorize(Policy = "admin,gerente,vendedor,inventario,asistente")]
public class VehiculosController : ControllerBase
{
    private readonly VentasDbContext _context;

    public VehiculosController(VentasDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous] // O [Authorize] para que cualquiera autenticado vea
    public async Task<IActionResult> GetVehiculos(CancellationToken ct)
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
                // CORREGIDO:
                OportunidadesCount = _context.Oportunidades.Count(o => o.VehiculoId == v.Id),
                CotizacionItemsCount = _context.CotizacionItems.Count(ci => ci.VehiculoId == v.Id)
            })
            .ToListAsync(ct);

        return Ok(new { data = vehiculos });
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous] // O [Authorize] para que cualquiera autenticado vea
    public async Task<IActionResult> GetVehiculoPorId(int id, CancellationToken ct)
    {
        var vehiculo = await _context.Vehiculos
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, ct);

        if (vehiculo == null) return NotFound();
        return Ok(new { data = vehiculo });
    }

    [HttpGet("marca/{marca}")]
    [AllowAnonymous] // O [Authorize] para que cualquiera autenticado vea
    public async Task<IActionResult> GetVehiculosPorMarca(string marca, CancellationToken ct)
    {
        var vehiculos = await _context.Vehiculos
            .AsNoTracking()
            .Where(v => v.Marca.ToLower() == marca.ToLower())
            .ToListAsync(ct);

        return Ok(new { data = vehiculos });
    }

    [HttpGet("search")]
    [AllowAnonymous] // O [Authorize] para que cualquiera autenticado vea
    public async Task<IActionResult> BuscarVehiculos([FromQuery] string search, CancellationToken ct)
    {
        var vehiculos = await _context.Vehiculos
            .AsNoTracking()
            .Where(v => v.Marca.Contains(search) || v.Modelo.Contains(search))
            .ToListAsync(ct);

        return Ok(new { data = vehiculos });
    }

    [HttpPost]
    [Authorize(Policy = "admin,inventario")]
    public async Task<IActionResult> CrearVehiculo([FromBody] Vehiculo vehiculo, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        _context.Vehiculos.Add(vehiculo);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetVehiculoPorId), new { id = vehiculo.Id }, new { data = vehiculo });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "admin,inventario")]
    public async Task<IActionResult> ActualizarVehiculo(int id, [FromBody] Vehiculo vehiculo, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (id != vehiculo.Id) return BadRequest("El Id no coincide");

        var existente = await _context.Vehiculos.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (existente == null) return NotFound();

        existente.Marca = vehiculo.Marca;
        existente.Modelo = vehiculo.Modelo;
        existente.Anio = vehiculo.Anio;
        existente.Precio = vehiculo.Precio;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> EliminarVehiculo(int id, CancellationToken ct)
    {
        var vehiculo = await _context.Vehiculos
            .FirstOrDefaultAsync(v => v.Id == id, ct);

        if (vehiculo == null) return NotFound();

        // CORREGIDO:
        var tieneOportunidades = await _context.Oportunidades
            .AnyAsync(o => o.VehiculoId == id, ct);
        
        var tieneCotizacionItems = await _context.CotizacionItems
            .AnyAsync(ci => ci.VehiculoId == id, ct);

        if (tieneOportunidades || tieneCotizacionItems)
            return BadRequest("No se puede eliminar el vehículo porque tiene oportunidades o items de cotización asociados");

        _context.Vehiculos.Remove(vehiculo);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }
}