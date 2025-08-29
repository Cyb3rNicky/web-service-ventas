using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using WebServiceVentas.Data;

public class VentasDbContextFactory : IDesignTimeDbContextFactory<VentasDbContext>
{
    public VentasDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<VentasDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=ventasdb;Username=postgres;Password=postgres123");

        return new VentasDbContext(optionsBuilder.Options);
    }
}