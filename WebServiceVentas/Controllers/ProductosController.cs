#12 4.967 /src/Models/Producto.cs(13,23): warning CS8618: Non-nullable property 'Nombre' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/src/WebServiceVentas.csproj]
#12 4.968 /src/Controllers/ProductosController.cs(84,23): error CS1061: 'Producto' does not contain a definition for 'Descripción' and no accessible extension method 'Descripción' accepting a first argument of type 'Producto' could be found (are you missing a using directive or an assembly reference?) [/src/WebServiceVentas.csproj]
#12 4.968 /src/Controllers/ProductosController.cs(84,46): error CS1061: 'Producto' does not contain a definition for 'Descripción' and no accessible extension method 'Descripción' accepting a first argument of type 'Producto' could be found (are you missing a using directive or an assembly reference?) [/src/WebServiceVentas.csproj]
#12 ERROR: process "/bin/sh -c dotnet publish -c Release -o /app/publish" did not complete successfully: exit code: 1
------
 > [build 5/5] RUN dotnet publish -c Release -o /app/publish:
0.989   Determining projects to restore...
1.604   All projects are up-to-date for restore.
4.967 /src/Models/Producto.cs(13,23): warning CS8618: Non-nullable property 'Nombre' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/src/WebServiceVentas.csproj]
4.968 /src/Controllers/ProductosController.cs(84,23): error CS1061: 'Producto' does not contain a definition for 'Descripción' and no accessible extension method 'Descripción' accepting a first argument of type 'Producto' could be found (are you missing a using directive or an assembly reference?) [/src/WebServiceVentas.csproj]
4.968 /src/Controllers/ProductosController.cs(84,46): error CS1061: 'Producto' does not contain a definition for 'Descripción' and no accessible extension method 'Descripción' accepting a first argument of type 'Producto' could be found (are you missing a using directive or an assembly reference?) [/src/WebServiceVentas.csproj]
------
Dockerfile:7
--------------------
   5 |     COPY . .
   6 |     RUN dotnet restore
   7 | >>> RUN dotnet publish -c Release -o /app/publish
   8 |     
   9 |     # Runtime stage
--------------------
error: failed to solve: process "/bin/sh -c dotnet publish -c Release -o /app/publish" did not complete successfully: exit code: 1﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Data;
using WebServiceVentas.Models;

namespace VentasApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private readonly VentasDbContext _context;

        public ProductosController(VentasDbContext context)
        {
            _context = context;
        }

        // GET api/productos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Producto>>> GetProductos()
        {
            return await _context.Productos.AsNoTracking().ToListAsync();
        }

        // GET api/productos/{nombre}
        [HttpGet("{nombre}")]
        public async Task<ActionResult<Producto>> GetProducto(string nombre)
        {
            var producto = await _context.Productos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Nombre == nombre);

            if (producto == null) return NotFound();
            return producto;
        }

        // POST api/productos
        [HttpPost]
        public async Task<ActionResult<Producto>> PostProducto(Producto producto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Productos.Add(producto);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (await _context.Productos.AnyAsync(p => p.Nombre == producto.Nombre))
                    return Conflict($"Ya existe un producto con el nombre '{producto.Nombre}'.");
                throw;
            }

            return CreatedAtAction(nameof(GetProducto), new { nombre = producto.Nombre }, producto);
        }

        // PUT api/productos/{nombre}
        [HttpPut("{nombre}")]
        public async Task<IActionResult> PutProducto(string nombre, Producto producto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Enforzar coherencia entre URL y payload
            if (!string.Equals(nombre, producto.Nombre, StringComparison.Ordinal))
            {
                return BadRequest("El nombre de la URL no coincide con el del producto.");
            }

            var existente = await _context.Productos.FirstOrDefaultAsync(p => p.Nombre == nombre);
            if (existente == null) return NotFound();

            // Actualizar campos (no cambiamos el Id)
            existente.Precio = producto.Precio;
            existente.Cantidad = producto.Cantidad;
            existente.Descripcion = producto.Descripcion;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Productos.AnyAsync(p => p.Nombre == nombre))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        // DELETE api/productos/{nombre}
        [HttpDelete("{nombre}")]
        public async Task<IActionResult> DeleteProducto(string nombre)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Nombre == nombre);
            if (producto == null) return NotFound();

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
