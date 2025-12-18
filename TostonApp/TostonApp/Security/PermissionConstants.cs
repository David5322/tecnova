namespace TostonApp.Security
{
    public static class PermissionConstants
    {
        // Prefijo para policies dinámicas
        // Ej: [Authorize(Policy = "PERMISO:PRODUCTOS_VER")]
        public const string PolicyPrefix = "PERMISO:";

        // Rol raíz del sistema
        public const string RolAdmin = "Admin";

        // Permisos que el Admin SIEMPRE debe tener
        public static readonly string[] PermisosProtegidosAdmin =
        {
            "CONFIG_VER",
            "CONFIG_GESTIONAR_ROLES",
            "CONFIG_GESTIONAR_PERMISOS",
            "USUARIOS_VER",
            "USUARIOS_GESTIONAR"
        };
    }
}
