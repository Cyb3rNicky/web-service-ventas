using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers;

// DTO for output (response)
public class CotizacionDto
{
    public int Id { get; set; }
    public int OportunidadId { get; set; }
    public bool Activa { get; set; }
    public decimal Total { get; set; }
}

// DTO for input (creation)
public class CrearCotizacionDto
{
    public int OportunidadId { get; set; }
    public bool Activa { get; set; }
    public decimal Total { get; set; }
}

[ApiController]
[Route("api/cotizaciones")]
[Authorize(Policy = "AdminGerenteVendedor")]
public class CotizacionesController : ControllerBase
{
    private readonly VentasDbContext _context;

    public CotizacionesController(VentasDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = "AdminGerenteVendedor")]
    public async Task<IActionResult> GetCotizaciones(CancellationToken ct)
    {
        try
        {
            var cotizaciones = await _context.Cotizaciones
                .AsNoTracking()
                .Select(c => new CotizacionDto
                {
                    Id = c.Id,
                    OportunidadId = c.OportunidadId,
                    Activa = c.Activa,
                    Total = c.Total
                })
                .ToListAsync(ct);

            return Ok(cotizaciones);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "AdminGerenteVendedor")]
    public async Task<IActionResult> GetCotizacionPorId(int id, CancellationToken ct)
    {
        try
        {
            var cotizacion = await _context.Cotizaciones
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new CotizacionDto
                {
                    Id = c.Id,
                    OportunidadId = c.OportunidadId,
                    Activa = c.Activa,
                    Total = c.Total
                })
                .FirstOrDefaultAsync(ct);

            if (cotizacion == null)
                return NotFound(new { message = "Cotización no encontrada" });

            return Ok(cotizacion);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Policy = "VendedorOrAdmin")]
    public async Task<IActionResult> CrearCotizacion([FromBody] CrearCotizacionDto request, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });

            var oportunidad = await _context.Oportunidades
                .FirstOrDefaultAsync(o => o.Id == request.OportunidadId, ct);

            if (oportunidad == null)
                return BadRequest(new { message = "Oportunidad no encontrada" });

            var cotizacionExistente = await _context.Cotizaciones
                .AnyAsync(c => c.OportunidadId == request.OportunidadId && c.Activa, ct);

            if (cotizacionExistente)
                return BadRequest(new { message = "Ya existe una cotización activa para esta oportunidad" });

            var cotizacion = new Cotizacion
            {
                OportunidadId = request.OportunidadId,
                Activa = request.Activa,
                Total = request.Total
            };

            _context.Cotizaciones.Add(cotizacion);
            await _context.SaveChangesAsync(ct);

            var dto = new CotizacionDto
            {
                Id = cotizacion.Id,
                OportunidadId = cotizacion.OportunidadId,
                Activa = cotizacion.Activa,
                Total = cotizacion.Total
            };

            return CreatedAtAction(nameof(GetCotizacionPorId), new { id = cotizacion.Id }, dto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPut("{id:int}/estado")]
    [Authorize(Policy = "VendedorOrAdmin")]
    public async Task<IActionResult> ActualizarEstadoCotizacion(int id, [FromBody] ActualizarEstadoCotizacionRequest request, CancellationToken ct)
    {
        try
        {
            var cotizacion = await _context.Cotizaciones.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (cotizacion == null)
                return NotFound(new { message = "Cotización no encontrada" });

            cotizacion.Activa = request.Activa;
            await _context.SaveChangesAsync(ct);

            var dto = new CotizacionDto
            {
                Id = cotizacion.Id,
                OportunidadId = cotizacion.OportunidadId,
                Activa = cotizacion.Activa,
                Total = cotizacion.Total
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOrGerente")]
    public async Task<IActionResult> EliminarCotizacion(int id, CancellationToken ct)
    {
        try
        {
            var cotizacion = await _context.Cotizaciones
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (cotizacion == null)
                return NotFound(new { message = "Cotización no encontrada" });

            _context.Cotizaciones.Remove(cotizacion);
            await _context.SaveChangesAsync(ct);

            return Ok(new { message = "Cotización eliminada exitosamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    public class ActualizarEstadoCotizacionRequest
    {
        public bool Activa { get; set; }
    }
}
