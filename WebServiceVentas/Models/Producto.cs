using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServiceVentas.Models
{
    [Table("Productos")]
    public class Producto
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        [Column(TypeName = "numeric")]
        public decimal Precio { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "La cantidad no puede ser negativa")]
        public int Cantidad { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [Column(TypeName = "text")]
        public string Descripcion { get; set; } = string.Empty;
    }
}
