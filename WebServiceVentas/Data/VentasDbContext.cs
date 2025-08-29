using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Models;

namespace WebServiceVentas.Data
{
    public class VentasDbContext : IdentityDbContext<Usuario, IdentityRole<int>, int>

    {
        public VentasDbContext(DbContextOptions<VentasDbContext> options) : base(options) { }

        public DbSet<Producto> Productos { get; set; } = null!;
        
        public DbSet<Cliente> Clientes { get; set; } = null!;

        public DbSet<Venta> Ventas { get; set; } = null!;

        public DbSet<VentaProducto> VentaProductos { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Solo mapear la tabla, las columnas ya se manejan con Data Annotations
            modelBuilder.Entity<Producto>()
                        .ToTable("Productos");

            // Opcional: definir precisión de Precio
            modelBuilder.Entity<Producto>()
                        .Property(p => p.Precio)
                        .HasPrecision(18, 2);
        }
    }
}
