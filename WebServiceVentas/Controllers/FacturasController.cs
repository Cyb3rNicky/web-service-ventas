using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers;

[ApiController]
[Route("api/facturas")]
[Authorize(Policy = "AdminGerenteVendedor")]
public class FacturasController : ControllerBase
{
    private readonly VentasDbContext _context;

    public FacturasController(VentasDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = "AdminInventarioAsistente")]
    public async Task<IActionResult> GetFacturas(CancellationToken ct)
    {
        try
        {
            var facturas = await _context.Facturas
                .AsNoTracking()
                .Include(f => f.Cotizacion)
                    .ThenInclude(c => c.Oportunidad)
                    .ThenInclude(o => o.Cliente)
                .Include(f => f.Cotizacion)
                    .ThenInclude(c => c.Oportunidad)
                    .ThenInclude(o => o.Usuario)
                .Select(f => new
                {
                    f.Id,
                    f.Numero,
                    Cliente = new { 
                        f.Cotizacion.Oportunidad.Cliente.Id, 
                        f.Cotizacion.Oportunidad.Cliente.Nombre,
                        f.Cotizacion.Oportunidad.Cliente.NIT
                    },
                    Vendedor = new {
                        f.Cotizacion.Oportunidad.Usuario.Id,
                        f.Cotizacion.Oportunidad.Usuario.Nombre,
                        f.Cotizacion.Oportunidad.Usuario.Apellido
                    },
                    f.Emitida,
                    f.Total
                })
                .OrderByDescending(f => f.Id) // Ordenar por ID en lugar de FechaCreacion
                .ToListAsync(ct);

            return Ok(new { 
                data = facturas,
                total = facturas.Count,
                totalEmitidas = facturas.Count(f => f.Emitida),
                totalPendientes = facturas.Count(f => !f.Emitida)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "AdminInventarioAsistente")]
    public async Task<IActionResult> GetFacturaPorId(int id, CancellationToken ct)
    {
        try
        {
            var factura = await _context.Facturas
                .AsNoTracking()
                .Include(f => f.Cotizacion)
                    .ThenInclude(c => c.Oportunidad)
                    .ThenInclude(o => o.Cliente)
                .Include(f => f.Cotizacion)
                    .ThenInclude(c => c.Oportunidad)
                    .ThenInclude(o => o.Vehiculo)
                .Include(f => f.Cotizacion)
                    .ThenInclude(c => c.Oportunidad)
                    .ThenInclude(o => o.Usuario)
                .Include(f => f.Cotizacion)
                    .ThenInclude(c => c.Items)
                    .ThenInclude(i => i.Vehiculo)
                .FirstOrDefaultAsync(f => f.Id == id, ct);

            if (factura == null) 
                return NotFound(new { message = "Factura no encontrada" });

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
                Vendedor = new
                {
                    factura.Cotizacion.Oportunidad.Usuario.Id,
                    factura.Cotizacion.Oportunidad.Usuario.Nombre,
                    factura.Cotizacion.Oportunidad.Usuario.Apellido,
                    factura.Cotizacion.Oportunidad.Usuario.Email
                },
                Vehiculo = factura.Cotizacion.Oportunidad.Vehiculo != null ? new
                {
                    factura.Cotizacion.Oportunidad.Vehiculo.Id,
                    factura.Cotizacion.Oportunidad.Vehiculo.Marca,
                    factura.Cotizacion.Oportunidad.Vehiculo.Modelo,
                    factura.Cotizacion.Oportunidad.Vehiculo.Anio,
                    factura.Cotizacion.Oportunidad.Vehiculo.Precio
                } : null,
                factura.Emitida,
                factura.Total,
                Cotizacion = new
                {
                    factura.Cotizacion.Id,
                    factura.Cotizacion.Activa,
                    Items = factura.Cotizacion.Items.Select(i => new
                    {
                        i.Id,
                        Vehiculo = new { i.Vehiculo.Marca, i.Vehiculo.Modelo },
                        i.Descripcion,
                        i.Cantidad,
                        i.PrecioUnitario,
                        i.Descuento,
                        i.Total
                    })
                }
            };

            return Ok(new { data = dto });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Policy = "AdminOrGerente")]
    public async Task<IActionResult> CrearFactura([FromBody] FacturaRequest request, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid) 
                return BadRequest(new { message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });

            var cotizacion = await _context.Cotizaciones
                .Include(c => c.Oportunidad)
                .ThenInclude(o => o.Cliente)
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == request.CotizacionId, ct);

            if (cotizacion == null) 
                return BadRequest(new { message = "Cotización no encontrada" });

            // Verificar que la cotización esté activa
            if (!cotizacion.Activa)
                return BadRequest(new { message = "No se puede crear factura para una cotización inactiva" });

            // Verificar que no exista ya una factura para esta cotización
            var facturaExistente = await _context.Facturas
                .AnyAsync(f => f.CotizacionId == request.CotizacionId, ct);

            if (facturaExistente)
                return BadRequest(new { message = "Ya existe una factura para esta cotización" });

            // Generar número de factura único
            var numeroFactura = $"FACT-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";

            var factura = new Factura
            {
                CotizacionId = request.CotizacionId,
                Numero = numeroFactura,
                Emitida = false,
                Total = cotizacion.Total
            };

            _context.Facturas.Add(factura);
            await _context.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetFacturaPorId), new { id = factura.Id }, new { 
                message = "Factura creada exitosamente",
                data = new { 
                    factura.Id, 
                    factura.Numero, 
                    factura.Total, 
                    factura.Emitida,
                    Cliente = new { 
                        cotizacion.Oportunidad.Cliente.Nombre,
                        cotizacion.Oportunidad.Cliente.NIT
                    }
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPut("{id:int}/emitir")]
    [Authorize(Policy = "AdminOrGerente")]
    public async Task<IActionResult> EmitirFactura(int id, CancellationToken ct)
    {
        try
        {
            var factura = await _context.Facturas
                .Include(f => f.Cotizacion)
                .ThenInclude(c => c.Oportunidad)
                .ThenInclude(o => o.Cliente)
                .FirstOrDefaultAsync(f => f.Id == id, ct);
                
            if (factura == null) 
                return NotFound(new { message = "Factura no encontrada" });

            if (factura.Emitida)
                return BadRequest(new { message = "La factura ya ha sido emitida" });

            factura.Emitida = true;
            await _context.SaveChangesAsync(ct);

            return Ok(new { 
                message = "Factura emitida exitosamente",
                data = new { 
                    factura.Id, 
                    factura.Numero, 
                    factura.Total, 
                    factura.Emitida,
                    Cliente = new { 
                        factura.Cotizacion.Oportunidad.Cliente.Nombre,
                        factura.Cotizacion.Oportunidad.Cliente.NIT
                    }
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> EliminarFactura(int id, CancellationToken ct)
    {
        try
        {
            var factura = await _context.Facturas.FirstOrDefaultAsync(f => f.Id == id, ct);
            if (factura == null) 
                return NotFound(new { message = "Factura no encontrada" });

            // Verificar si la factura ya fue emitida
            if (factura.Emitida)
                return BadRequest(new { message = "No se puede eliminar una factura ya emitida" });

            _context.Facturas.Remove(factura);
            await _context.SaveChangesAsync(ct);
            
            return Ok(new { message = "Factura eliminada exitosamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("cliente/{clienteId:int}")]
    [Authorize(Policy = "AdminInventarioAsistente")]
    public async Task<IActionResult> GetFacturasPorCliente(int clienteId, CancellationToken ct)
    {
        try
        {
            var facturas = await _context.Facturas
                .AsNoTracking()
                .Where(f => f.Cotizacion.Oportunidad.ClienteId == clienteId)
                .Include(f => f.Cotizacion)
                    .ThenInclude(c => c.Oportunidad)
                    .ThenInclude(o => o.Usuario)
                .Select(f => new
                {
                    f.Id,
                    f.Numero,
                    Vendedor = new {
                        f.Cotizacion.Oportunidad.Usuario.Nombre,
                        f.Cotizacion.Oportunidad.Usuario.Apellido
                    },
                    f.Emitida,
                    f.Total
                })
                .OrderByDescending(f => f.Id)
                .ToListAsync(ct);

            return Ok(new { 
                data = facturas,
                total = facturas.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("pendientes")]
    [Authorize(Policy = "AdminOrGerente")]
    public async Task<IActionResult> GetFacturasPendientes(CancellationToken ct)
    {
        try
        {
            var facturas = await _context.Facturas
                .AsNoTracking()
                .Where(f => !f.Emitida)
                .Include(f => f.Cotizacion)
                    .ThenInclude(c => c.Oportunidad)
                    .ThenInclude(o => o.Cliente)
                .Select(f => new
                {
                    f.Id,
                    f.Numero,
                    Cliente = new { 
                        f.Cotizacion.Oportunidad.Cliente.Nombre,
                        f.Cotizacion.Oportunidad.Cliente.NIT
                    },
                    f.Total
                })
                .OrderBy(f => f.Id)
                .ToListAsync(ct);

            return Ok(new { 
                data = facturas,
                total = facturas.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("cotizacion/{cotizacionId:int}")]
    [Authorize(Policy = "AdminInventarioAsistente")]
    public async Task<IActionResult> GetFacturaPorCotizacion(int cotizacionId, CancellationToken ct)
    {
        try
        {
            var factura = await _context.Facturas
                .AsNoTracking()
                .Where(f => f.CotizacionId == cotizacionId)
                .Include(f => f.Cotizacion)
                    .ThenInclude(c => c.Oportunidad)
                    .ThenInclude(o => o.Cliente)
                .Select(f => new
                {
                    f.Id,
                    f.Numero,
                    f.Emitida,
                    f.Total,
                    Cliente = new { 
                        f.Cotizacion.Oportunidad.Cliente.Nombre,
                        f.Cotizacion.Oportunidad.Cliente.NIT
                    }
                })
                .FirstOrDefaultAsync(ct);

            if (factura == null)
                return NotFound(new { message = "No se encontró factura para esta cotización" });

            return Ok(new { data = factura });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }
}

public class FacturaRequest
{
    public int CotizacionId { get; set; }
}