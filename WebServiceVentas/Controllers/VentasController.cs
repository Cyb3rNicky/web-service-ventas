using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers;

[ApiController]
[Route("api/ventas")]
[Authorize(Policy = "admin,gerente,vendedor")]
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

    [HttpPost]
    [Authorize(Roles = "admin,gerente,vendedor")]
    public async Task<IActionResult> CrearVenta([FromBody] VentaRequest request, CancellationToken ct)
    {
        if (request.Productos.Count == 0)
            return BadRequest("Debe incluir al menos un producto.");

        var cliente = await _context.Set<Cliente>().FindAsync(new object[] { request.ClienteId }, ct);
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
            var producto = await _context.Set<Producto>().FindAsync(new object[] { prodReq.ProductoId }, ct);
            if (producto == null)
                return BadRequest($"Producto {prodReq.ProductoId} no existe.");
            if (producto.Cantidad < prodReq.Cantidad)
                return BadRequest($"Stock insuficiente para el producto {producto.Nombre} (stock: {producto.Cantidad}).");

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
        _context.Set<Venta>().Add(venta);
        await _context.SaveChangesAsync(ct);

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

        return Ok(new { data = ventaDto });
    }

    [HttpGet]
    [Authorize(Roles = "admin,gerente,vendedor,asistente")]
    public async Task<IActionResult> GetVentas(CancellationToken ct)
    {
        var ventas = await _context.Set<Venta>()
            .AsNoTracking()
            .Include(v => v.Cliente)
            .Include(v => v.ProductosVendidos)
                .ThenInclude(vp => vp.Producto)
            .ToListAsync(ct);

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

        return Ok(new { data = resultado });
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "admin,gerente,vendedor,asistente")]
    public async Task<IActionResult> GetVentaPorId(int id, CancellationToken ct)
    {
        var venta = await _context.Set<Venta>()
            .AsNoTracking()
            .Include(v => v.Cliente)
            .Include(v => v.ProductosVendidos)
                .ThenInclude(vp => vp.Producto)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

        if (venta == null) return NotFound();

        var dto = new
        {
            venta.Id,
            Fecha = venta.Fecha.ToString("yyyy-MM-dd"),
            Cliente = new { venta.Cliente.Id, venta.Cliente.Nombre, venta.Cliente.NIT, venta.Cliente.Direccion },
            venta.Total,
            Productos = venta.ProductosVendidos.Select(pv => new
            {
                pv.ProductoId,
                Nombre = pv.Producto.Nombre,
                pv.Cantidad,
                pv.PrecioUnitario
            })
        };

        return Ok(new { data = dto });
    }
}