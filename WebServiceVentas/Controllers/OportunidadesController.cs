using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers;

[ApiController]
[Route("api/oportunidades")]
[Authorize(Policy = "admin,gerente,vendedor")]
public class OportunidadesController : ControllerBase
{
    private readonly VentasDbContext _context;

    public OportunidadesController(VentasDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "admin,gerente,vendedor,asistente")] // Asistentes solo ven
    public async Task<IActionResult> GetOportunidades(CancellationToken ct)
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
                Vehiculo = o.Vehiculo != null ? new { o.Vehiculo.Id, o.Vehiculo.Marca, o.Vehiculo.Modelo } : null,
                Etapa = new { o.Etapa.Id, o.Etapa.Nombre, o.Etapa.Orden },
                o.Activa,
                CotizacionesCount = _context.Cotizaciones.Count(c => c.OportunidadId == o.Id)
            })
            .ToListAsync(ct);

        return Ok(new { data = oportunidades });
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "admin,gerente,vendedor,asistente")] // Asistentes solo ven
    public async Task<IActionResult> GetOportunidadPorId(int id, CancellationToken ct)
    {
        var oportunidad = await _context.Oportunidades
            .AsNoTracking()
            .Include(o => o.Cliente)
            .Include(o => o.Usuario)
            .Include(o => o.Vehiculo)
            .Include(o => o.Etapa)
            .Include(o => o.Cotizaciones)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (oportunidad == null) return NotFound();

        var cotizacionesList = new List<object>();
        if (oportunidad.Cotizaciones != null)
        {
            foreach (var cotizacion in oportunidad.Cotizaciones)
            {
                cotizacionesList.Add(new
                {
                    cotizacion.Id,
                    cotizacion.Total,
                    cotizacion.Activa
                });
            }
        }

        var dto = new
        {
            oportunidad.Id,
            Cliente = new { oportunidad.Cliente.Id, oportunidad.Cliente.Nombre, oportunidad.Cliente.NIT, oportunidad.Cliente.Direccion },
            Vendedor = new { oportunidad.Usuario.Id, oportunidad.Usuario.Nombre, oportunidad.Usuario.Apellido, oportunidad.Usuario.Email },
            Vehiculo = oportunidad.Vehiculo != null ? new
            {
                oportunidad.Vehiculo.Id,
                oportunidad.Vehiculo.Marca,
                oportunidad.Vehiculo.Modelo,
                oportunidad.Vehiculo.Anio,
                oportunidad.Vehiculo.Precio
            } : null,
            Etapa = new { oportunidad.Etapa.Id, oportunidad.Etapa.Nombre, oportunidad.Etapa.Orden },
            oportunidad.Activa,
            Cotizaciones = cotizacionesList
        };

        return Ok(new { data = dto });
    }

    [HttpPost]
    [Authorize(Roles = "admin,gerente,vendedor")]
    public async Task<IActionResult> CrearOportunidad([FromBody] OportunidadRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Validar que existan las entidades relacionadas
        var cliente = await _context.Clientes.FindAsync(new object[] { request.ClienteId }, ct);
        if (cliente == null) return BadRequest("Cliente no encontrado");

        var usuario = await _context.Users.FindAsync(new object[] { request.UsuarioId }, ct);
        if (usuario == null) return BadRequest("Usuario no encontrado");

        var etapa = await _context.Etapas.FindAsync(new object[] { request.EtapaId }, ct);
        if (etapa == null) return BadRequest("Etapa no encontrada");

        if (request.VehiculoId.HasValue)
        {
            var vehiculo = await _context.Vehiculos.FindAsync(new object[] { request.VehiculoId.Value }, ct);
            if (vehiculo == null) return BadRequest("Vehículo no encontrado");
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

        return CreatedAtAction(nameof(GetOportunidadPorId), new { id = oportunidad.Id }, new { data = oportunidad });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin,gerente,vendedor")]
    public async Task<IActionResult> ActualizarOportunidad(int id, [FromBody] OportunidadUpdateRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var oportunidad = await _context.Oportunidades.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (oportunidad == null) return NotFound();

        if (request.EtapaId.HasValue)
        {
            var etapa = await _context.Etapas.FindAsync(new object[] { request.EtapaId.Value }, ct);
            if (etapa == null) return BadRequest("Etapa no encontrada");
            oportunidad.EtapaId = request.EtapaId.Value;
        }

        if (request.VehiculoId.HasValue)
        {
            var vehiculo = await _context.Vehiculos.FindAsync(new object[] { request.VehiculoId.Value }, ct);
            if (vehiculo == null) return BadRequest("Vehículo no encontrado");
            oportunidad.VehiculoId = request.VehiculoId.Value;
        }

        if (request.Activa.HasValue)
        {
            oportunidad.Activa = request.Activa.Value;
        }

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin,gerente")] // Vendedores no eliminan
    public async Task<IActionResult> EliminarOportunidad(int id, CancellationToken ct)
    {
        var oportunidad = await _context.Oportunidades
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (oportunidad == null) return NotFound();

        var tieneCotizaciones = await _context.Cotizaciones
            .AnyAsync(c => c.OportunidadId == id, ct);

        if (tieneCotizaciones)
            return BadRequest("No se puede eliminar la oportunidad porque tiene cotizaciones asociadas");

        _context.Oportunidades.Remove(oportunidad);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    public class OportunidadRequest
    {
        public int ClienteId { get; set; }
        public int UsuarioId { get; set; }
        public int? VehiculoId { get; set; }
        public int EtapaId { get; set; }
    }

    public class OportunidadUpdateRequest
    {
        public int? VehiculoId { get; set; }
        public int? EtapaId { get; set; }
        public bool? Activa { get; set; }
    }
}