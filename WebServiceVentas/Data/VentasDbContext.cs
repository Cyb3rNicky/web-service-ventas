using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Models;

namespace WebServiceVentas.Data;

public partial class VentasDbContext
{
    // DbSets del “módulo vehículos / oportunidades”
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<Etapa> Etapas => Set<Etapa>();
    public DbSet<Oportunidad> Oportunidades => Set<Oportunidad>();
    public DbSet<Cotizacion> Cotizaciones => Set<Cotizacion>();
    public DbSet<CotizacionItem> CotizacionItems => Set<CotizacionItem>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<VentaProducto> VentaProductos => Set<VentaProducto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===================== Dominio Cotizaciones / Vehículos =====================
        modelBuilder.Entity<Cotizacion>()
            .Property(c => c.Total)
            .HasPrecision(18, 2);

        modelBuilder.Entity<CotizacionItem>(b =>
        {
            b.Property(i => i.PrecioUnitario).HasPrecision(18, 2);
            b.Property(i => i.Descuento).HasPrecision(18, 2);
            b.Property(i => i.Total).HasPrecision(18, 2);
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
             .WithMany()
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


        // Cliente
        modelBuilder.Entity<Cliente>(b =>
        {
            // Si NIT debe ser único descomenta:
            // b.HasIndex(c => c.NIT).IsUnique();
        });

        // Venta
        modelBuilder.Entity<Venta>(b =>
        {
            b.Property(v => v.Total).HasColumnType("numeric"); // coherente con AddVentas
            b.HasOne(v => v.Cliente)
             .WithMany()
             .HasForeignKey(v => v.ClienteId)
             .OnDelete(DeleteBehavior.Cascade); // coincide con migración actual
        });

        // VentaProducto
        modelBuilder.Entity<VentaProducto>(b =>
        {
            b.Property(vp => vp.PrecioUnitario).HasColumnType("numeric");
            b.HasOne(vp => vp.Venta)
             .WithMany(v => v.ProductosVendidos)
             .HasForeignKey(vp => vp.VentaId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(vp => vp.Vehiculo)
             .WithMany()
             .HasForeignKey(vp => vp.VehiculoId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}