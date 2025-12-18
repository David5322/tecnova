using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using TostonApp.Data;
using TostonApp.Models.Identity;

namespace TostonApp.Security
{
    public class PermisoAuthorizationHandler : AuthorizationHandler<PermisoRequirement>
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMemoryCache _cache;

        private static readonly TimeSpan CacheTTL = TimeSpan.FromMinutes(5);

        public PermisoAuthorizationHandler(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IMemoryCache cache)
        {
            _db = db;
            _userManager = userManager;
            _cache = cache;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermisoRequirement requirement)
        {
            if (context.User?.Identity?.IsAuthenticated != true)
                return;

            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return;

            var codigoPermiso = requirement.CodigoPermiso?.Trim();
            if (string.IsNullOrWhiteSpace(codigoPermiso))
                return;

            // Cache key: usuario+permiso
            var cacheKey = $"permiso:{userId}:{codigoPermiso}".ToLowerInvariant();
            if (_cache.TryGetValue(cacheKey, out bool cachedAllowed))
            {
                if (cachedAllowed) context.Succeed(requirement);
                return;
            }

            // Verificar usuario activo
            var userEntity = await _userManager.FindByIdAsync(userId);
            if (userEntity is null || !userEntity.Activo)
            {
                _cache.Set(cacheKey, false, CacheTTL);
                return;
            }

            // 1) Override por usuario (UsuariosPermisos)
            var overridePermitido = await _db.UsuarioPermisos
                .AsNoTracking()
                .Where(up => up.UsuarioId == userId && up.Permiso.Codigo == codigoPermiso)
                .Select(up => (bool?)up.Permitido)
                .FirstOrDefaultAsync();

            if (overridePermitido.HasValue)
            {
                var allowed = overridePermitido.Value;
                _cache.Set(cacheKey, allowed, CacheTTL);
                if (allowed) context.Succeed(requirement);
                return;
            }

            // 2) Permisos por roles (RolesPermisos)
            var roles = await _userManager.GetRolesAsync(userEntity);
            if (roles is null || roles.Count == 0)
            {
                _cache.Set(cacheKey, false, CacheTTL);
                return;
            }

            // Nota: rp.Rol.Name existe porque tu RolPermiso enlaza ApplicationRole
            var allowedByRole = await _db.RolPermisos
                .AsNoTracking()
                .Where(rp => roles.Contains(rp.Rol.Name!) && rp.Permiso.Codigo == codigoPermiso)
                .AnyAsync();

            _cache.Set(cacheKey, allowedByRole, CacheTTL);

            if (allowedByRole)
                context.Succeed(requirement);
        }
    }
}
