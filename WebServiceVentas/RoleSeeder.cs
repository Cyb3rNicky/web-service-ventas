using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace WebServiceVentas
{
    public static class RoleSeeder
    {
        public static async Task SeedRolesAsync(RoleManager<IdentityRole<int>> roleManager)
        {
            string[] roles = {
                "admin",        // Acceso total al sistema
                "gerente",      // Gestión de ventas y reportes
                "vendedor",     // Gestión de oportunidades y cotizaciones
                "asistente",    // Consultas y soporte
                "inventario"    // Gestión de vehículos y productos
             };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(role));
                }
            }
        }
    }
}
