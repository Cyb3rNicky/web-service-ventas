using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServiceVentas.Models
{
    [Table("Ventas")]
    public class Venta
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public decimal Total { get; set; }

        public List<VentaProducto> ProductosVendidos { get; set; } = new();
    }

    [Table("VentaProductos")]
    public class VentaProducto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int VentaId { get; set; }
        public Venta Venta { get; set; } = null!;

        [Required]
        public int ProductoId { get; set; }
        public Producto Producto { get; set; } = null!;

        [Required]
        public int Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }
    }
}
