using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WebServiceVentas.Models
{
    [Table("Productos")]
    public class Producto
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [Column("Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [Column("Precio")]
        public decimal Precio { get; set; }

        [Required]
        [Column("cantidad")] // minúscula
        public int cantidad { get; set; }

        [Required]
        [Column("descripcion", TypeName = "text")] // minúscula
        [JsonPropertyName("descripcion")]
        public string descripcion { get; set; } = string.Empty;
    }
}
