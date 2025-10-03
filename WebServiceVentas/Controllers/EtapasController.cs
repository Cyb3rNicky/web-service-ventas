using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers;

[ApiController]
[Route("api/etapas")]
[Authorize(Policy = "VendedorOrAdmin")]
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
        var etapas = await _context.Etapas
            .AsNoTracking()
            .OrderBy(e => e.Orden)
            .Select(e => new
            {
                e.Id,
                e.Nombre,
                e.Orden,
                e.Anio,
                e.Precio,
                // CORREGIDO:
                OportunidadesCount = _context.Oportunidades.Count(o => o.EtapaId == e.Id)
            })
            .ToListAsync(ct);

        return Ok(new { data = etapas });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEtapaPorId(int id, CancellationToken ct)
    {
        var etapa = await _context.Etapas
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (etapa == null) return NotFound();
        return Ok(new { data = etapa });
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CrearEtapa([FromBody] Etapa etapa, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        _context.Etapas.Add(etapa);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetEtapaPorId), new { id = etapa.Id }, new { data = etapa });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ActualizarEtapa(int id, [FromBody] Etapa etapa, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (id != etapa.Id) return BadRequest("El Id no coincide");

        var existente = await _context.Etapas.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (existente == null) return NotFound();

        existente.Nombre = etapa.Nombre;
        existente.Orden = etapa.Orden;
        existente.Anio = etapa.Anio;
        existente.Precio = etapa.Precio;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> EliminarEtapa(int id, CancellationToken ct)
    {
        var etapa = await _context.Etapas
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (etapa == null) return NotFound();

        // CORREGIDO:
        var tieneOportunidades = await _context.Oportunidades
            .AnyAsync(o => o.EtapaId == id, ct);

        if (tieneOportunidades)
            return BadRequest("No se puede eliminar la etapa porque tiene oportunidades asociadas");

        _context.Etapas.Remove(etapa);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }
}