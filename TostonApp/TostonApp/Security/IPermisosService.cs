using System.Security.Claims;

namespace TostonApp.Security
{
    public interface IPermisosService
    {
        Task<bool> CanAsync(ClaimsPrincipal user, string permisoCodigo);
    }
}
