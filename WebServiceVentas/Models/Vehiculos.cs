using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServiceVentas.Models;

[Table("Vehiculos")]
public class Vehiculo
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Marca { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Modelo { get; set; } = string.Empty;

    public int Anio { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    [Range(0, double.MaxValue)]
    public decimal Precio { get; set; }

    public List<Oportunidad>? Oportunidades { get; set; }
    public List<CotizacionItem>? CotizacionItems { get; set; }
}