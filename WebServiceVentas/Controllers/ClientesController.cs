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

    [HttpPost]
    [Authorize(Policy = "VendedorOrAdmin")]
    public async Task<IActionResult> PostCliente([FromBody] Cliente cliente, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid) 
                return BadRequest(new { message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });

            // Validar si ya existe un cliente con el mismo NIT
            var clienteExistente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.NIT == cliente.NIT, ct);
                
            if (clienteExistente != null)
                return BadRequest(new { message = "Ya existe un cliente con este NIT" });

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetClienteById), new { id = cliente.Id }, new { 
                message = "Cliente creado exitosamente",
                data = cliente 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Policy = "Authenticated")]
    public async Task<IActionResult> GetClientes(CancellationToken ct)
    {
        try
        {
            var clientes = await _context.Clientes
                .AsNoTracking()
                .Select(c => new
                {
                    c.Id,
                    c.Nombre,
                    c.NIT,
                    c.Direccion,
                    OportunidadesCount = _context.Oportunidades.Count(o => o.ClienteId == c.Id),
                    OportunidadesActivasCount = _context.Oportunidades.Count(o => o.ClienteId == c.Id && o.Activa),
                    CotizacionesCount = _context.Cotizaciones.Count(co => co.Oportunidad.ClienteId == c.Id)
                })
                .ToListAsync(ct);

            return Ok(new { 
                data = clientes,
                total = clientes.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("nombre/{nombre}")]
    [Authorize(Policy = "Authenticated")]
    public async Task<IActionResult> GetPorNombre(string nombre, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrEmpty(nombre) || nombre.Length < 2)
                return BadRequest(new { message = "El nombre debe tener al menos 2 caracteres" });

            var clientes = await _context.Clientes
                .AsNoTracking()
                .Where(c => c.Nombre.Contains(nombre))
                .Select(c => new
                {
                    c.Id,
                    c.Nombre,
                    c.NIT,
                    c.Direccion,
                    OportunidadesCount = _context.Oportunidades.Count(o => o.ClienteId == c.Id)
                })
                .ToListAsync(ct);

            return Ok(new { 
                data = clientes,
                total = clientes.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("nit/{nit}")]
    [Authorize(Policy = "Authenticated")]
    public async Task<IActionResult> GetPorNit(string nit, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrEmpty(nit))
                return BadRequest(new { message = "El NIT es requerido" });

            var cliente = await _context.Clientes
                .AsNoTracking()
                .Where(c => c.NIT == nit)
                .Select(c => new
                {
                    c.Id,
                    c.Nombre,
                    c.NIT,
                    c.Direccion,
                    OportunidadesCount = _context.Oportunidades.Count(o => o.ClienteId == c.Id),
                    OportunidadesActivasCount = _context.Oportunidades.Count(o => o.ClienteId == c.Id && o.Activa)
                })
                .FirstOrDefaultAsync(ct);

            if (cliente == null) 
                return NotFound(new { message = "Cliente no encontrado" });
                
            return Ok(new { data = cliente });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Authenticated")]
    public async Task<IActionResult> GetClienteById(int id, CancellationToken ct)
    {
        try
        {
            var cliente = await _context.Clientes
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    c.Nombre,
                    c.NIT,
                    c.Direccion,
                    OportunidadesCount = _context.Oportunidades.Count(o => o.ClienteId == c.Id),
                    OportunidadesActivasCount = _context.Oportunidades.Count(o => o.ClienteId == c.Id && o.Activa),
                    Oportunidades = _context.Oportunidades
                        .Where(o => o.ClienteId == c.Id)
                        .Include(o => o.Usuario)
                        .Include(o => o.Vehiculo)
                        .Include(o => o.Etapa)
                        .Select(o => new { 
                            o.Id,
                            Vendedor = new { o.Usuario.Nombre, o.Usuario.Apellido },
                            Vehiculo = o.Vehiculo != null ? new { o.Vehiculo.Marca, o.Vehiculo.Modelo } : null,
                            Etapa = new { o.Etapa.Nombre },
                            o.Activa,
                            CotizacionesCount = o.Cotizaciones != null ? o.Cotizaciones.Count : 0
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(ct);

            if (cliente == null) 
                return NotFound(new { message = "Cliente no encontrado" });
                
            return Ok(new { data = cliente });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "VendedorOrAdmin")]
    public async Task<IActionResult> PutCliente(int id, [FromBody] Cliente cliente, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid) 
                return BadRequest(new { message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });
                
            if (id != cliente.Id) 
                return BadRequest(new { message = "El ID no coincide" });

            var existente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (existente == null) 
                return NotFound(new { message = "Cliente no encontrado" });

            // Verificar si el NIT ya existe en otro cliente
            var nitExistente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.NIT == cliente.NIT && c.Id != id, ct);
                
            if (nitExistente != null)
                return BadRequest(new { message = "Ya existe otro cliente con este NIT" });

            existente.Nombre = cliente.Nombre;
            existente.NIT = cliente.NIT;
            existente.Direccion = cliente.Direccion;

            await _context.SaveChangesAsync(ct);
            
            return Ok(new { 
                message = "Cliente actualizado exitosamente",
                data = new { 
                    existente.Id, 
                    existente.Nombre, 
                    existente.NIT, 
                    existente.Direccion 
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOrGerente")]
    public async Task<IActionResult> DeleteCliente(int id, CancellationToken ct)
    {
        try
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (cliente == null) 
                return NotFound(new { message = "Cliente no encontrado" });

            // Verificar oportunidades directamente desde la tabla Oportunidades
            var tieneOportunidades = await _context.Oportunidades
                .AnyAsync(o => o.ClienteId == id, ct);

            if (tieneOportunidades)
                return BadRequest(new { message = "No se puede eliminar el cliente porque tiene oportunidades asociadas" });

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync(ct);
            
            return Ok(new { message = "Cliente eliminado exitosamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("{id:int}/oportunidades")]
    [Authorize(Policy = "AdminGerenteVendedor")]
    public async Task<IActionResult> GetOportunidadesPorCliente(int id, CancellationToken ct)
    {
        try
        {
            var cliente = await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (cliente == null) 
                return NotFound(new { message = "Cliente no encontrado" });

            var oportunidades = await _context.Oportunidades
                .AsNoTracking()
                .Where(o => o.ClienteId == id)
                .Include(o => o.Usuario)
                .Include(o => o.Vehiculo)
                .Include(o => o.Etapa)
                .Include(o => o.Cotizaciones)
                .Select(o => new
                {
                    o.Id,
                    Vendedor = new { o.Usuario.Nombre, o.Usuario.Apellido },
                    Vehiculo = o.Vehiculo != null ? new { o.Vehiculo.Marca, o.Vehiculo.Modelo } : null,
                    Etapa = new { o.Etapa.Nombre },
                    o.Activa,
                    CotizacionesCount = o.Cotizaciones != null ? o.Cotizaciones.Count : 0,
                    FacturasCount = _context.Facturas.Count(f => f.Cotizacion.OportunidadId == o.Id)
                })
                .ToListAsync(ct);

            return Ok(new { 
                data = oportunidades,
                total = oportunidades.Count,
                cliente = new { cliente.Id, cliente.Nombre, cliente.NIT }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }
}
