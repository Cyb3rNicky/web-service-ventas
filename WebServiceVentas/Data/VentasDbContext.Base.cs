using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebServiceVentas.Models;

namespace WebServiceVentas.Data;

// Esta es la parte "base" que declara la herencia y el constructor.
public partial class VentasDbContext : IdentityDbContext<Usuario, IdentityRole<int>, int>
{
    public VentasDbContext(DbContextOptions<VentasDbContext> options)
        : base(options)
    {
    }

    // Si en algún momento quieres hooks globales, puedes ponerlos aquí.
}