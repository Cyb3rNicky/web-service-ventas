using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers;

[ApiController]
[Route("api/etapas")]
[Authorize(Policy = "Authenticated")] // 🔹 CORREGIDO: Todos los usuarios autenticados pueden ver etapas
public class EtapasController : ControllerBase
{
    private readonly VentasDbContext _context;

    public EtapasController(VentasDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = "Authenticated")] // ✅ CORRECTO: Cualquier usuario autenticado puede ver
    public async Task<IActionResult> GetEtapas(CancellationToken ct)
    {
        try
        {
            var etapas = await _context.Etapas
                .AsNoTracking()
                .OrderBy(e => e.Orden)
                .Select(e => new
                {
                    e.Id,
                    e.Nombre,
                    e.Orden,
                    e.Anio,
                    e.Precio,
                    OportunidadesCount = _context.Oportunidades.Count(o => o.EtapaId == e.Id),
                    OportunidadesActivasCount = _context.Oportunidades.Count(o => o.EtapaId == e.Id && o.Activa)
                })
                .ToListAsync(ct);

            return Ok(new { 
                data = etapas,
                total = etapas.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Authenticated")] // ✅ CORRECTO: Cualquier usuario autenticado puede ver
    public async Task<IActionResult> GetEtapaPorId(int id, CancellationToken ct)
    {
        try
        {
            var etapa = await _context.Etapas
                .AsNoTracking()
                .Where(e => e.Id == id)
                .Select(e => new
                {
                    e.Id,
                    e.Nombre,
                    e.Orden,
                    e.Anio,
                    e.Precio,
                    OportunidadesCount = _context.Oportunidades.Count(o => o.EtapaId == e.Id),
                    OportunidadesActivasCount = _context.Oportunidades.Count(o => o.EtapaId == e.Id && o.Activa),
                    Oportunidades = _context.Oportunidades
                        .Where(o => o.EtapaId == e.Id)
                        .Include(o => o.Cliente)
                        .Include(o => o.Usuario)
                        .Select(o => new
                        {
                            o.Id,
                            Cliente = new { o.Cliente.Nombre },
                            Vendedor = new { o.Usuario.Nombre, o.Usuario.Apellido },
                            o.Activa
                        })
                        .Take(10) // Limitar a 10 oportunidades para no sobrecargar
                        .ToList()
                })
                .FirstOrDefaultAsync(ct);

            if (etapa == null) 
                return NotFound(new { message = "Etapa no encontrada" });
                
            return Ok(new { data = etapa });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")] // ✅ CORRECTO: Solo admins pueden crear
    public async Task<IActionResult> CrearEtapa([FromBody] Etapa etapa, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid) 
                return BadRequest(new { message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });

            // Validar si ya existe una etapa con el mismo nombre
            var etapaExistente = await _context.Etapas
                .FirstOrDefaultAsync(e => e.Nombre == etapa.Nombre, ct);
                
            if (etapaExistente != null)
                return BadRequest(new { message = "Ya existe una etapa con este nombre" });

            // Validar si ya existe una etapa con el mismo orden
            var ordenExistente = await _context.Etapas
                .FirstOrDefaultAsync(e => e.Orden == etapa.Orden, ct);
                
            if (ordenExistente != null)
                return BadRequest(new { message = "Ya existe una etapa con este orden" });

            _context.Etapas.Add(etapa);
            await _context.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetEtapaPorId), new { id = etapa.Id }, new { 
                message = "Etapa creada exitosamente",
                data = new { etapa.Id, etapa.Nombre, etapa.Orden, etapa.Anio, etapa.Precio }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")] // ✅ CORRECTO: Solo admins pueden actualizar
    public async Task<IActionResult> ActualizarEtapa(int id, [FromBody] Etapa etapa, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid) 
                return BadRequest(new { message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });
                
            if (id != etapa.Id) 
                return BadRequest(new { message = "El ID no coincide" });

            var existente = await _context.Etapas.FirstOrDefaultAsync(e => e.Id == id, ct);
            if (existente == null) 
                return NotFound(new { message = "Etapa no encontrada" });

            // Validar si ya existe otra etapa con el mismo nombre
            var nombreExistente = await _context.Etapas
                .FirstOrDefaultAsync(e => e.Nombre == etapa.Nombre && e.Id != id, ct);
                
            if (nombreExistente != null)
                return BadRequest(new { message = "Ya existe otra etapa con este nombre" });

            // Validar si ya existe otra etapa con el mismo orden
            var ordenExistente = await _context.Etapas
                .FirstOrDefaultAsync(e => e.Orden == etapa.Orden && e.Id != id, ct);
                
            if (ordenExistente != null)
                return BadRequest(new { message = "Ya existe otra etapa con este orden" });

            existente.Nombre = etapa.Nombre;
            existente.Orden = etapa.Orden;
            existente.Anio = etapa.Anio;
            existente.Precio = etapa.Precio;

            await _context.SaveChangesAsync(ct);
            
            return Ok(new { 
                message = "Etapa actualizada exitosamente",
                data = new { existente.Id, existente.Nombre, existente.Orden, existente.Anio, existente.Precio }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")] // ✅ CORRECTO: Solo admins pueden eliminar
    public async Task<IActionResult> EliminarEtapa(int id, CancellationToken ct)
    {
        try
        {
            var etapa = await _context.Etapas
                .FirstOrDefaultAsync(e => e.Id == id, ct);

            if (etapa == null) 
                return NotFound(new { message = "Etapa no encontrada" });

            // Verificar si tiene oportunidades asociadas
            var tieneOportunidades = await _context.Oportunidades
                .AnyAsync(o => o.EtapaId == id, ct);

            if (tieneOportunidades)
                return BadRequest(new { message = "No se puede eliminar la etapa porque tiene oportunidades asociadas" });

            _context.Etapas.Remove(etapa);
            await _context.SaveChangesAsync(ct);
            
            return Ok(new { message = "Etapa eliminada exitosamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("activas")]
    [Authorize(Policy = "Authenticated")] // ✅ CORRECTO: Cualquier usuario autenticado puede ver
    public async Task<IActionResult> GetEtapasActivas(CancellationToken ct)
    {
        try
        {
            var etapas = await _context.Etapas
                .AsNoTracking()
                .Where(e => e.Anio >= DateTime.Now.Year - 1) // Etapas del año actual o anterior
                .OrderBy(e => e.Orden)
                .Select(e => new
                {
                    e.Id,
                    e.Nombre,
                    e.Orden,
                    e.Anio,
                    e.Precio,
                    OportunidadesActivasCount = _context.Oportunidades.Count(o => o.EtapaId == e.Id && o.Activa)
                })
                .ToListAsync(ct);

            return Ok(new { 
                data = etapas,
                total = etapas.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("{id:int}/oportunidades")]
    [Authorize(Policy = "AdminGerenteVendedor")] // 🔹 CORREGIDO: Solo usuarios con permisos de ventas pueden ver oportunidades
    public async Task<IActionResult> GetOportunidadesPorEtapa(int id, CancellationToken ct)
    {
        try
        {
            var etapa = await _context.Etapas
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id, ct);

            if (etapa == null) 
                return NotFound(new { message = "Etapa no encontrada" });

            var oportunidades = await _context.Oportunidades
                .AsNoTracking()
                .Where(o => o.EtapaId == id)
                .Include(o => o.Cliente)
                .Include(o => o.Usuario)
                .Include(o => o.Vehiculo)
                .Select(o => new
                {
                    o.Id,
                    Cliente = new { o.Cliente.Nombre, o.Cliente.NIT },
                    Vendedor = new { o.Usuario.Nombre, o.Usuario.Apellido },
                    Vehiculo = o.Vehiculo != null ? new { o.Vehiculo.Marca, o.Vehiculo.Modelo } : null,
                    o.Activa,
                    CotizacionesCount = o.Cotizaciones != null ? o.Cotizaciones.Count : 0
                })
                .ToListAsync(ct);

            return Ok(new { 
                data = oportunidades,
                total = oportunidades.Count,
                etapa = new { etapa.Id, etapa.Nombre }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }
}