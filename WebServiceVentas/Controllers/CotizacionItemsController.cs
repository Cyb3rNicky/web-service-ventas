using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers;

[ApiController]
[Route("api/cotizacion-items")]
[Authorize(Policy = "VendedorOrAdmin")]
public class CotizacionItemsController : ControllerBase
{
    private readonly VentasDbContext _context;

    public CotizacionItemsController(VentasDbContext context)
    {
        _context = context;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCotizacionItemPorId(int id, CancellationToken ct)
    {
        var item = await _context.CotizacionItems
            .AsNoTracking()
            .Include(ci => ci.Vehiculo)
            .Include(ci => ci.Cotizacion)
            .FirstOrDefaultAsync(ci => ci.Id == id, ct);

        if (item == null) return NotFound();

        var dto = new
        {
            item.Id,
            item.CotizacionId,
            Vehiculo = new { item.Vehiculo.Marca, item.Vehiculo.Modelo, item.Vehiculo.Anio },
            item.Descripcion,
            item.Cantidad,
            item.PrecioUnitario,
            item.Descuento,
            item.Total
        };

        return Ok(new { data = dto });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> ActualizarCotizacionItem(int id, [FromBody] ActualizarItemRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var item = await _context.CotizacionItems
            .Include(ci => ci.Cotizacion)
            .FirstOrDefaultAsync(ci => ci.Id == id, ct);

        if (item == null) return NotFound();

        // Actualizar campos
        item.Descripcion = request.Descripcion;
        item.Cantidad = request.Cantidad;
        item.PrecioUnitario = request.PrecioUnitario;
        item.Descuento = request.Descuento;
        item.Total = (request.PrecioUnitario * request.Cantidad) - request.Descuento;

        // Recalcular total de la cotización
        var cotizacion = item.Cotizacion;
        var totalCotizacion = await _context.CotizacionItems
            .Where(ci => ci.CotizacionId == cotizacion.Id)
            .SumAsync(ci => ci.Total, ct);

        cotizacion.Total = totalCotizacion;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> EliminarCotizacionItem(int id, CancellationToken ct)
    {
        var item = await _context.CotizacionItems
            .Include(ci => ci.Cotizacion)
            .FirstOrDefaultAsync(ci => ci.Id == id, ct);

        if (item == null) return NotFound();

        var cotizacionId = item.CotizacionId;

        _context.CotizacionItems.Remove(item);

        // Recalcular total de la cotización
        var cotizacion = await _context.Cotizaciones.FindAsync(new object[] { cotizacionId }, ct);
        if (cotizacion != null)
        {
            var totalCotizacion = await _context.CotizacionItems
                .Where(ci => ci.CotizacionId == cotizacionId)
                .SumAsync(ci => ci.Total, ct);

            cotizacion.Total = totalCotizacion;
        }

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> AgregarItemACotizacion([FromBody] AgregarItemRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var cotizacion = await _context.Cotizaciones.FindAsync(new object[] { request.CotizacionId }, ct);
        if (cotizacion == null) return BadRequest("Cotización no encontrada");

        var vehiculo = await _context.Vehiculos.FindAsync(new object[] { request.VehiculoId }, ct);
        if (vehiculo == null) return BadRequest("Vehículo no encontrado");

        var itemTotal = (request.PrecioUnitario * request.Cantidad) - request.Descuento;

        var item = new CotizacionItem
        {
            CotizacionId = request.CotizacionId,
            VehiculoId = request.VehiculoId,
            Descripcion = request.Descripcion,
            Cantidad = request.Cantidad,
            PrecioUnitario = request.PrecioUnitario,
            Descuento = request.Descuento,
            Total = itemTotal
        };

        _context.CotizacionItems.Add(item);

        // Recalcular total de la cotización
        var totalCotizacion = await _context.CotizacionItems
            .Where(ci => ci.CotizacionId == request.CotizacionId)
            .SumAsync(ci => ci.Total, ct) + itemTotal;

        cotizacion.Total = totalCotizacion;

        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetCotizacionItemPorId), new { id = item.Id }, new { data = item });
    }

    public class ActualizarItemRequest
    {
        public string Descripcion { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
    }

    public class AgregarItemRequest
    {
        public int CotizacionId { get; set; }
        public int VehiculoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
    }
}