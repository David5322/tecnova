using System.ComponentModel.DataAnnotations;

namespace tecnova.Models
{
    public class Producto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
        [StringLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres.")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres.")]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "El precio unitario es obligatorio.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser un número positivo.")]
        [Display(Name = "Precio unitario")]
        public decimal PrecioUnitario { get; set; }

        [Required(ErrorMessage = "El stock disponible es obligatorio.")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock debe ser un número entero no negativo.")]
        [Display(Name = "Stock disponible")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "El código único es obligatorio.")]
        [StringLength(100, ErrorMessage = "El código no puede exceder 100 caracteres.")]
        [Display(Name = "Código único")]
        public string Codigo { get; set; }
    }
}
