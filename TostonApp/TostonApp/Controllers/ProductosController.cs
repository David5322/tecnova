using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TostonApp.Data;
using TostonApp.Models.Dominio;

namespace TostonApp.Controllers
{
    [Authorize(Policy = "PERMISO:PRODUCTOS_VER")]
    public class ProductosController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ProductosController(ApplicationDbContext db)
        {
            _db = db;
        }

        // LISTA
        public async Task<IActionResult> Index()
        {
            // Admin ve todo; Cliente verá lo que tu lógica de vista permita.
            // Aquí listamos todo; si quieres filtrar por VisibleParaClientes para cliente,
            // lo hacemos luego con IPermisosService o con una policy distinta.
            var items = await _db.Productos
                .AsNoTracking()
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return View(items);
        }

        // DETALLE
        public async Task<IActionResult> Details(int id)
        {
            var producto = await _db.Productos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (producto == null) return NotFound();
            return View(producto);
        }

        // CREAR
        [Authorize(Policy = "PERMISO:PRODUCTOS_CREAR")]
        public IActionResult Create() => View(new Producto());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "PERMISO:PRODUCTOS_CREAR")]
        public async Task<IActionResult> Create(Producto model)
        {
            if (!ModelState.IsValid) return View(model);

            _db.Productos.Add(model);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // EDITAR
        [Authorize(Policy = "PERMISO:PRODUCTOS_EDITAR")]
        public async Task<IActionResult> Edit(int id)
        {
            var producto = await _db.Productos.FindAsync(id);
            if (producto == null) return NotFound();
            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "PERMISO:PRODUCTOS_EDITAR")]
        public async Task<IActionResult> Edit(int id, Producto model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var entity = await _db.Productos.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return NotFound();

            entity.Nombre = model.Nombre;
            entity.Descripcion = model.Descripcion;
            entity.Precio = model.Precio;
            entity.VisibleParaClientes = model.VisibleParaClientes;
            entity.Activo = model.Activo;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ELIMINAR
        [Authorize(Policy = "PERMISO:PRODUCTOS_ELIMINAR")]
        public async Task<IActionResult> Delete(int id)
        {
            var producto = await _db.Productos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (producto == null) return NotFound();
            return View(producto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "PERMISO:PRODUCTOS_ELIMINAR")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var entity = await _db.Productos.FindAsync(id);
            if (entity == null) return NotFound();

            _db.Productos.Remove(entity);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // EXTRA "PRO": Cambiar visibilidad (para probar permisos)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "PERMISO:PRODUCTOS_EDITAR")]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var entity = await _db.Productos.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return NotFound();

            entity.VisibleParaClientes = !entity.VisibleParaClientes;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
