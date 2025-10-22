using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace WebServiceVentas.Controllers;

[ApiController]
[Route("api/ventas")]
[Authorize(Policy = "AdminGerenteVendedor")]
public class VentasController : ControllerBase
{
    private readonly VentasDbContext _context;

    public VentasController(VentasDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize(Policy = "VendedorOrAdmin")]
    public async Task<IActionResult> CrearVenta([FromBody] VentaRequest request, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });

            if (request.Vehiculos.Count == 0)
                return BadRequest(new { message = "Debe incluir al menos un vehículo" });

            var cliente = await _context.Clientes.FindAsync(new object[] { request.ClienteId }, ct);
            if (cliente == null)
                return BadRequest(new { message = "Cliente no encontrado" });

            decimal total = 0m;
            var vehiculosVendidos = new List<VentaProducto>();

            // Validar stock y preparar vehículos
            foreach (var vehReq in request.Vehiculos)
            {
                var vehiculo = await _context.Vehiculos.FindAsync(new object[] { vehReq.VehiculoId }, ct);
                if (vehiculo == null)
                    return BadRequest(new { message = $"Vehículo {vehReq.VehiculoId} no existe" });

                // Si tienes un campo de stock/cantidad en Vehiculo, valida aquí
                // if (vehiculo.Cantidad < vehReq.Cantidad)
                //     return BadRequest(new { message = $"Stock insuficiente para el vehículo {vehiculo.Marca} {vehiculo.Modelo} (stock: {vehiculo.Cantidad}, solicitado: {vehReq.Cantidad})" });

                if (vehReq.Cantidad <= 0)
                    return BadRequest(new { message = $"La cantidad para el vehículo {vehiculo.Marca} {vehiculo.Modelo} debe ser mayor a 0" });

                var vp = new VentaProducto
                {
                    VehiculoId = vehiculo.Id,
                    Cantidad = vehReq.Cantidad,
                    PrecioUnitario = vehiculo.Precio
                };

                vehiculosVendidos.Add(vp);
                total += vehiculo.Precio * vehReq.Cantidad;

                // Actualizar stock del vehículo si aplica
                // vehiculo.Cantidad -= vehReq.Cantidad;
                // vehiculosActualizar.Add(vehiculo);
            }

            var venta = new Venta
            {
                ClienteId = cliente.Id,
                Fecha = DateTime.Now,
                Total = total,
                ProductosVendidos = vehiculosVendidos
            };

            _context.Ventas.Add(venta);
            await _context.SaveChangesAsync(ct);

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
                Vehiculos = venta.ProductosVendidos.Select(vv => new
                {
                    VehiculoId = vv.VehiculoId,
                    Vehiculo = _context.Vehiculos
                        .Where(v => v.Id == vv.VehiculoId)
                        .Select(v => new { v.Marca, v.Modelo, v.Anio })
                        .FirstOrDefault(),
                    vv.Cantidad,
                    vv.PrecioUnitario,
                    Subtotal = vv.Cantidad * vv.PrecioUnitario
                })
            };

            return Ok(new
            {
                message = "Venta registrada exitosamente",
                data = ventaDto
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    // DTOs
    public class VentaRequest
    {
        public int ClienteId { get; set; }
        public List<VehiculoVentaRequest> Vehiculos { get; set; } = new();
    }

    public class VehiculoVentaRequest
    {
        public int VehiculoId { get; set; }
        public int Cantidad { get; set; }
    }
}
