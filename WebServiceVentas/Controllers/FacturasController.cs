using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers;

[ApiController]
[Route("api/facturas")]
[Authorize(Policy = "admin,gerente,vendedor")]
public class FacturasController : ControllerBase
{
    private readonly VentasDbContext _context;

    public FacturasController(VentasDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "admin,gerente,vendedor,asistente")]
    public async Task<IActionResult> GetFacturas(CancellationToken ct)
    {
        var facturas = await _context.Facturas
            .AsNoTracking()
            .Include(f => f.Cotizacion)
                .ThenInclude(c => c.Oportunidad)
                .ThenInclude(o => o.Cliente)
            .Select(f => new
            {
                f.Id,
                f.Numero,
                Cliente = new { f.Cotizacion.Oportunidad.Cliente.Id, f.Cotizacion.Oportunidad.Cliente.Nombre },
                f.Emitida,
                f.Total
            })
            .ToListAsync(ct);

        return Ok(new { data = facturas });
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "admin,gerente,vendedor,asistente")]
    public async Task<IActionResult> GetFacturaPorId(int id, CancellationToken ct)
    {
        var factura = await _context.Facturas
            .AsNoTracking()
            .Include(f => f.Cotizacion)
                .ThenInclude(c => c.Oportunidad)
                .ThenInclude(o => o.Cliente)
            .Include(f => f.Cotizacion)
                .ThenInclude(c => c.Oportunidad)
                .ThenInclude(o => o.Vehiculo)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        if (factura == null) return NotFound();

        var dto = new
        {
            factura.Id,
            factura.Numero,
            Cliente = new
            {
                factura.Cotizacion.Oportunidad.Cliente.Id,
                factura.Cotizacion.Oportunidad.Cliente.Nombre,
                factura.Cotizacion.Oportunidad.Cliente.NIT,
                factura.Cotizacion.Oportunidad.Cliente.Direccion
            },
            Vehiculo = factura.Cotizacion.Oportunidad.Vehiculo != null ? new
            {
                factura.Cotizacion.Oportunidad.Vehiculo.Marca,
                factura.Cotizacion.Oportunidad.Vehiculo.Modelo,
                factura.Cotizacion.Oportunidad.Vehiculo.Anio
            } : null,
            factura.Emitida,
            factura.Total,
            CotizacionId = factura.Cotizacion.Id
        };

        return Ok(new { data = dto });
    }

    [HttpPost]
    [Authorize(Roles = "admin,gerente")] // Solo gerente y admin crean facturas
    public async Task<IActionResult> CrearFactura([FromBody] FacturaRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var cotizacion = await _context.Cotizaciones
            .Include(c => c.Oportunidad)
            .FirstOrDefaultAsync(c => c.Id == request.CotizacionId, ct);

        if (cotizacion == null) return BadRequest("Cotización no encontrada");

        // Generar número de factura único
        var numeroFactura = $"FACT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";

        var factura = new Factura
        {
            CotizacionId = request.CotizacionId,
            Numero = numeroFactura,
            Emitida = false,
            Total = cotizacion.Total
        };

        _context.Facturas.Add(factura);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetFacturaPorId), new { id = factura.Id }, new { data = factura });
    }

    [HttpPut("{id:int}/emitir")]
    [Authorize(Roles = "admin,gerente")] // Solo gerente y admin emiten facturas
    public async Task<IActionResult> EmitirFactura(int id, CancellationToken ct)
    {
        var factura = await _context.Facturas.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (factura == null) return NotFound();

        factura.Emitida = true;
        await _context.SaveChangesAsync(ct);

        return Ok(new { data = factura });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")] // Solo admin elimina facturas
    public async Task<IActionResult> EliminarFactura(int id, CancellationToken ct)
    {
        var factura = await _context.Facturas.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (factura == null) return NotFound();

        _context.Facturas.Remove(factura);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    public class FacturaRequest
    {
        public int CotizacionId { get; set; }
    }
}