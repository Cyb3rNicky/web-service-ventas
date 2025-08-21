using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServiceVentas.Models
{
    [Table("AspNetUsers")]
    public class Usuario : IdentityUser<int>
    {
        // Nombre y apellido personalizados
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
    }
}