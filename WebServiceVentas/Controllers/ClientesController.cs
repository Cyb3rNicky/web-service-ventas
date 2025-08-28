using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers
{
    [ApiController]
    [Route("api/clientes")] // minúsculas
    public class ClientesController : ControllerBase
    {
        private readonly VentasDbContext _context;

        public ClientesController(VentasDbContext context)
        {
            _context = context;
        }

        // POST: /api/clientes
        [HttpPost]
        public async Task<IActionResult> PostCliente([FromBody] Cliente cliente)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            // Mismo shape en la respuesta de creación
            return CreatedAtAction(nameof(GetClienteById), new { id = cliente.Id }, new { data = cliente });
        }

        // GET: /api/clientes
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetClientes()
        {
            var clientes = await _context.Clientes
                .AsNoTracking()
                .ToListAsync();

            return Ok(new { data = clientes }); // [] si no hay registros
        }

        // GET: /api/clientes/nombre/{nombre}
        [HttpGet("nombre/{nombre}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPorNombre(string nombre)
        {
            var clientes = await _context.Clientes
                .AsNoTracking()
                .Where(c => c.Nombre == nombre) // exacto; si quieres "contiene", avísame
                .ToListAsync();

            return Ok(new { data = clientes }); // [] si no hay coincidencias
        }

        // GET: /api/clientes/nit/{nit}
        [HttpGet("nit/{nit}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPorNit(string nit)
        {
            var cliente = await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.NIT == nit);

            if (cliente == null)
                return NotFound();

            return Ok(new { data = cliente });
        }

        // GET: /api/clientes/{id}
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetClienteById(int id)
        {
            var cliente = await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
                return NotFound();

            return Ok(new { data = cliente });
        }
    }
}