using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers;

// DTO para salida (response)
public class EtapaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Orden { get; set; }
    public int? Anio { get; set; }
    public decimal? Precio { get; set; }
}

// DTO para entrada (creación/actualización)
public class CrearEtapaDto
{
    public string Nombre { get; set; } = string.Empty;
    public int Orden { get; set; }
    public int? Anio { get; set; }
    public decimal? Precio { get; set; }
}

[ApiController]
[Route("api/etapas")]
[Authorize(Policy = "Authenticated")]
public class EtapasController : ControllerBase
{
    private readonly VentasDbContext _context;

    public EtapasController(VentasDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetEtapas(CancellationToken ct)
    {
        try
        {
            var etapas = await _context.Etapas
                .AsNoTracking()
                .OrderBy(e => e.Orden)
                .Select(e => new EtapaDto
                {
                    Id = e.Id,
                    Nombre = e.Nombre,
                    Orden = e.Orden,
                    Anio = e.Anio,
                    Precio = e.Precio
                })
                .ToListAsync(ct);

            return Ok(etapas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEtapaPorId(int id, CancellationToken ct)
    {
        try
        {
            var etapa = await _context.Etapas
                .AsNoTracking()
                .Where(e => e.Id == id)
                .Select(e => new EtapaDto
                {
                    Id = e.Id,
                    Nombre = e.Nombre,
                    Orden = e.Orden,
                    Anio = e.Anio,
                    Precio = e.Precio
                })
                .FirstOrDefaultAsync(ct);

            if (etapa == null)
                return NotFound(new { message = "Etapa no encontrada" });

            return Ok(etapa);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CrearEtapa([FromBody] CrearEtapaDto dto, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });

            var etapaExistente = await _context.Etapas
                .FirstOrDefaultAsync(e => e.Nombre == dto.Nombre, ct);

            if (etapaExistente != null)
                return BadRequest(new { message = "Ya existe una etapa con este nombre" });

            var ordenExistente = await _context.Etapas
                .FirstOrDefaultAsync(e => e.Orden == dto.Orden, ct);

            if (ordenExistente != null)
                return BadRequest(new { message = "Ya existe una etapa con este orden" });

            var etapa = new Etapa
            {
                Nombre = dto.Nombre,
                Orden = dto.Orden,
                Anio = dto.Anio,
                Precio = dto.Precio
            };

            _context.Etapas.Add(etapa);
            await _context.SaveChangesAsync(ct);

            var result = new EtapaDto
            {
                Id = etapa.Id,
                Nombre = etapa.Nombre,
                Orden = etapa.Orden,
                Anio = etapa.Anio,
                Precio = etapa.Precio
            };

            return CreatedAtAction(nameof(GetEtapaPorId), new { id = etapa.Id }, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ActualizarEtapa(int id, [FromBody] CrearEtapaDto dto, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });

            var existente = await _context.Etapas.FirstOrDefaultAsync(e => e.Id == id, ct);
            if (existente == null)
                return NotFound(new { message = "Etapa no encontrada" });

            var nombreExistente = await _context.Etapas
                .FirstOrDefaultAsync(e => e.Nombre == dto.Nombre && e.Id != id, ct);

            if (nombreExistente != null)
                return BadRequest(new { message = "Ya existe otra etapa con este nombre" });

            var ordenExistente = await _context.Etapas
                .FirstOrDefaultAsync(e => e.Orden == dto.Orden && e.Id != id, ct);

            if (ordenExistente != null)
                return BadRequest(new { message = "Ya existe otra etapa con este orden" });

            existente.Nombre = dto.Nombre;
            existente.Orden = dto.Orden;
            existente.Anio = dto.Anio;
            existente.Precio = dto.Precio;

            await _context.SaveChangesAsync(ct);

            var result = new EtapaDto
            {
                Id = existente.Id,
                Nombre = existente.Nombre,
                Orden = existente.Orden,
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
    public async Task<IActionResult> EliminarEtapa(int id, CancellationToken ct)
    {
        try
        {
            var etapa = await _context.Etapas
                .FirstOrDefaultAsync(e => e.Id == id, ct);

            if (etapa == null)
                return NotFound(new { message = "Etapa no encontrada" });

            _context.Etapas.Remove(etapa);
            await _context.SaveChangesAsync(ct);

            return Ok(new { message = "Etapa eliminada exitosamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }
}
