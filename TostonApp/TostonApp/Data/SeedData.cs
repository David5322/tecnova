using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TostonApp.Models.Dominio;
using TostonApp.Models.Identity;
using TostonApp.Models.Seguridad;

namespace TostonApp.Data
{
    public static class SeedData
    {
        public static async Task EnsureSeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            // 1) Migrar base de datos (scaffold no cambia esto)
            await db.Database.MigrateAsync();

            // 2) Roles base
            var adminRole = await EnsureRoleAsync(roleManager, "Admin", "Acceso total al sistema");
            var clienteRole = await EnsureRoleAsync(roleManager, "Cliente", "Acceso limitado según permisos configurados");

            // 3) Catálogo de permisos
            await EnsurePermisosCatalogoAsync(db);

            // 4) Asignación de permisos por rol (RBAC)
            // Admin -> todos los permisos
            var allPermCodes = await db.Permisos.AsNoTracking()
                .Select(p => p.Codigo)
                .ToListAsync();

            await EnsurePermisosRolAsync(db, adminRole.Id, allPermCodes);

            // Cliente -> permisos mínimos (ajusta a tu necesidad)
            await EnsurePermisosRolAsync(db, clienteRole.Id, new List<string>
            {
                "PRODUCTOS_VER",
                "PEDIDOS_CREAR"
            });

            // 5) Usuario Admin inicial
            var adminUser = await EnsureUserAsync(userManager,
                email: "admin@tostonapp.com",
                password: "Admin123*",
                nombreCompleto: "Administrador TostonApp",
                activo: true);

            await EnsureUserInRoleAsync(userManager, adminUser, adminRole.Name!);

            // 6) Usuario Cliente demo
            var clienteUser = await EnsureUserAsync(userManager,
                email: "cliente@tostonapp.com",
                password: "Cliente123*",
                nombreCompleto: "Cliente Demo",
                activo: true);

            await EnsureUserInRoleAsync(userManager, clienteUser, clienteRole.Name!);

            // 7) Override por usuario (ejemplo avanzado)
            // Ej: negar PEDIDOS_CREAR al cliente aunque el rol lo permita
            await SetUsuarioPermisoOverrideAsync(db, clienteUser.Id, "PEDIDOS_CREAR", permitido: false);

            // 8) Datos demo: productos
            await EnsureProductosDemoAsync(db);
        }

        // =========================
        // ROLES
        // =========================
        private static async Task<ApplicationRole> EnsureRoleAsync(
            RoleManager<ApplicationRole> roleManager,
            string roleName,
            string descripcion)
        {
            var existing = await roleManager.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (existing != null) return existing;

            var role = new ApplicationRole
            {
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant(),
                Descripcion = descripcion
            };

            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                var error = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new Exception($"No se pudo crear rol '{roleName}': {error}");
            }

            return role;
        }

        // =========================
        // USUARIOS
        // =========================
        private static async Task<ApplicationUser> EnsureUserAsync(
            UserManager<ApplicationUser> userManager,
            string email,
            string password,
            string nombreCompleto,
            bool activo)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user != null) return user;

            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                NombreCompleto = nombreCompleto,
                Activo = activo
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var error = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new Exception($"No se pudo crear usuario '{email}': {error}");
            }

            return user;
        }

        private static async Task EnsureUserInRoleAsync(
            UserManager<ApplicationUser> userManager,
            ApplicationUser user,
            string roleName)
        {
            if (await userManager.IsInRoleAsync(user, roleName)) return;

            var result = await userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                var error = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new Exception($"No se pudo asignar rol '{roleName}' a '{user.Email}': {error}");
            }
        }

        // =========================
        // PERMISOS: catálogo
        // =========================
        private static async Task EnsurePermisosCatalogoAsync(ApplicationDbContext db)
        {
            var basePerms = new List<Permiso>
            {
                new Permiso { Codigo = "CONFIG_VER", Descripcion = "Ver módulo de configuración" },
                new Permiso { Codigo = "CONFIG_GESTIONAR_ROLES", Descripcion = "Gestionar roles" },
                new Permiso { Codigo = "CONFIG_GESTIONAR_PERMISOS", Descripcion = "Gestionar permisos (roles/usuarios)" },

                new Permiso { Codigo = "USUARIOS_VER", Descripcion = "Ver usuarios" },
                new Permiso { Codigo = "USUARIOS_GESTIONAR", Descripcion = "Gestionar usuarios" },

                new Permiso { Codigo = "PRODUCTOS_VER", Descripcion = "Ver productos" },
                new Permiso { Codigo = "PRODUCTOS_CREAR", Descripcion = "Crear productos" },
                new Permiso { Codigo = "PRODUCTOS_EDITAR", Descripcion = "Editar productos" },
                new Permiso { Codigo = "PRODUCTOS_ELIMINAR", Descripcion = "Eliminar productos" },

                new Permiso { Codigo = "PEDIDOS_VER", Descripcion = "Ver pedidos" },
                new Permiso { Codigo = "PEDIDOS_CREAR", Descripcion = "Crear pedidos" },
                new Permiso { Codigo = "PEDIDOS_CANCELAR", Descripcion = "Cancelar pedidos" },

                new Permiso { Codigo = "REPORTES_VER", Descripcion = "Ver reportes" }
            };

            var existentes = await db.Permisos.AsNoTracking()
                .Select(p => p.Codigo)
                .ToListAsync();

            var set = existentes.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var nuevos = basePerms
                .Where(p => !set.Contains(p.Codigo))
                .ToList();

            if (nuevos.Count > 0)
            {
                db.Permisos.AddRange(nuevos);
                await db.SaveChangesAsync();
            }
        }

        // =========================
        // PERMISOS POR ROL
        // =========================
        private static async Task EnsurePermisosRolAsync(
            ApplicationDbContext db,
            string roleId,
            List<string> codigosPermiso)
        {
            // Trae IDs reales
            var permisos = await db.Permisos.AsNoTracking()
                .Where(p => codigosPermiso.Contains(p.Codigo))
                .Select(p => new { p.Id, p.Codigo })
                .ToListAsync();

            if (permisos.Count != codigosPermiso.Count)
            {
                var found = permisos.Select(x => x.Codigo).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var missing = codigosPermiso.Where(c => !found.Contains(c));
                throw new Exception($"Faltan permisos en BD: {string.Join(", ", missing)}");
            }

            // Ya asignados
            var asignados = await db.RolPermisos.AsNoTracking()
                .Where(rp => rp.RolId == roleId)
                .Select(rp => rp.PermisoId)
                .ToListAsync();

            var asignadosSet = asignados.ToHashSet();

            var toAdd = permisos
                .Where(p => !asignadosSet.Contains(p.Id))
                .Select(p => new RolPermiso
                {
                    RolId = roleId,
                    PermisoId = p.Id
                })
                .ToList();

            if (toAdd.Count > 0)
            {
                db.RolPermisos.AddRange(toAdd);
                await db.SaveChangesAsync();
            }
        }

        // =========================
        // OVERRIDE USUARIO (UsuariosPermisos)
        // =========================
        private static async Task SetUsuarioPermisoOverrideAsync(
            ApplicationDbContext db,
            string userId,
            string permisoCodigo,
            bool permitido)
        {
            var permiso = await db.Permisos.FirstOrDefaultAsync(p => p.Codigo == permisoCodigo);
            if (permiso is null) throw new Exception($"Permiso no existe: {permisoCodigo}");

            var existente = await db.UsuarioPermisos
                .FirstOrDefaultAsync(x => x.UsuarioId == userId && x.PermisoId == permiso.Id);

            if (existente is null)
            {
                db.UsuarioPermisos.Add(new UsuarioPermiso
                {
                    UsuarioId = userId,
                    PermisoId = permiso.Id,
                    Permitido = permitido
                });
            }
            else
            {
                existente.Permitido = permitido;
                db.UsuarioPermisos.Update(existente);
            }

            await db.SaveChangesAsync();
        }

        // =========================
        // PRODUCTOS DEMO
        // =========================
        private static async Task EnsureProductosDemoAsync(ApplicationDbContext db)
        {
            if (await db.Productos.AnyAsync()) return;

            db.Productos.AddRange(
                new Producto
                {
                    Nombre = "Tostón Clásico",
                    Descripcion = "Tostón tradicional con sal y limón.",
                    Precio = 6000m,
                    VisibleParaClientes = true,
                    Activo = true
                },
                new Producto
                {
                    Nombre = "Tostón Especial",
                    Descripcion = "Con salsa de la casa y toppings.",
                    Precio = 9000m,
                    VisibleParaClientes = true,
                    Activo = true
                },
                new Producto
                {
                    Nombre = "Producto Interno (Solo Admin)",
                    Descripcion = "No visible para clientes.",
                    Precio = 15000m,
                    VisibleParaClientes = false,
                    Activo = true
                }
            );

            await db.SaveChangesAsync();
        }
    }
}
