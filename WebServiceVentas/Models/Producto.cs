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

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Column("Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        [Column("Precio")]
        public decimal Precio { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "La cantidad no puede ser negativa")]
        [Column("cantidad")]
        public int Cantidad { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [Column("descripcion", TypeName = "text")]
        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; } = string.Empty;
    }
}
