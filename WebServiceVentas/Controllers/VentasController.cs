using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers;

[ApiController]
[Route("api/ventas")]
[Authorize(Policy = "AdminGerenteVendedor")] // 🔹 CORREGIDO: Usa el nombre de la política
public class VentasController : ControllerBase
{
    private readonly VentasDbContext _context;

    public VentasController(VentasDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize(Policy = "VendedorOrAdmin")] // 🔹 CORREGIDO: Solo vendedores y admins pueden crear ventas
    public async Task<IActionResult> CrearVenta([FromBody] VentaRequest request, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });

            if (request.Productos.Count == 0)
                return BadRequest(new { message = "Debe incluir al menos un producto" });

            var cliente = await _context.Clientes.FindAsync(new object[] { request.ClienteId }, ct);
            if (cliente == null)
                return BadRequest(new { message = "Cliente no encontrado" });

            decimal total = 0m;
            var productosVendidos = new List<VentaProducto>();
            var productosActualizar = new List<Producto>();

            // Validar stock y preparar productos
            foreach (var prodReq in request.Productos)
            {
                var producto = await _context.Productos.FindAsync(new object[] { prodReq.ProductoId }, ct);
                if (producto == null)
                    return BadRequest(new { message = $"Producto {prodReq.ProductoId} no existe" });

                if (producto.Cantidad < prodReq.Cantidad)
                    return BadRequest(new { message = $"Stock insuficiente para el producto {producto.Nombre} (stock: {producto.Cantidad}, solicitado: {prodReq.Cantidad})" });

                if (prodReq.Cantidad <= 0)
                    return BadRequest(new { message = $"La cantidad para el producto {producto.Nombre} debe ser mayor a 0" });

                // Crear item de venta
                var vp = new VentaProducto
                {
                    ProductoId = producto.Id,
                    Cantidad = prodReq.Cantidad,
                    PrecioUnitario = producto.Precio
                };

                productosVendidos.Add(vp);
                total += producto.Precio * prodReq.Cantidad;

                // Actualizar stock del producto
                producto.Cantidad -= prodReq.Cantidad;
                productosActualizar.Add(producto);
            }

            // Crear la venta
            var venta = new Venta
            {
                ClienteId = cliente.Id,
                Fecha = DateTime.Now,
                Total = total,
                ProductosVendidos = productosVendidos
            };

            _context.Ventas.Add(venta);
            await _context.SaveChangesAsync(ct);

            // Preparar respuesta
            var ventaDto = new
            {
                venta.Id,
                Fecha = venta.Fecha.ToString("yyyy-MM-dd HH:mm:ss"),
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
                    Producto = _context.Productos
                        .Where(p => p.Id == vp.ProductoId)
                        .Select(p => new { p.Nombre, p.Descripcion })
                        .FirstOrDefault(),
                    vp.Cantidad,
                    vp.PrecioUnitario,
                    Subtotal = vp.Cantidad * vp.PrecioUnitario
                })
            };

            return Ok(new { 
                message = "Venta registrada exitosamente",
                data = ventaDto 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Policy = "AdminInventarioAsistente")] // 🔹 CORREGIDO: admin, inventario, asistente
    public async Task<IActionResult> GetVentas(CancellationToken ct)
    {
        try
        {
            var ventas = await _context.Ventas
                .AsNoTracking()
                .Include(v => v.Cliente)
                .Include(v => v.ProductosVendidos)
                    .ThenInclude(vp => vp.Producto)
                .OrderByDescending(v => v.Fecha)
                .Select(v => new
                {
                    v.Id,
                    Fecha = v.Fecha.ToString("yyyy-MM-dd HH:mm:ss"),
                    Cliente = new { 
                        v.Cliente.Id, 
                        v.Cliente.Nombre, 
                        v.Cliente.NIT, 
                        v.Cliente.Direccion 
                    },
                    v.Total,
                    ProductosCount = v.ProductosVendidos.Count,
                    Productos = v.ProductosVendidos.Select(pv => new
                    {
                        pv.ProductoId,
                        Nombre = pv.Producto.Nombre,
                        pv.Cantidad,
                        pv.PrecioUnitario,
                        Subtotal = pv.Cantidad * pv.PrecioUnitario
                    })
                })
                .ToListAsync(ct);

            var estadisticas = new
            {
                totalVentas = ventas.Count,
                totalIngresos = ventas.Sum(v => v.Total),
                promedioVenta = ventas.Count > 0 ? ventas.Average(v => v.Total) : 0
            };

            return Ok(new { 
                data = ventas,
                estadisticas = estadisticas
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "AdminInventarioAsistente")] // 🔹 CORREGIDO: admin, inventario, asistente
    public async Task<IActionResult> GetVentaPorId(int id, CancellationToken ct)
    {
        try
        {
            var venta = await _context.Ventas
                .AsNoTracking()
                .Include(v => v.Cliente)
                .Include(v => v.ProductosVendidos)
                    .ThenInclude(vp => vp.Producto)
                .FirstOrDefaultAsync(v => v.Id == id, ct);

            if (venta == null) 
                return NotFound(new { message = "Venta no encontrada" });

            var dto = new
            {
                venta.Id,
                Fecha = venta.Fecha.ToString("yyyy-MM-dd HH:mm:ss"),
                Cliente = new { 
                    venta.Cliente.Id, 
                    venta.Cliente.Nombre, 
                    venta.Cliente.NIT, 
                    venta.Cliente.Direccion 
                },
                venta.Total,
                Productos = venta.ProductosVendidos.Select(pv => new
                {
                    pv.ProductoId,
                    Nombre = pv.Producto.Nombre,
                    Descripcion = pv.Producto.Descripcion,
                    pv.Cantidad,
                    pv.PrecioUnitario,
                    Subtotal = pv.Cantidad * pv.PrecioUnitario
                })
            };

            return Ok(new { data = dto });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("cliente/{clienteId:int}")]
    [Authorize(Policy = "AdminInventarioAsistente")] // 🔹 CORREGIDO: admin, inventario, asistente
    public async Task<IActionResult> GetVentasPorCliente(int clienteId, CancellationToken ct)
    {
        try
        {
            var cliente = await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == clienteId, ct);

            if (cliente == null)
                return NotFound(new { message = "Cliente no encontrado" });

            var ventas = await _context.Ventas
                .AsNoTracking()
                .Where(v => v.ClienteId == clienteId)
                .Include(v => v.ProductosVendidos)
                    .ThenInclude(vp => vp.Producto)
                .OrderByDescending(v => v.Fecha)
                .Select(v => new
                {
                    v.Id,
                    Fecha = v.Fecha.ToString("yyyy-MM-dd HH:mm:ss"),
                    v.Total,
                    ProductosCount = v.ProductosVendidos.Count,
                    Productos = v.ProductosVendidos.Take(3).Select(pv => new // Mostrar solo primeros 3 productos
                    {
                        Nombre = pv.Producto.Nombre,
                        pv.Cantidad
                    })
                })
                .ToListAsync(ct);

            var estadisticasCliente = new
            {
                totalVentas = ventas.Count,
                totalGastado = ventas.Sum(v => v.Total),
                promedioCompra = ventas.Count > 0 ? ventas.Average(v => v.Total) : 0
            };

            return Ok(new { 
                data = ventas,
                cliente = new { cliente.Id, cliente.Nombre, cliente.NIT },
                estadisticas = estadisticasCliente
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("fecha/{fecha}")]
    [Authorize(Policy = "AdminInventarioAsistente")] // 🔹 CORREGIDO: admin, inventario, asistente
    public async Task<IActionResult> GetVentasPorFecha(string fecha, CancellationToken ct)
    {
        try
        {
            if (!DateTime.TryParse(fecha, out var fechaBusqueda))
                return BadRequest(new { message = "Formato de fecha inválido. Use YYYY-MM-DD" });

            var ventas = await _context.Ventas
                .AsNoTracking()
                .Where(v => v.Fecha.Date == fechaBusqueda.Date)
                .Include(v => v.Cliente)
                .Include(v => v.ProductosVendidos)
                    .ThenInclude(vp => vp.Producto)
                .OrderByDescending(v => v.Fecha)
                .Select(v => new
                {
                    v.Id,
                    Fecha = v.Fecha.ToString("yyyy-MM-dd HH:mm:ss"),
                    Cliente = new { v.Cliente.Nombre, v.Cliente.NIT },
                    v.Total,
                    ProductosCount = v.ProductosVendidos.Count
                })
                .ToListAsync(ct);

            var estadisticasDia = new
            {
                totalVentas = ventas.Count,
                totalIngresos = ventas.Sum(v => v.Total),
                ventaMasAlta = ventas.Count > 0 ? ventas.Max(v => v.Total) : 0
            };

            return Ok(new { 
                data = ventas,
                fecha = fechaBusqueda.ToString("yyyy-MM-dd"),
                estadisticas = estadisticasDia
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("rango-fechas")]
    [Authorize(Policy = "AdminOnly")] // 🔹 CORREGIDO: Solo admin puede ver reportes por rango de fechas
    public async Task<IActionResult> GetVentasPorRangoFechas(
        [FromQuery] string fechaInicio, 
        [FromQuery] string fechaFin, 
        CancellationToken ct)
    {
        try
        {
            if (!DateTime.TryParse(fechaInicio, out var inicio) || !DateTime.TryParse(fechaFin, out var fin))
                return BadRequest(new { message = "Formato de fecha inválido. Use YYYY-MM-DD" });

            if (inicio > fin)
                return BadRequest(new { message = "La fecha de inicio no puede ser mayor a la fecha fin" });

            var ventas = await _context.Ventas
                .AsNoTracking()
                .Where(v => v.Fecha.Date >= inicio.Date && v.Fecha.Date <= fin.Date)
                .Include(v => v.Cliente)
                .Include(v => v.ProductosVendidos)
                .OrderByDescending(v => v.Fecha)
                .Select(v => new
                {
                    v.Id,
                    Fecha = v.Fecha.ToString("yyyy-MM-dd HH:mm:ss"),
                    Cliente = new { v.Cliente.Nombre, v.Cliente.NIT },
                    v.Total,
                    ProductosCount = v.ProductosVendidos.Count
                })
                .ToListAsync(ct);

            var estadisticasRango = new
            {
                totalVentas = ventas.Count,
                totalIngresos = ventas.Sum(v => v.Total),
                promedioDiario = ventas.Count > 0 ? ventas.Average(v => v.Total) : 0,
                ventaMasAlta = ventas.Count > 0 ? ventas.Max(v => v.Total) : 0,
                dias = (fin - inicio).TotalDays + 1
            };

            return Ok(new { 
                data = ventas,
                rango = new { fechaInicio = inicio.ToString("yyyy-MM-dd"), fechaFin = fin.ToString("yyyy-MM-dd") },
                estadisticas = estadisticasRango
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }
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