using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers;

[ApiController]
[Route("api/cotizaciones")]
[Authorize(Policy = "AdminGerenteVendedor")] // 🔹 CORREGIDO: Usa la política específica
public class CotizacionesController : ControllerBase
{
    private readonly VentasDbContext _context;

    public CotizacionesController(VentasDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = "AdminGerenteVendedor")] // 🔹 CORREGIDO
    public async Task<IActionResult> GetCotizaciones(CancellationToken ct)
    {
        try
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

            return Ok(new { 
                data = cotizaciones,
                total = cotizaciones.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "AdminGerenteVendedor")] // 🔹 CORREGIDO
    public async Task<IActionResult> GetCotizacionPorId(int id, CancellationToken ct)
    {
        try
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

            if (cotizacion == null) 
                return NotFound(new { message = "Cotización no encontrada" });

            // Manejar las facturas de forma separada
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
                    Cliente = new { 
                        cotizacion.Oportunidad.Cliente.Id, 
                        cotizacion.Oportunidad.Cliente.Nombre,
                        cotizacion.Oportunidad.Cliente.NIT
                    },
                    Vehiculo = cotizacion.Oportunidad.Vehiculo != null ? new { 
                        cotizacion.Oportunidad.Vehiculo.Marca, 
                        cotizacion.Oportunidad.Vehiculo.Modelo,
                        cotizacion.Oportunidad.Vehiculo.Anio
                    } : null
                },
                cotizacion.Activa,
                cotizacion.Total,
                Items = cotizacion.Items.Select(i => new
                {
                    i.Id,
                    Vehiculo = new { 
                        i.Vehiculo.Id,
                        i.Vehiculo.Marca, 
                        i.Vehiculo.Modelo, 
                        i.Vehiculo.Anio,
                        i.Vehiculo.Precio
                    },
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
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

[HttpPost]
[Authorize(Policy = "VendedorOrAdmin")]
public async Task<IActionResult> CrearCotizacion([FromBody] CotizacionRequest request, CancellationToken ct)
{
    try
    {
        if (!ModelState.IsValid) 
            return BadRequest(new { message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });

        var oportunidad = await _context.Oportunidades
            .Include(o => o.Cliente)
            .FirstOrDefaultAsync(o => o.Id == request.OportunidadId, ct);

        if (oportunidad == null) 
            return BadRequest(new { message = "Oportunidad no encontrada" });

        // 🔹 CORRECCIÓN: Verificar si ya existe una cotización activa para esta oportunidad
        var cotizacionExistente = await _context.Cotizaciones
            .AnyAsync(c => c.OportunidadId == request.OportunidadId && c.Activa, ct);

        if (cotizacionExistente)
            return BadRequest(new { message = "Ya existe una cotización activa para esta oportunidad" });

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
            if (vehiculo == null) 
                return BadRequest(new { message = $"Vehículo {itemReq.VehiculoId} no encontrado" });

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

        return CreatedAtAction(nameof(GetCotizacionPorId), new { id = cotizacion.Id }, new { 
            message = "Cotización creada exitosamente",
            data = new { cotizacion.Id, cotizacion.Total, cotizacion.Activa }
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
    }
}

    [HttpPut("{id:int}/estado")]
    [Authorize(Policy = "VendedorOrAdmin")] // 🔹 CORREGIDO: Solo vendedores y admins pueden cambiar estado
    public async Task<IActionResult> ActualizarEstadoCotizacion(int id, [FromBody] ActualizarEstadoCotizacionRequest request, CancellationToken ct)
    {
        try
        {
            var cotizacion = await _context.Cotizaciones.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (cotizacion == null) 
                return NotFound(new { message = "Cotización no encontrada" });

            cotizacion.Activa = request.Activa;
            await _context.SaveChangesAsync(ct);

            return Ok(new { 
                message = $"Cotización {(request.Activa ? "activada" : "desactivada")} exitosamente",
                data = new { cotizacion.Id, cotizacion.Activa }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOrGerente")] // 🔹 CORREGIDO: Solo admins y gerentes pueden eliminar
    public async Task<IActionResult> EliminarCotizacion(int id, CancellationToken ct)
    {
        try
        {
            var cotizacion = await _context.Cotizaciones
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (cotizacion == null) 
                return NotFound(new { message = "Cotización no encontrada" });

            var tieneFacturas = await _context.Facturas
                .AnyAsync(f => f.CotizacionId == id, ct);

            if (tieneFacturas)
                return BadRequest(new { message = "No se puede eliminar la cotización porque tiene facturas asociadas" });

            _context.Cotizaciones.Remove(cotizacion);
            await _context.SaveChangesAsync(ct);
            
            return Ok(new { message = "Cotización eliminada exitosamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
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

    public class ActualizarEstadoCotizacionRequest
    {
        public bool Activa { get; set; }
    }
}