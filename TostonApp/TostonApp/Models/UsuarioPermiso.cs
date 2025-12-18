using TostonApp.Models.Identity;

namespace TostonApp.Models.Seguridad
{
    public class UsuarioPermiso
    {
        public string UsuarioId { get; set; }
        public ApplicationUser Usuario { get; set; }

        public int PermisoId { get; set; }
        public Permiso Permiso { get; set; }

        // true = permitido | false = denegado
        public bool Permitido { get; set; }
    }
}
