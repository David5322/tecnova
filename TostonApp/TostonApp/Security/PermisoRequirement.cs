using Microsoft.AspNetCore.Authorization;

namespace TostonApp.Security
{
    public sealed class PermisoRequirement : IAuthorizationRequirement
    {
        public string CodigoPermiso { get; }

        public PermisoRequirement(string codigoPermiso)
        {
            CodigoPermiso = codigoPermiso;
        }
    }
}
