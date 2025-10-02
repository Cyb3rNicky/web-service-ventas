using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Models;

namespace WebServiceVentas.Data;

// Partial principal: define la herencia y el constructor
public partial class VentasDbContext : IdentityDbContext<Usuario, IdentityRole<int>, int>
{
    public VentasDbContext(DbContextOptions<VentasDbContext> options)
        : base(options)
    {
    }
}