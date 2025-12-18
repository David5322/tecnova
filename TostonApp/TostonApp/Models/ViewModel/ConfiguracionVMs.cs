using System.ComponentModel.DataAnnotations;

namespace TostonApp.Models.ViewModels
{
    public class RolFormVM
    {
        public string? Id { get; set; }

        [Required, StringLength(80)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string Descripcion { get; set; } = string.Empty;
    }

    public class PermisoCheckVM
    {
        public int PermisoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Asignado { get; set; }
    }

    public class RolPermisosVM
    {
        public string RolId { get; set; } = string.Empty;
        public string NombreRol { get; set; } = string.Empty;
        public List<PermisoCheckVM> Permisos { get; set; } = new();
    }

    public class UsuarioRolVM
    {
        public string UsuarioId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;

        public List<RolItemVM> RolesDisponibles { get; set; } = new();
        public List<string> RolesActuales { get; set; } = new();
        public List<string> RolesSeleccionados { get; set; } = new();
    }

    public class RolItemVM
    {
        public string RolId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Seleccionado { get; set; }
    }

    public class UsuarioPermisosVM
    {
        public string UsuarioId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Permisos base (del catálogo) + estado (por rol) + override por usuario
        public List<UsuarioPermisoItemVM> Permisos { get; set; } = new();
    }

    public class UsuarioPermisoItemVM
    {
        public int PermisoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;

        // ¿Lo otorgan los roles?
        public bool PorRol { get; set; }

        // Override: null = sin override, true = permitir, false = denegar
        public bool? Override { get; set; }
    }
}
