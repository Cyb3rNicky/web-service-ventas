using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;
using System.ComponentModel.DataAnnotations;

namespace WebServiceVentas.Controllers;

[ApiController]
[Route("api/facturas")]
public class FacturasController : ControllerBase
{
    private readonly VentasDbContext _context;

    public FacturasController(VentasDbContext context)
    {
        _context = context;
    }

    // GET: api/facturas
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetFacturas()
    {
        var facturas = await _context.Facturas
            .AsNoTracking()
            .OrderByDescending(f => f.Id)
            .Select(f => new
            {
                f.Id,
                f.CotizacionId,
                f.Numero,
                f.Estado,
                f.Total
            })
            .ToListAsync();
        return Ok(facturas);
    }

    // GET: api/facturas/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> GetFactura(int id)
    {
        var factura = await _context.Facturas
            .AsNoTracking()
            .Where(f => f.Id == id)
            .Select(f => new
            {
                f.Id,
                f.CotizacionId,
                f.Numero,
                f.Estado,
                f.Total
            })
            .FirstOrDefaultAsync();

        if (factura == null)
            return NotFound(new { message = "Factura no encontrada" });

        return Ok(factura);
    }

    // POST: api/facturas
    [HttpPost]
    public async Task<ActionResult<object>> CrearFactura([FromBody] FacturaDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var factura = new Factura
        {
            CotizacionId = request.CotizacionId,
            Numero = request.Numero,
            Estado = request.Estado,
            Total = request.Total
        };

        _context.Facturas.Add(factura);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetFactura), new { id = factura.Id }, new
        {
            factura.Id,
            factura.CotizacionId,
            factura.Numero,
            factura.Estado,
            factura.Total
        });
    }

    // PUT: api/facturas/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> ActualizarFactura(int id, [FromBody] FacturaDto request)
    {
        var factura = await _context.Facturas.FindAsync(id);
        if (factura == null)
            return NotFound(new { message = "Factura no encontrada" });

        factura.CotizacionId = request.CotizacionId;
        factura.Numero = request.Numero;
        factura.Estado = request.Estado;
        factura.Total = request.Total;

        await _context.SaveChangesAsync();
        return Ok(new
        {
            factura.Id,
            factura.CotizacionId,
            factura.Numero,
            factura.Estado,
            factura.Total
        });
    }

    // DELETE: api/facturas/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> EliminarFactura(int id)
    {
        var factura = await _context.Facturas.FindAsync(id);
        if (factura == null)
            return NotFound(new { message = "Factura no encontrada" });

        _context.Facturas.Remove(factura);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Factura eliminada exitosamente" });
    }
}

// DTO for POST/PUT
public class FacturaDto
{
    [Required]
    public int CotizacionId { get; set; }

    [Required, MaxLength(50)]
    public string Numero { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Estado { get; set; } = string.Empty;

    [Required]
    public decimal Total { get; set; }
}
