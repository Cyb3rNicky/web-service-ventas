using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebServiceVentas.Models

{
    public class Producto
    {
        public int Id { get; set; }

        [Required]
        public string Nombre { get; set; }

        public decimal Precio { get; set; }

        public int cantidad { get; set; }

        [Column(TypeName = "text")]
        public string descripcion { get; set; }
    }
}
