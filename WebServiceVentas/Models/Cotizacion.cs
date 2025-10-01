using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    public List<CotizacionItem> Items { get; set; } = new();

    public List<Factura>? Facturas { get; set; }
