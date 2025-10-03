using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers;

[ApiController]
[Route("api/clientes")]
[Authorize(Policy = "VendedorOrAdmin")]
public class ClientesController : ControllerBase
{
    private readonly VentasDbContext _context;

    public ClientesController(VentasDbContext context)
    {
        _context = context;
    }

    private IQueryable<Cliente> QueryClientes => _context.Set<Cliente>().AsQueryable();

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> PostCliente([FromBody] Cliente cliente, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        _context.Set<Cliente>().Add(cliente);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetClienteById), new { id = cliente.Id }, new { data = cliente });
    }

    [HttpGet]
    [Authorize(Policy = "Authenticated")]
    public async Task<IActionResult> GetClientes(CancellationToken ct)
    {
        var clientes = await QueryClientes.AsNoTracking().ToListAsync(ct);
        return Ok(new { data = clientes });
    }

    [HttpGet("nombre/{nombre}")]
    [Authorize(Policy = "Authenticated")]
    public async Task<IActionResult> GetPorNombre(string nombre, CancellationToken ct)
    {
        var clientes = await QueryClientes
            .AsNoTracking()
            .Where(c => c.Nombre == nombre)
            .ToListAsync(ct);

        return Ok(new { data = clientes });
    }

    [HttpGet("nit/{nit}")]
    [Authorize(Policy = "Authenticated")]
    public async Task<IActionResult> GetPorNit(string nit, CancellationToken ct)
    {
        var cliente = await QueryClientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.NIT == nit, ct);

        if (cliente == null) return NotFound();
        return Ok(new { data = cliente });
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Authenticated")]
    public async Task<IActionResult> GetClienteById(int id, CancellationToken ct)
    {
        var cliente = await QueryClientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (cliente == null) return NotFound();
        return Ok(new { data = cliente });
    }
}