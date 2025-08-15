using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Models;

namespace WebServiceVentas.Data
{
    public class VentasDbContext : DbContext
    {
        public VentasDbContext(DbContextOptions<VentasDbContext> options) : base(options) { }

        public DbSet<Producto> Productos { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Producto>()
                .ToTable("Productos");

            modelBuilder.Entity<Producto>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.Nombre).HasColumnName("Nombre");
                entity.Property(e => e.Precio).HasColumnName("Precio");
                entity.Property(e => e.Cantidad).HasColumnName("Cantidad");
                entity.Property(e => e.Descripcion).HasColumnName("Descripcion");
            });
        }
    }
}
