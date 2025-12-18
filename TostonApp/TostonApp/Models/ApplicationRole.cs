using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using TostonApp.Models.Seguridad;

namespace TostonApp.Models.Identity
{
    public class ApplicationRole : IdentityRole
    {
        public string Descripcion { get; set; } = string.Empty;

        // Relación con permisos
        public ICollection<RolPermiso> RolPermisos { get; set; }
            = new List<RolPermiso>();
    }
}
