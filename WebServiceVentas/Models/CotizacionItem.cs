using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServiceVentas.Models;

[Table("CotizacionItems")]
public class CotizacionItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CotizacionId { get; set; }
    public Cotizacion Cotizacion { get; set; } = null!;

    [Required]
    public int VehiculoId { get; set; }
    public Vehiculo Vehiculo { get; set; } = null!;

    // Descripción “final” que vería el cliente (puede incluir paquete, color, etc.)
    [Required, MaxLength(250)]
    public string Descripcion { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser >= 1")]
    public int Cantidad { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    [Range(0, double.MaxValue)]
    public decimal PrecioUnitario { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    [Range(0, double.MaxValue)]
    public decimal Descuento { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal Total { get; set; }
}