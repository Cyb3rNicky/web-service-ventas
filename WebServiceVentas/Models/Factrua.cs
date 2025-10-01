using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServiceVentas.Models;

[Table("Facturas")]
public class Factura
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CotizacionId { get; set; }
    public Cotizacion Cotizacion { get; set; } = null!;

    [Required, MaxLength(50)]
    public string Numero { get; set; } = string.Empty;

    public bool Emitida { get; set; } = false;

    [Column(TypeName = "numeric(18,2)")]
    public decimal Total { get; set; }
}