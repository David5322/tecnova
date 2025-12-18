using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using TostonApp.Data;
using TostonApp.Models.Identity;

namespace TostonApp.Security
{
    public class PermisosService : IPermisosService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMemoryCache _cache;

        private static readonly TimeSpan CacheTTL = TimeSpan.FromMinutes(5);

        public PermisosService(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IMemoryCache cache)
        {
            _db = db;
            _userManager = userManager;
            _cache = cache;
        }

        public async Task<bool> CanAsync(ClaimsPrincipal user, string permisoCodigo)
        {
            if (user?.Identity?.IsAuthenticated != true) return false;

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return false;

            permisoCodigo = (permisoCodigo ?? "").Trim();
            if (string.IsNullOrWhiteSpace(permisoCodigo)) return false;

            var cacheKey = $"permiso:{userId}:{permisoCodigo}".ToLowerInvariant();
            if (_cache.TryGetValue(cacheKey, out bool cachedAllowed))
                return cachedAllowed;

            var userEntity = await _userManager.FindByIdAsync(userId);
            if (userEntity is null || !userEntity.Activo)
            {
                _cache.Set(cacheKey, false, CacheTTL);
                return false;
            }

            // Override por usuario
            var overridePermitido = await _db.UsuarioPermisos
                .AsNoTracking()
                .Where(up => up.UsuarioId == userId && up.Permiso.Codigo == permisoCodigo)
                .Select(up => (bool?)up.Permitido)
                .FirstOrDefaultAsync();

            if (overridePermitido.HasValue)
            {
                _cache.Set(cacheKey, overridePermitido.Value, CacheTTL);
                return overridePermitido.Value;
            }

            // Roles -> permisos
            var roles = await _userManager.GetRolesAsync(userEntity);
            if (roles is null || roles.Count == 0)
            {
                _cache.Set(cacheKey, false, CacheTTL);
                return false;
            }

            var allowedByRole = await _db.RolPermisos
                .AsNoTracking()
                .Where(rp => roles.Contains(rp.Rol.Name!) && rp.Permiso.Codigo == permisoCodigo)
                .AnyAsync();

            _cache.Set(cacheKey, allowedByRole, CacheTTL);
            return allowedByRole;
        }
    }
}
