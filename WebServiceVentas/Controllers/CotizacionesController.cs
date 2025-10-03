using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers;

[ApiController]
[Route("api/cotizaciones")]
[Authorize(Policy = "VendedorOrAdmin")]
public class CotizacionesController : ControllerBase
{
    private readonly VentasDbContext _context;

    public CotizacionesController(VentasDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetCotizaciones(CancellationToken ct)
    {
        var cotizaciones = await _context.Cotizaciones
            .AsNoTracking()
            .Include(c => c.Oportunidad)
                .ThenInclude(o => o.Cliente)
            .Include(c => c.Oportunidad)
                .ThenInclude(o => o.Vehiculo)
            .Select(c => new
            {
                c.Id,
                OportunidadId = c.Oportunidad.Id,
                Cliente = new { c.Oportunidad.Cliente.Id, c.Oportunidad.Cliente.Nombre },
                Vehiculo = c.Oportunidad.Vehiculo != null ? new { c.Oportunidad.Vehiculo.Marca, c.Oportunidad.Vehiculo.Modelo } : null,
                c.Activa,
                c.Total,
                ItemsCount = _context.CotizacionItems.Count(ci => ci.CotizacionId == c.Id),
                FacturasCount = _context.Facturas.Count(f => f.CotizacionId == c.Id)
            })
            .ToListAsync(ct);

        return Ok(new { data = cotizaciones });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCotizacionPorId(int id, CancellationToken ct)
    {
        var cotizacion = await _context.Cotizaciones
            .AsNoTracking()
            .Include(c => c.Oportunidad)
                .ThenInclude(o => o.Cliente)
            .Include(c => c.Oportunidad)
                .ThenInclude(o => o.Vehiculo)
            .Include(c => c.Items)
                .ThenInclude(i => i.Vehiculo)
            .Include(c => c.Facturas)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (cotizacion == null) return NotFound();

        // CORRECCIÓN: Manejar las facturas de forma separada
        var facturasList = new List<object>();
        if (cotizacion.Facturas != null)
        {
            foreach (var factura in cotizacion.Facturas)
            {
                facturasList.Add(new
                {
                    factura.Id,
                    factura.Numero,
                    factura.Emitida,
                    factura.Total
                });
            }
        }

        var dto = new
        {
            cotizacion.Id,
            Oportunidad = new
            {
                cotizacion.Oportunidad.Id,
                Cliente = new { cotizacion.Oportunidad.Cliente.Id, cotizacion.Oportunidad.Cliente.Nombre },
                Vehiculo = cotizacion.Oportunidad.Vehiculo != null ? new { 
                    cotizacion.Oportunidad.Vehiculo.Marca, 
                    cotizacion.Oportunidad.Vehiculo.Modelo 
                } : null
            },
            cotizacion.Activa,
            cotizacion.Total,
            Items = cotizacion.Items.Select(i => new
            {
                i.Id,
                Vehiculo = new { i.Vehiculo.Marca, i.Vehiculo.Modelo, i.Vehiculo.Anio },
                i.Descripcion,
                i.Cantidad,
                i.PrecioUnitario,
                i.Descuento,
                i.Total
            }),
            Facturas = facturasList
        };

        return Ok(new { data = dto });
    }

    [HttpPost]
    public async Task<IActionResult> CrearCotizacion([FromBody] CotizacionRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var oportunidad = await _context.Oportunidades
            .FirstOrDefaultAsync(o => o.Id == request.OportunidadId, ct);

        if (oportunidad == null) return BadRequest("Oportunidad no encontrada");

        var cotizacion = new Cotizacion
        {
            OportunidadId = request.OportunidadId,
            Activa = true,
            Items = new List<CotizacionItem>()
        };

        decimal total = 0;

        foreach (var itemReq in request.Items)
        {
            var vehiculo = await _context.Vehiculos.FindAsync(new object[] { itemReq.VehiculoId }, ct);
            if (vehiculo == null) return BadRequest($"Vehículo {itemReq.VehiculoId} no encontrado");

            var itemTotal = (itemReq.PrecioUnitario * itemReq.Cantidad) - itemReq.Descuento;

            var item = new CotizacionItem
            {
                VehiculoId = itemReq.VehiculoId,
                Descripcion = itemReq.Descripcion,
                Cantidad = itemReq.Cantidad,
                PrecioUnitario = itemReq.PrecioUnitario,
                Descuento = itemReq.Descuento,
                Total = itemTotal
            };

            cotizacion.Items.Add(item);
            total += itemTotal;
        }

        cotizacion.Total = total;
        _context.Cotizaciones.Add(cotizacion);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetCotizacionPorId), new { id = cotizacion.Id }, new { data = cotizacion });
    }

    [HttpPut("{id:int}/estado")]
    public async Task<IActionResult> ActualizarEstadoCotizacion(int id, [FromBody] ActualizarEstadoRequest request, CancellationToken ct)
    {
        var cotizacion = await _context.Cotizaciones.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cotizacion == null) return NotFound();

        cotizacion.Activa = request.Activa;
        await _context.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> EliminarCotizacion(int id, CancellationToken ct)
    {
        var cotizacion = await _context.Cotizaciones
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (cotizacion == null) return NotFound();

        var tieneFacturas = await _context.Facturas
            .AnyAsync(f => f.CotizacionId == id, ct);

        if (tieneFacturas)
            return BadRequest("No se puede eliminar la cotización porque tiene facturas asociadas");

        _context.Cotizaciones.Remove(cotizacion);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    public class CotizacionRequest
    {
        public int OportunidadId { get; set; }
        public List<CotizacionItemRequest> Items { get; set; } = new();
    }

    public class CotizacionItemRequest
    {
        public int VehiculoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
    }

    public class ActualizarEstadoRequest
    {
        public bool Activa { get; set; }
    }
}