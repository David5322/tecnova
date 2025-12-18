using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TostonApp.Data;
using TostonApp.Models.Identity;
using TostonApp.Models.Seguridad;
using TostonApp.Models.ViewModels;
using TostonApp.Security;

namespace TostonApp.Controllers
{
    [Authorize(Policy = "PERMISO:CONFIG_VER")]
    public class ConfiguracionController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ConfiguracionController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
        }

        // =========================
        // HOME CONFIG
        // =========================
        public IActionResult Index() => View();

        // =========================
        // ROLES
        // =========================

        [Authorize(Policy = "PERMISO:CONFIG_GESTIONAR_ROLES")]
        public async Task<IActionResult> Roles()
        {
            var roles = await _roleManager.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync();
            return View(roles);
        }

        [Authorize(Policy = "PERMISO:CONFIG_GESTIONAR_ROLES")]
        public IActionResult CrearRol() => View(new RolFormVM());

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "PERMISO:CONFIG_GESTIONAR_ROLES")]
        public async Task<IActionResult> CrearRol(RolFormVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var name = (vm.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("", "Nombre inválido.");
                return View(vm);
            }

            if (await _roleManager.RoleExistsAsync(name))
            {
                ModelState.AddModelError("", "El rol ya existe.");
                return View(vm);
            }

            var role = new ApplicationRole
            {
                Name = name,
                NormalizedName = name.ToUpperInvariant(),
                Descripcion = vm.Descripcion?.Trim() ?? ""
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", string.Join(" | ", result.Errors.Select(e => e.Description)));
                return View(vm);
            }

            TempData["Ok"] = "Rol creado correctamente.";
            return RedirectToAction(nameof(Roles));
        }

        [Authorize(Policy = "PERMISO:CONFIG_GESTIONAR_ROLES")]
        public async Task<IActionResult> EditarRol(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            // opcional: bloquear edición de nombre del Admin
            // (si lo quieres, puedes ocultar el input en la vista o forzar aquí)
            return View(new RolFormVM
            {
                Id = role.Id,
                Name = role.Name ?? "",
                Descripcion = role.Descripcion ?? ""
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "PERMISO:CONFIG_GESTIONAR_ROLES")]
        public async Task<IActionResult> EditarRol(RolFormVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var role = await _roleManager.FindByIdAsync(vm.Id!);
            if (role == null) return NotFound();

            var newName = (vm.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                ModelState.AddModelError("", "Nombre inválido.");
                return View(vm);
            }

            // 🔒 Si es Admin, NO permitimos renombrarlo (recomendado)
            if (role.Name == PermissionConstants.RolAdmin && newName != PermissionConstants.RolAdmin)
            {
                TempData["Error"] = "El rol Administrador no se puede renombrar.";
                return RedirectToAction(nameof(EditarRol), new { id = role.Id });
            }

            // Si cambias nombre de un rol no-admin, verifica duplicado
            if (role.Name != newName && await _roleManager.RoleExistsAsync(newName))
            {
                ModelState.AddModelError("", "Ya existe un rol con ese nombre.");
                return View(vm);
            }

            role.Name = newName;
            role.NormalizedName = newName.ToUpperInvariant();
            role.Descripcion = vm.Descripcion?.Trim() ?? "";

            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", string.Join(" | ", result.Errors.Select(e => e.Description)));
                return View(vm);
            }

            TempData["Ok"] = "Rol actualizado.";
            return RedirectToAction(nameof(Roles));
        }

        // 🔒 NO eliminar rol Admin, ni roles con usuarios
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "PERMISO:CONFIG_GESTIONAR_ROLES")]
        public async Task<IActionResult> EliminarRol(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            if (role.Name == PermissionConstants.RolAdmin)
            {
                TempData["Error"] = "El rol Administrador no puede ser eliminado.";
                return RedirectToAction(nameof(Roles));
            }

            var tieneUsuarios = await _db.Set<IdentityUserRole<string>>()
                .AnyAsync(x => x.RoleId == id);

            if (tieneUsuarios)
            {
                TempData["Error"] = "No se puede eliminar el rol porque hay usuarios asignados.";
                return RedirectToAction(nameof(Roles));
            }

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Roles));
            }

            TempData["Ok"] = "Rol eliminado correctamente.";
            return RedirectToAction(nameof(Roles));
        }

        // =========================
        // PERMISOS POR ROL
        // =========================

        [Authorize(Policy = "PERMISO:CONFIG_GESTIONAR_PERMISOS")]
        public async Task<IActionResult> PermisosPorRol(string rolId)
        {
            var role = await _roleManager.FindByIdAsync(rolId);
            if (role == null) return NotFound();

            var permisos = await _db.Permisos.AsNoTracking().OrderBy(p => p.Codigo).ToListAsync();
            var asignados = await _db.RolPermisos
                .Where(rp => rp.RolId == rolId)
                .Select(rp => rp.PermisoId)
                .ToListAsync();

            var vm = new RolPermisosVM
            {
                RolId = role.Id,
                NombreRol = role.Name ?? "",
                Permisos = permisos.Select(p => new PermisoCheckVM
                {
                    PermisoId = p.Id,
                    Codigo = p.Codigo,
                    Descripcion = p.Descripcion,
                    Asignado = asignados.Contains(p.Id)
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "PERMISO:CONFIG_GESTIONAR_PERMISOS")]
        public async Task<IActionResult> PermisosPorRol(RolPermisosVM vm)
        {
            var role = await _roleManager.FindByIdAsync(vm.RolId);
            if (role == null) return NotFound();

            var selectedIds = vm.Permisos.Where(p => p.Asignado).Select(p => p.PermisoId).ToHashSet();

            // 🔒 Admin debe conservar permisos críticos
            if (role.Name == PermissionConstants.RolAdmin)
            {
                var criticosIds = await _db.Permisos
                    .Where(p => PermissionConstants.PermisosProtegidosAdmin.Contains(p.Codigo))
                    .Select(p => p.Id)
                    .ToListAsync();

                if (criticosIds.Any(id => !selectedIds.Contains(id)))
                {
                    TempData["Error"] = "El rol Administrador debe conservar sus permisos críticos.";
                    return RedirectToAction(nameof(PermisosPorRol), new { rolId = role.Id });
                }
            }

            var actuales = await _db.RolPermisos.Where(rp => rp.RolId == vm.RolId).ToListAsync();

            // quitar los que ya no están
            _db.RolPermisos.RemoveRange(actuales.Where(rp => !selectedIds.Contains(rp.PermisoId)));

            // agregar nuevos
            var nuevos = selectedIds
                .Where(pid => !actuales.Any(a => a.PermisoId == pid))
                .Select(pid => new RolPermiso { RolId = vm.RolId, PermisoId = pid })
                .ToList();

            if (nuevos.Any())
                await _db.RolPermisos.AddRangeAsync(nuevos);

            await _db.SaveChangesAsync();
            TempData["Ok"] = "Permisos del rol actualizados.";
            return RedirectToAction(nameof(PermisosPorRol), new { rolId = vm.RolId });
        }

        // =========================
        // USUARIOS
        // =========================

        [Authorize(Policy = "PERMISO:USUARIOS_VER")]
        public async Task<IActionResult> Usuarios()
        {
            var users = await _userManager.Users.AsNoTracking()
                .OrderBy(u => u.Email)
                .ToListAsync();

            return View(users);
        }

        // 🔒 No desactivar Admin
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "PERMISO:USUARIOS_GESTIONAR")]
        public async Task<IActionResult> ToggleActivo(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(PermissionConstants.RolAdmin))
            {
                TempData["Error"] = "No se puede desactivar un usuario Administrador.";
                return RedirectToAction(nameof(Usuarios));
            }

            user.Activo = !user.Activo;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
            else
                TempData["Ok"] = "Estado del usuario actualizado.";

            return RedirectToAction(nameof(Usuarios));
        }

        // =========================
        // ROLES POR USUARIO
        // =========================

        [Authorize(Policy = "PERMISO:USUARIOS_GESTIONAR")]
        public async Task<IActionResult> RolesPorUsuario(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var roles = await _roleManager.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync();
            var actuales = await _userManager.GetRolesAsync(user);

            var vm = new UsuarioRolVM
            {
                UsuarioId = user.Id,
                Email = user.Email ?? "",
                NombreCompleto = user.NombreCompleto ?? "",
                RolesActuales = actuales.ToList(),
                RolesDisponibles = roles.Select(r => new RolItemVM
                {
                    RolId = r.Id,
                    Name = r.Name ?? "",
                    Seleccionado = actuales.Contains(r.Name ?? "")
                }).ToList()
            };

            return View(vm);
        }

        // ✅ Compatible con tu VM: RolesSeleccionados = LISTA DE ROLE IDs (string)
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "PERMISO:USUARIOS_GESTIONAR")]
        public async Task<IActionResult> RolesPorUsuario(UsuarioRolVM vm)
        {
            var user = await _userManager.FindByIdAsync(vm.UsuarioId);
            if (user == null) return NotFound();

            // Roles actuales (NOMBRES)
            var actuales = await _userManager.GetRolesAsync(user);

            // Convertir RoleIds seleccionados a RoleNames
            var seleccionadosNombres = await _roleManager.Roles
                .Where(r => vm.RolesSeleccionados.Contains(r.Id))
                .Select(r => r.Name!)
                .ToListAsync();

            var currentUserId = _userManager.GetUserId(User);

            // 🔒 No permitir quitarse su propio Admin
            if (user.Id == currentUserId &&
                actuales.Contains(PermissionConstants.RolAdmin) &&
                !seleccionadosNombres.Contains(PermissionConstants.RolAdmin))
            {
                TempData["Error"] = "No puedes quitarte tu propio rol Administrador.";
                return RedirectToAction(nameof(RolesPorUsuario), new { userId = user.Id });
            }

            var remove = actuales.Except(seleccionadosNombres).ToList();
            if (remove.Any())
            {
                var r1 = await _userManager.RemoveFromRolesAsync(user, remove);
                if (!r1.Succeeded)
                {
                    TempData["Error"] = string.Join(" | ", r1.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(RolesPorUsuario), new { userId = user.Id });
                }
            }

            var add = seleccionadosNombres.Except(actuales).ToList();
            if (add.Any())
            {
                var r2 = await _userManager.AddToRolesAsync(user, add);
                if (!r2.Succeeded)
                {
                    TempData["Error"] = string.Join(" | ", r2.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(RolesPorUsuario), new { userId = user.Id });
                }
            }

            // ✅ invalidar cookie vieja
            await _userManager.UpdateSecurityStampAsync(user);

            // ✅ si es el usuario actual, refrescar sesión
            if (user.Id == currentUserId)
                await _signInManager.RefreshSignInAsync(user);

            TempData["Ok"] = "Roles actualizados correctamente.";
            return RedirectToAction(nameof(RolesPorUsuario), new { userId = user.Id });
        }

        // =========================
        // PERMISOS POR USUARIO (OVERRIDE allow/deny/inherit)
        // =========================

        [Authorize(Policy = "PERMISO:CONFIG_GESTIONAR_PERMISOS")]
        public async Task<IActionResult> PermisosPorUsuario(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Permisos catálogo
            var permisos = await _db.Permisos.AsNoTracking().OrderBy(p => p.Codigo).ToListAsync();

            // Permisos por roles del usuario
            var roleNames = await _userManager.GetRolesAsync(user);

            var roleIds = await _roleManager.Roles
                .Where(r => roleNames.Contains(r.Name!))
                .Select(r => r.Id)
                .ToListAsync();

            var permisosPorRol = await _db.RolPermisos
                .Where(rp => roleIds.Contains(rp.RolId))
                .Select(rp => rp.PermisoId)
                .Distinct()
                .ToListAsync();

            // Overrides
            var overrides = await _db.UsuarioPermisos
                .Where(up => up.UsuarioId == user.Id)
                .ToListAsync();

            var vm = new UsuarioPermisosVM
            {
                UsuarioId = user.Id,
                Email = user.Email ?? "",
                Permisos = permisos.Select(p =>
                {
                    var ov = overrides.FirstOrDefault(x => x.PermisoId == p.Id);
                    return new UsuarioPermisoItemVM
                    {
                        PermisoId = p.Id,
                        Codigo = p.Codigo,
                        Descripcion = p.Descripcion,
                        PorRol = permisosPorRol.Contains(p.Id),
                        Override = ov == null ? (bool?)null : ov.Permitido
                    };
                }).ToList()
            };

            return View(vm);
        }

        // modo: allow | deny | inherit
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "PERMISO:CONFIG_GESTIONAR_PERMISOS")]
        public async Task<IActionResult> GuardarPermisosPorUsuario(string userId, int permisoId, string modo)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // 🔒 opcional: NO permitir denegar críticos a Admin
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(PermissionConstants.RolAdmin))
            {
                var permiso = await _db.Permisos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == permisoId);
                if (permiso != null && PermissionConstants.PermisosProtegidosAdmin.Contains(permiso.Codigo) && modo == "deny")
                {
                    TempData["Error"] = "No se puede denegar un permiso crítico al Administrador.";
                    return RedirectToAction(nameof(PermisosPorUsuario), new { userId });
                }
            }

            var existing = await _db.UsuarioPermisos
                .FirstOrDefaultAsync(x => x.UsuarioId == userId && x.PermisoId == permisoId);

            if (modo == "inherit")
            {
                if (existing != null)
                {
                    _db.UsuarioPermisos.Remove(existing);
                    await _db.SaveChangesAsync();
                }

                await _userManager.UpdateSecurityStampAsync(user);
                if (_userManager.GetUserId(User) == userId)
                    await _signInManager.RefreshSignInAsync(user);

                TempData["Ok"] = "Permiso configurado en modo Heredar.";
                return RedirectToAction(nameof(PermisosPorUsuario), new { userId });
            }

            var permitido = modo == "allow";

            if (existing == null)
            {
                existing = new UsuarioPermiso
                {
                    UsuarioId = userId,
                    PermisoId = permisoId,
                    Permitido = permitido
                };
                _db.UsuarioPermisos.Add(existing);
            }
            else
            {
                existing.Permitido = permitido;
                _db.UsuarioPermisos.Update(existing);
            }

            await _db.SaveChangesAsync();

            await _userManager.UpdateSecurityStampAsync(user);
            if (_userManager.GetUserId(User) == userId)
                await _signInManager.RefreshSignInAsync(user);

            TempData["Ok"] = permitido ? "Override: Permitir aplicado." : "Override: Denegar aplicado.";
            return RedirectToAction(nameof(PermisosPorUsuario), new { userId });
        }
    }
}
