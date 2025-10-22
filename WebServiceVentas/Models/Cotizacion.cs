using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization; 

namespace WebServiceVentas.Models;

[Table("Cotizaciones")]
public class Cotizacion
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OportunidadId { get; set; }
    public Oportunidad Oportunidad { get; set; } = null!;

    public bool Activa { get; set; } = true;

    [Column(TypeName = "numeric(18,2)")]
    public decimal Total { get; set; }

    [JsonIgnore]
    public ICollection<CotizacionItem> Items { get; set; } = new List<CotizacionItem>();

    [JsonIgnore]
    public ICollection<Factura> Facturas { get; set; }
}
