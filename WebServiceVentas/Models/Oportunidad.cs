using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServiceVentas.Models;

[Table("Oportunidades")]
public class Oportunidad
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    [Required]
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int? VehiculoId { get; set; }
    public Vehiculo? Vehiculo { get; set; }

    [Required]
    public int EtapaId { get; set; }
    public Etapa Etapa { get; set; } = null!;

    public bool Activa { get; set; } = true;

    public List<Cotizacion>? Cotizaciones { get; set; }
}