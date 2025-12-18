using System.ComponentModel.DataAnnotations;

namespace tecnova.Models
{
    public class Cliente
    {
        public int Id { get; set; }

        // 🔑 ID ÚNICO DEL CLIENTE
        [Required(ErrorMessage = "El ID del cliente es obligatorio.")]
        [StringLength(20, ErrorMessage = "El ID no puede exceder 20 caracteres.")]
        [Display(Name = "ID del cliente")]
        public string Identificacion { get; set; }

        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [StringLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres.")]
        [Display(Name = "Nombre completo")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        [Display(Name = "Correo electrónico")]
        public string Correo { get; set; }

        [Required(ErrorMessage = "La dirección es obligatoria.")]
        [StringLength(250, ErrorMessage = "La dirección no puede exceder 250 caracteres.")]
        public string Direccion { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [Phone(ErrorMessage = "Formato de teléfono inválido.")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }
    }
}
