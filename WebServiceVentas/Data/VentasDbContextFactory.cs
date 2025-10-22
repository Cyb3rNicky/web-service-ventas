using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using WebServiceVentas.Data;

public class VentasDbContextFactory : IDesignTimeDbContextFactory<VentasDbContext>
{
    public VentasDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<VentasDbContext>();
        optionsBuilder.UseNpgsql("Host=dpg-d3eidvb3fgac7387mi3g-a.oregon-postgres.render.com;Port=5432;Database=dbventas_7ikl;Username=dbventas_7ikl_user;Password=IpY6UWxcuGS0104BCddbJYbRmcBTXAnu;SSL Mode=Require;Trust Server Certificate=True;");

        return new VentasDbContext(optionsBuilder.Options);
    }
}