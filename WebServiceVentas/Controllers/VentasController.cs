using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers
{
    [ApiController]
    [Route("api/ventas")]
    [Authorize(Policy = "VendedorOrAdmin")] // Solo vendedores y admin
    public class VentasController : ControllerBase
    {
        private readonly VentasDbContext _context;

        public VentasController(VentasDbContext context)
        {
            _context = context;
        }

        public class VentaRequest
        {
            public int ClienteId { get; set; }
            public List<ProductoVentaRequest> Productos { get; set; } = new();
        }

        public class ProductoVentaRequest
        {
            public int ProductoId { get; set; }
            public int Cantidad { get; set; }
        }

        // POST: /api/ventas
        [HttpPost]
        public async Task<IActionResult> CrearVenta([FromBody] VentaRequest request)
        {
            var cliente = await _context.Clientes.FindAsync(request.ClienteId);
            if (cliente == null)
                return BadRequest("Cliente no encontrado.");

            decimal total = 0m;

            var venta = new Venta
            {
                ClienteId = cliente.Id,
                Fecha = DateTime.UtcNow,
                ProductosVendidos = new List<VentaProducto>()
            };

            foreach (var prodReq in request.Productos)
            {
                var producto = await _context.Productos.FindAsync(prodReq.ProductoId);
                if (producto == null)
                    return BadRequest($"Producto {prodReq.ProductoId} no existe.");
                if (producto.Cantidad < prodReq.Cantidad)
                    return BadRequest($"Stock insuficiente para el producto {producto.Nombre}.");

                producto.Cantidad -= prodReq.Cantidad;

                var vp = new VentaProducto
                {
                    ProductoId = producto.Id,
                    Cantidad = prodReq.Cantidad,
                    PrecioUnitario = producto.Precio
                };

                venta.ProductosVendidos.Add(vp);
                total += producto.Precio * prodReq.Cantidad;
            }

            venta.Total = total;
            _context.Ventas.Add(venta);
            await _context.SaveChangesAsync();

            var ventaDto = new
            {
                venta.Id,
                Fecha = venta.Fecha.ToString("yyyy-MM-dd"),
                Cliente = new
                {
                    cliente.Id,
                    cliente.Nombre,
                    cliente.NIT,
                    cliente.Direccion
                },
                venta.Total,
                Productos = venta.ProductosVendidos.Select(vp => new
                {
                    vp.ProductoId,
                    vp.Cantidad,
                    vp.PrecioUnitario
                })
            };

            // Mismo formato: { data: { ... } }
            return Ok(new { data = ventaDto });
        }

        // GET: /api/ventas
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetVentas()
        {
            var ventas = await _context.Ventas
                .AsNoTracking()
                .Include(v => v.Cliente)
                .Include(v => v.ProductosVendidos)
                    .ThenInclude(vp => vp.Producto)
                .ToListAsync();

            var resultado = ventas.Select(v => new
            {
                v.Id,
                Fecha = v.Fecha.ToString("yyyy-MM-dd"),
                Cliente = new { v.Cliente.Id, v.Cliente.Nombre, v.Cliente.NIT, v.Cliente.Direccion },
                v.Total,
                Productos = v.ProductosVendidos.Select(pv => new
                {
                    pv.ProductoId,
                    Nombre = pv.Producto.Nombre,
                    pv.Cantidad,
                    pv.PrecioUnitario
                })
            }).ToList();

            // Siempre { data: [...] } (vacío = [])
            return Ok(new { data = resultado });
        }

        // GET: /api/ventas/{id}
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetVentaPorId(int id)
        {
            var v = await _context.Ventas
                .AsNoTracking()
                .Include(x => x.Cliente)
                .Include(x => x.ProductosVendidos)
                    .ThenInclude(vp => vp.Producto)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (v is null) return NotFound();

            var dto = new
            {
                v.Id,
                Fecha = v.Fecha.ToString("yyyy-MM-dd"),
                Cliente = new { v.Cliente.Id, v.Cliente.Nombre, v.Cliente.NIT, v.Cliente.Direccion },
                v.Total,
                Productos = v.ProductosVendidos.Select(pv => new
                {
                    pv.ProductoId,
                    Nombre = pv.Producto.Nombre,
                    pv.Cantidad,
                    pv.PrecioUnitario
                })
            };

            // Mismo shape: { data: { ... } }
            return Ok(new { data = dto });
        }

        // GET: /api/ventas/cliente/{clienteId}
        [HttpGet("cliente/{clienteId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetVentasPorCliente(int clienteId)
        {
            var ventas = await _context.Ventas
                .AsNoTracking()
                .Where(v => v.ClienteId == clienteId)
                .Include(v => v.Cliente)
                .Include(v => v.ProductosVendidos)
                    .ThenInclude(vp => vp.Producto)
                .ToListAsync();

            var resultado = ventas.Select(v => new
            {
                v.Id,
                Fecha = v.Fecha.ToString("yyyy-MM-dd"),
                Cliente = new { v.Cliente.Id, v.Cliente.Nombre, v.Cliente.NIT, v.Cliente.Direccion },
                v.Total,
                Productos = v.ProductosVendidos.Select(pv => new
                {
                    pv.ProductoId,
                    Nombre = pv.Producto.Nombre,
                    pv.Cantidad,
                    pv.PrecioUnitario
                })
            }).ToList();

            // Importante: no NotFound para listas vacías; regresamos []
            return Ok(new { data = resultado });
        }
    }
}