using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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


    [JsonIgnore]
    public List<Oportunidad>? Oportunidades { get; set; }
    [JsonIgnore]
    public List<CotizacionItem>? CotizacionItems { get; set; }
}