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
    [Authorize(Policy = "VendedorOrAdmin")]
    public async Task<IActionResult> GetCotizacionItemPorId(int id, CancellationToken ct)
    {
        try
        {
            var item = await _context.CotizacionItems
                .AsNoTracking()
                .Where(ci => ci.Id == id)
                .Select(ci => new
                {
                    ci.Id,
                    ci.CotizacionId,
                    ci.VehiculoId,
                    ci.Descripcion,
                    ci.Cantidad,
                    ci.PrecioUnitario,
                    ci.Descuento,
                    ci.Total
                })
                .FirstOrDefaultAsync(ct);

            if (item == null)
                return NotFound(new { message = "Item de cotización no encontrado" });

            return Ok(item);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "VendedorOrAdmin")]
    public async Task<IActionResult> ActualizarCotizacionItem(int id, [FromBody] ActualizarItemRequest request, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });

            var item = await _context.CotizacionItems
                .Include(ci => ci.Cotizacion)
                .FirstOrDefaultAsync(ci => ci.Id == id, ct);

            if (item == null)
                return NotFound(new { message = "Item de cotización no encontrado" });

            if (!item.Cotizacion.Activa)
                return BadRequest(new { message = "No se puede modificar un item de una cotización inactiva" });

            item.Descripcion = request.Descripcion;
            item.Cantidad = request.Cantidad;
            item.PrecioUnitario = request.PrecioUnitario;
            item.Descuento = request.Descuento;
            item.Total = (request.PrecioUnitario * request.Cantidad) - request.Descuento;

            var cotizacion = item.Cotizacion;
            var totalCotizacion = await _context.CotizacionItems
                .Where(ci => ci.CotizacionId == cotizacion.Id)
                .SumAsync(ci => ci.Total, ct);

            cotizacion.Total = totalCotizacion;

            await _context.SaveChangesAsync(ct);

            return Ok(new
            {
                item.Id,
                item.CotizacionId,
                item.VehiculoId,
                item.Descripcion,
                item.Cantidad,
                item.PrecioUnitario,
                item.Descuento,
                item.Total
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "VendedorOrAdmin")]
    public async Task<IActionResult> EliminarCotizacionItem(int id, CancellationToken ct)
    {
        try
        {
            var item = await _context.CotizacionItems
                .Include(ci => ci.Cotizacion)
                .FirstOrDefaultAsync(ci => ci.Id == id, ct);

            if (item == null)
                return NotFound(new { message = "Item de cotización no encontrado" });

            if (!item.Cotizacion.Activa)
                return BadRequest(new { message = "No se puede eliminar un item de una cotización inactiva" });

            var cotizacionId = item.CotizacionId;

            _context.CotizacionItems.Remove(item);

            var cotizacion = await _context.Cotizaciones.FindAsync(new object[] { cotizacionId }, ct);
            if (cotizacion != null)
            {
                var totalCotizacion = await _context.CotizacionItems
                    .Where(ci => ci.CotizacionId == cotizacionId)
                    .SumAsync(ci => ci.Total, ct);

                cotizacion.Total = totalCotizacion;
            }

            await _context.SaveChangesAsync(ct);

            return Ok(new { message = "Item de cotización eliminado exitosamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Policy = "VendedorOrAdmin")]
    public async Task<IActionResult> AgregarItemACotizacion([FromBody] AgregarItemRequest request, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });

            var cotizacion = await _context.Cotizaciones
                .FirstOrDefaultAsync(c => c.Id == request.CotizacionId, ct);

            if (cotizacion == null)
                return BadRequest(new { message = "Cotización no encontrada" });

            if (!cotizacion.Activa)
                return BadRequest(new { message = "No se pueden agregar items a una cotización inactiva" });

            var vehiculo = await _context.Vehiculos.FindAsync(new object[] { request.VehiculoId }, ct);
            if (vehiculo == null)
                return BadRequest(new { message = "Vehículo no encontrado" });

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

            var totalCotizacion = await _context.CotizacionItems
                .Where(ci => ci.CotizacionId == request.CotizacionId)
                .SumAsync(ci => ci.Total, ct) + itemTotal;

            cotizacion.Total = totalCotizacion;

            await _context.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetCotizacionItemPorId), new { id = item.Id }, new
            {
                item.Id,
                item.CotizacionId,
                item.VehiculoId,
                item.Descripcion,
                item.Cantidad,
                item.PrecioUnitario,
                item.Descuento,
                item.Total
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("cotizacion/{cotizacionId:int}")]
    [Authorize(Policy = "VendedorOrAdmin")]
    public async Task<IActionResult> GetItemsPorCotizacion(int cotizacionId, CancellationToken ct)
    {
        try
        {
            var items = await _context.CotizacionItems
                .AsNoTracking()
                .Where(ci => ci.CotizacionId == cotizacionId)
                .Select(ci => new
                {
                    ci.Id,
                    ci.CotizacionId,
                    ci.VehiculoId,
                    ci.Descripcion,
                    ci.Cantidad,
                    ci.PrecioUnitario,
                    ci.Descuento,
                    ci.Total
                })
                .ToListAsync(ct);

            return Ok(items);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }
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
