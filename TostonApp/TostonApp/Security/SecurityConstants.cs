namespace TostonApp.Security
{
    /// <summary>
    /// Alias para compatibilidad: si en el proyecto quedó SecurityConstants,
    /// lo redirigimos a PermissionConstants.
    /// </summary>
    public static class SecurityConstants
    {
        public const string RolAdmin = PermissionConstants.RolAdmin;
        public static readonly string[] PermisosProtegidosAdmin = PermissionConstants.PermisosProtegidosAdmin;
        public const string PolicyPrefix = PermissionConstants.PolicyPrefix;
    }
}
