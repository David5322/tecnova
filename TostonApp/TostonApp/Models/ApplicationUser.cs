using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using TostonApp.Models.Seguridad;

namespace TostonApp.Models.Identity
{
    public class ApplicationUser : IdentityUser
    {
        // Datos adicionales
        public string NombreCompleto { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        // Relación con permisos personalizados
        public ICollection<UsuarioPermiso> UsuarioPermisos { get; set; }
            = new List<UsuarioPermiso>();
    }
}
