using TostonApp.Models.Identity;

namespace TostonApp.Models.Seguridad
{
    public class RolPermiso
    {
        public string RolId { get; set; }
        public ApplicationRole Rol { get; set; }

        public int PermisoId { get; set; }
        public Permiso Permiso { get; set; }
    }
}
