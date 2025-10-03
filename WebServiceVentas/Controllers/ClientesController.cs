using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers;

[ApiController]
[Route("api/clientes")]
[Authorize(Policy = "admin,gerente,vendedor,asistente")]
public class ClientesController : ControllerBase
{
    private readonly VentasDbContext _context;

    public ClientesController(VentasDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize(Policy = "admin,gerente,vendedor")]
    public async Task<IActionResult> PostCliente([FromBody] Cliente cliente, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetClienteById), new { id = cliente.Id }, new { data = cliente });
    }

    [HttpGet]
    [Authorize(Policy = "Authenticated")]
    public async Task<IActionResult> GetClientes(CancellationToken ct)
    {
        var clientes = await _context.Clientes
            .AsNoTracking()
            .Select(c => new
            {
                c.Id,
                c.Nombre,
                c.NIT,
                c.Direccion,
                OportunidadesCount = _context.Oportunidades.Count(o => o.ClienteId == c.Id) //Contar oportunidades desde la tabla Oportunidades
            })
            .ToListAsync(ct);

        return Ok(new { data = clientes });
    }

    [HttpGet("nombre/{nombre}")]
    [Authorize(Policy = "Authenticated")]
    public async Task<IActionResult> GetPorNombre(string nombre, CancellationToken ct)
    {
        var clientes = await _context.Clientes
            .AsNoTracking()
            .Where(c => c.Nombre.Contains(nombre))
            .ToListAsync(ct);

        return Ok(new { data = clientes });
    }

    [HttpGet("nit/{nit}")]
    [Authorize(Policy = "Authenticated")]
    public async Task<IActionResult> GetPorNit(string nit, CancellationToken ct)
    {
        var cliente = await _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.NIT == nit, ct);

        if (cliente == null) return NotFound();
        return Ok(new { data = cliente });
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Authenticated")]
    public async Task<IActionResult> GetClienteById(int id, CancellationToken ct)
    {
        var cliente = await _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (cliente == null) return NotFound();
        return Ok(new { data = cliente });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "admin,gerente,vendedor")]
    public async Task<IActionResult> PutCliente(int id, [FromBody] Cliente cliente, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (id != cliente.Id) return BadRequest("El Id no coincide");

        var existente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (existente == null) return NotFound();

        existente.Nombre = cliente.Nombre;
        existente.NIT = cliente.NIT;
        existente.Direccion = cliente.Direccion;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "admin,gerente")]
    public async Task<IActionResult> DeleteCliente(int id, CancellationToken ct)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (cliente == null) return NotFound();

        // CORRECCIÓN: Verificar oportunidades directamente desde la tabla Oportunidades
        var tieneOportunidades = await _context.Oportunidades
            .AnyAsync(o => o.ClienteId == id, ct);

        if (tieneOportunidades)
            return BadRequest("No se puede eliminar el cliente porque tiene oportunidades asociadas");

        _context.Clientes.Remove(cliente);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }
}