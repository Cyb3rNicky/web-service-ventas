using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization; 

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

    [JsonIgnore]
    public List<Oportunidad>? Oportunidades { get; set; }
}
