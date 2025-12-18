using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tecnova.Models
{
    public class Venta
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Cliente")]
        public int ClienteId { get; set; }

        [Required]
        [Display(Name = "Fecha de venta")]
        public DateTime Fecha { get; set; }

        [Required]
        [Range(0.0, double.MaxValue)]
        [Display(Name = "Total")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        // Navegación
        public Cliente Cliente { get; set; }

        public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
    }
}
