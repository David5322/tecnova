using System.Collections.Generic;

namespace TostonApp.Models.Seguridad
{
    public class Permiso
    {
        public int Id { get; set; }

        // Ej: "PRODUCTOS_VER", "PRODUCTOS_CREAR"
        public string Codigo { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public ICollection<RolPermiso> RolPermisos { get; set; }
            = new List<RolPermiso>();

        public ICollection<UsuarioPermiso> UsuarioPermisos { get; set; }
            = new List<UsuarioPermiso>();
    }
}
