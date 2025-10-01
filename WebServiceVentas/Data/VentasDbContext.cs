using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Models;

namespace WebServiceVentas.Data;

public partial class VentasDbContext
{
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<Etapa> Etapas => Set<Etapa>();
    public DbSet<Oportunidad> Oportunidades => Set<Oportunidad>();
    public DbSet<Cotizacion> Cotizaciones => Set<Cotizacion>();
    public DbSet<CotizacionItem> CotizacionItems => Set<CotizacionItem>();
    public DbSet<Factura> Facturas => Set<Factura>();

    // EXTENSIÓN del método OnModelCreating en un partial (opcional) para no ensuciar el archivo original.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Precisión global (ya tienes para Producto, replicar)
        modelBuilder.Entity<Cotizacion>()
            .Property(c => c.Total)
            .HasPrecision(18, 2);

        modelBuilder.Entity<CotizacionItem>(b =>
        {
            b.Property(i => i.PrecioUnitario).HasPrecision(18, 2);
            b.Property(i => i.Descuento).HasPrecision(18, 2);
            b.Property(i => i.Total).HasPrecision(18, 2);

            // Evitar duplicados de (Cotizacion, Vehiculo)
            b.HasIndex(i => new { i.CotizacionId, i.VehiculoId }).IsUnique();
        });

        modelBuilder.Entity<Factura>(b =>
        {
            b.Property(f => f.Total).HasPrecision(18, 2);
            b.HasIndex(f => f.Numero).IsUnique();
        });

        modelBuilder.Entity<Oportunidad>(b =>
        {
            b.HasOne(o => o.Cliente)
             .WithMany()
             .HasForeignKey(o => o.ClienteId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(o => o.Usuario)
             .WithMany() // Si luego quieres Usuario.Oportunidades cambia a ICollection<Oportunidad>
             .HasForeignKey(o => o.UsuarioId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(o => o.Vehiculo)
             .WithMany(v => v.Oportunidades)
             .HasForeignKey(o => o.VehiculoId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Cotizacion>(b =>
        {
            b.HasOne(c => c.Oportunidad)
             .WithMany(o => o.Cotizaciones)
             .HasForeignKey(c => c.OportunidadId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CotizacionItem>(b =>
        {
            b.HasOne(i => i.Cotizacion)
             .WithMany(c => c.Items)
             .HasForeignKey(i => i.CotizacionId);

            b.HasOne(i => i.Vehiculo)
             .WithMany(v => v.CotizacionItems)
             .HasForeignKey(i => i.VehiculoId);
        });

        modelBuilder.Entity<Factura>(b =>
        {
            b.HasOne(f => f.Cotizacion)
             .WithMany(c => c.Facturas)
             .HasForeignKey(f => f.CotizacionId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}