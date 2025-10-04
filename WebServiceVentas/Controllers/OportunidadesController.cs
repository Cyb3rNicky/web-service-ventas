using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers;

[ApiController]
[Route("api/oportunidades")]
[Authorize(Policy = "AdminGerenteVendedor")]
public class OportunidadesController : ControllerBase
{
    private readonly VentasDbContext _context;

    public OportunidadesController(VentasDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = "AdminGerenteVendedor")]
    public async Task<IActionResult> GetOportunidades(CancellationToken ct)
    {
        try
        {
            var oportunidades = await _context.Oportunidades
                .AsNoTracking()
                .Include(o => o.Cliente)
                .Include(o => o.Usuario)
                .Include(o => o.Vehiculo)
                .Include(o => o.Etapa)
                .Select(o => new
                {
                    o.Id,
                    Cliente = new { o.Cliente.Id, o.Cliente.Nombre },
                    Vendedor = new { o.Usuario.Id, o.Usuario.Nombre, o.Usuario.Apellido },
                    Vehiculo = o.Vehiculo != null ? new { o.Vehiculo.Marca, o.Vehiculo.Modelo } : null,
                    Etapa = new { o.Etapa.Id, o.Etapa.Nombre },
                    o.Activa,
                    CotizacionesCount = o.Cotizaciones != null ? o.Cotizaciones.Count : 0,
                    FacturasCount = _context.Facturas.Count(f => f.Cotizacion.OportunidadId == o.Id)
                })
                .ToListAsync(ct);

            return Ok(new { 
                data = oportunidades,
                total = oportunidades.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "AdminGerenteVendedor")]
    public async Task<IActionResult> GetOportunidadPorId(int id, CancellationToken ct)
    {
        try
        {
            var oportunidad = await _context.Oportunidades
                .AsNoTracking()
                .Include(o => o.Cliente)
                .Include(o => o.Usuario)
                .Include(o => o.Vehiculo)
                .Include(o => o.Etapa)
                .Include(o => o.Cotizaciones)
                    .ThenInclude(c => c.Items)
                .Include(o => o.Cotizaciones)
                    .ThenInclude(c => c.Facturas)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (oportunidad == null) 
                return NotFound(new { message = "Oportunidad no encontrada" });

            var dto = new
            {
                oportunidad.Id,
                Cliente = new { 
                    oportunidad.Cliente.Id, 
                    oportunidad.Cliente.Nombre,
                    oportunidad.Cliente.NIT,
                    oportunidad.Cliente.Direccion
                },
                Vendedor = new { 
                    oportunidad.Usuario.Id,
                    oportunidad.Usuario.Nombre, 
                    oportunidad.Usuario.Apellido,
                    oportunidad.Usuario.Email
                },
                Vehiculo = oportunidad.Vehiculo != null ? new { 
                    oportunidad.Vehiculo.Id,
                    oportunidad.Vehiculo.Marca, 
                    oportunidad.Vehiculo.Modelo,
                    oportunidad.Vehiculo.Anio,
                    oportunidad.Vehiculo.Precio
                } : null,
                Etapa = new {
                    oportunidad.Etapa.Id,
                    oportunidad.Etapa.Nombre
                },
                oportunidad.Activa,
                Cotizaciones = oportunidad.Cotizaciones != null ? oportunidad.Cotizaciones.Select(c => new
                {
                    c.Id,
                    c.Activa,
                    c.Total,
                    ItemsCount = c.Items != null ? c.Items.Count : 0,
                    FacturasCount = c.Facturas != null ? c.Facturas.Count : 0
                }) : Enumerable.Empty<object>()
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
    public async Task<IActionResult> CrearOportunidad([FromBody] OportunidadRequest request, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid) 
                return BadRequest(new { message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });

            // Verificar que el cliente existe
            var cliente = await _context.Clientes.FindAsync(new object[] { request.ClienteId }, ct);
            if (cliente == null)
                return BadRequest(new { message = "Cliente no encontrado" });

            // Verificar que el usuario (vendedor) existe
            var usuario = await _context.Users.FindAsync(new object[] { request.UsuarioId }, ct);
            if (usuario == null)
                return BadRequest(new { message = "Usuario no encontrado" });

            // Verificar que la etapa existe
            var etapa = await _context.Etapas.FindAsync(new object[] { request.EtapaId }, ct);
            if (etapa == null)
                return BadRequest(new { message = "Etapa no encontrada" });

            // Verificar vehículo si se proporciona
            if (request.VehiculoId.HasValue)
            {
                var vehiculo = await _context.Vehiculos.FindAsync(new object[] { request.VehiculoId.Value }, ct);
                if (vehiculo == null)
                    return BadRequest(new { message = "Vehículo no encontrado" });
            }

            var oportunidad = new Oportunidad
            {
                ClienteId = request.ClienteId,
                UsuarioId = request.UsuarioId,
                VehiculoId = request.VehiculoId,
                EtapaId = request.EtapaId,
                Activa = true
            };

            _context.Oportunidades.Add(oportunidad);
            await _context.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetOportunidadPorId), new { id = oportunidad.Id }, new { 
                message = "Oportunidad creada exitosamente",
                data = new { oportunidad.Id, Cliente = cliente.Nombre, Vendedor = $"{usuario.Nombre} {usuario.Apellido}" }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "VendedorOrAdmin")]
    public async Task<IActionResult> ActualizarOportunidad(int id, [FromBody] OportunidadRequest request, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid) 
                return BadRequest(new { message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });

            var oportunidad = await _context.Oportunidades
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (oportunidad == null) 
                return NotFound(new { message = "Oportunidad no encontrada" });

            // Verificar cliente
            var cliente = await _context.Clientes.FindAsync(new object[] { request.ClienteId }, ct);
            if (cliente == null)
                return BadRequest(new { message = "Cliente no encontrado" });

            // Verificar usuario (vendedor)
            var usuario = await _context.Users.FindAsync(new object[] { request.UsuarioId }, ct);
            if (usuario == null)
                return BadRequest(new { message = "Usuario no encontrado" });

            // Verificar etapa
            var etapa = await _context.Etapas.FindAsync(new object[] { request.EtapaId }, ct);
            if (etapa == null)
                return BadRequest(new { message = "Etapa no encontrada" });

            // Verificar vehículo si se proporciona
            if (request.VehiculoId.HasValue)
            {
                var vehiculo = await _context.Vehiculos.FindAsync(new object[] { request.VehiculoId.Value }, ct);
                if (vehiculo == null)
                    return BadRequest(new { message = "Vehículo no encontrado" });
            }

            oportunidad.ClienteId = request.ClienteId;
            oportunidad.UsuarioId = request.UsuarioId;
            oportunidad.VehiculoId = request.VehiculoId;
            oportunidad.EtapaId = request.EtapaId;
            oportunidad.Activa = request.Activa;

            await _context.SaveChangesAsync(ct);

            return Ok(new { 
                message = "Oportunidad actualizada exitosamente",
                data = new { oportunidad.Id, Cliente = cliente.Nombre, Vendedor = $"{usuario.Nombre} {usuario.Apellido}" }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPut("{id:int}/estado")]
    [Authorize(Policy = "VendedorOrAdmin")]
    public async Task<IActionResult> ActualizarEstadoOportunidad(int id, [FromBody] ActualizarEstadoOportunidadRequest request, CancellationToken ct)
    {
        try
        {
            var oportunidad = await _context.Oportunidades
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (oportunidad == null) 
                return NotFound(new { message = "Oportunidad no encontrada" });

            oportunidad.Activa = request.Activa;

            await _context.SaveChangesAsync(ct);

            return Ok(new { 
                message = $"Oportunidad {(request.Activa ? "activada" : "desactivada")} exitosamente",
                data = new { oportunidad.Id, oportunidad.Activa }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOrGerente")]
    public async Task<IActionResult> EliminarOportunidad(int id, CancellationToken ct)
    {
        try
        {
            var oportunidad = await _context.Oportunidades
                .Include(o => o.Cotizaciones)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (oportunidad == null) 
                return NotFound(new { message = "Oportunidad no encontrada" });

            // Verificar si tiene cotizaciones asociadas
            if (oportunidad.Cotizaciones != null && oportunidad.Cotizaciones.Any())
                return BadRequest(new { message = "No se puede eliminar la oportunidad porque tiene cotizaciones asociadas" });

            _context.Oportunidades.Remove(oportunidad);
            await _context.SaveChangesAsync(ct);
            
            return Ok(new { message = "Oportunidad eliminada exitosamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("cliente/{clienteId:int}")]
    [Authorize(Policy = "AdminGerenteVendedor")]
    public async Task<IActionResult> GetOportunidadesPorCliente(int clienteId, CancellationToken ct)
    {
        try
        {
            var oportunidades = await _context.Oportunidades
                .AsNoTracking()
                .Where(o => o.ClienteId == clienteId)
                .Include(o => o.Usuario)
                .Include(o => o.Vehiculo)
                .Include(o => o.Etapa)
                .Select(o => new
                {
                    o.Id,
                    Vendedor = new { o.Usuario.Nombre, o.Usuario.Apellido },
                    Vehiculo = o.Vehiculo != null ? new { o.Vehiculo.Marca, o.Vehiculo.Modelo } : null,
                    Etapa = new { o.Etapa.Nombre },
                    o.Activa,
                    CotizacionesCount = o.Cotizaciones != null ? o.Cotizaciones.Count : 0
                })
                .ToListAsync(ct);

            return Ok(new { 
                data = oportunidades,
                total = oportunidades.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("usuario/{usuarioId:int}")]
    [Authorize(Policy = "AdminGerenteVendedor")]
    public async Task<IActionResult> GetOportunidadesPorUsuario(int usuarioId, CancellationToken ct)
    {
        try
        {
            var oportunidades = await _context.Oportunidades
                .AsNoTracking()
                .Where(o => o.UsuarioId == usuarioId)
                .Include(o => o.Cliente)
                .Include(o => o.Vehiculo)
                .Include(o => o.Etapa)
                .Select(o => new
                {
                    o.Id,
                    Cliente = new { o.Cliente.Nombre },
                    Vehiculo = o.Vehiculo != null ? new { o.Vehiculo.Marca, o.Vehiculo.Modelo } : null,
                    Etapa = new { o.Etapa.Nombre },
                    o.Activa,
                    CotizacionesCount = o.Cotizaciones != null ? o.Cotizaciones.Count : 0
                })
                .ToListAsync(ct);

            return Ok(new { 
                data = oportunidades,
                total = oportunidades.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }
}

public class OportunidadRequest
{
    public int ClienteId { get; set; }
    public int UsuarioId { get; set; }
    public int? VehiculoId { get; set; }
    public int EtapaId { get; set; }
    public bool Activa { get; set; } = true;
}

public class ActualizarEstadoOportunidadRequest
{
    public bool Activa { get; set; }
}