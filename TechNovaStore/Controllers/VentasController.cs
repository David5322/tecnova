using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tecnova.Data;
using tecnova.Models;

namespace tecnova.Controllers
{
    [Authorize(Roles = "Admin")]
    public class VentasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VentasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // INDEX / HISTORIAL DE VENTAS
        public async Task<IActionResult> Index(string searchCliente, string searchClienteId, DateTime? fecha)
        {
            var ventas = _context.Ventas
                .Include(v => v.Cliente)
                .AsQueryable();

            // 🔍 FILTRO POR NOMBRE DEL CLIENTE
            if (!string.IsNullOrEmpty(searchCliente))
            {
                ventas = ventas.Where(v =>
                    v.Cliente.Nombre.Contains(searchCliente));
            }

            // 🔍 FILTRO POR ID DEL CLIENTE
            if (!string.IsNullOrEmpty(searchClienteId))
            {
                ventas = ventas.Where(v =>
                    v.Cliente.Identificacion == searchClienteId);
            }

            // 📅 FILTRO POR FECHA
            if (fecha.HasValue)
            {
                ventas = ventas.Where(v =>
                    v.Fecha.Date == fecha.Value.Date);
            }

            return View(await ventas.ToListAsync());
        }

        // DETALLES DE VENTA
        public async Task<IActionResult> Details(int id)
        {
            var venta = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (venta == null)
            {
                return NotFound();
            }

            return View(venta);
        }

        // GET: Ventas/Create
        public IActionResult Create()
        {
            ViewBag.Clientes = _context.Clientes.ToList();
            ViewBag.Productos = _context.Productos.ToList();
            return View();
        }

        // POST: Ventas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int ClienteId, int[] ProductoId, int[] Cantidad)
        {
            if (ProductoId == null || Cantidad == null || ProductoId.Length == 0)
            {
                ModelState.AddModelError("", "Debe seleccionar al menos un producto.");
                ViewBag.Clientes = _context.Clientes.ToList();
                ViewBag.Productos = _context.Productos.ToList();
                return View();
            }

            decimal totalVenta = 0;

            // VALIDAR STOCK
            for (int i = 0; i < ProductoId.Length; i++)
            {
                var producto = await _context.Productos.FindAsync(ProductoId[i]);

                if (producto == null)
                {
                    ModelState.AddModelError("", "Producto no encontrado.");
                    return View();
                }

                if (producto.Stock < Cantidad[i])
                {
                    ModelState.AddModelError("",
                        $"Stock insuficiente para el producto {producto.Nombre}. Disponible: {producto.Stock}");
                    ViewBag.Clientes = _context.Clientes.ToList();
                    ViewBag.Productos = _context.Productos.ToList();
                    return View();
                }
            }

            // CREAR VENTA
            var venta = new Venta
            {
                ClienteId = ClienteId,
                Fecha = DateTime.Now,
                Total = 0
            };

            _context.Ventas.Add(venta);
            await _context.SaveChangesAsync();

            // CREAR DETALLES
            for (int i = 0; i < ProductoId.Length; i++)
            {
                var producto = await _context.Productos.FindAsync(ProductoId[i]);

                decimal subtotal = producto.PrecioUnitario * Cantidad[i];
                totalVenta += subtotal;

                var detalle = new DetalleVenta
                {
                    VentaId = venta.Id,
                    ProductoId = ProductoId[i],
                    Cantidad = Cantidad[i],
                    PrecioUnitario = producto.PrecioUnitario,
                    Subtotal = subtotal
                };

                _context.DetalleVentas.Add(detalle);

                // DESCONTAR STOCK
                producto.Stock -= Cantidad[i];
                _context.Productos.Update(producto);
            }

            // ACTUALIZAR TOTAL
            venta.Total = totalVenta;
            _context.Ventas.Update(venta);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}

