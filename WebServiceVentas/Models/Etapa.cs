using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServiceVentas.Models;

[Table("Etapas")]
public class Etapa
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    // Orden dentro del pipeline
    public int Orden { get; set; }

    public int? Anio { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal? Precio { get; set; }

    public List<Oportunidad>? Oportunidades { get; set; }
}