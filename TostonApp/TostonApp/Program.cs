using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using TostonApp.Data;
using TostonApp.Models.Identity;
using TostonApp.Security;
using TostonApp.Services.Email;

var builder = WebApplication.CreateBuilder(args);

// ===========================
// MVC + Razor Pages (Identity UI)
// ===========================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ===========================
// Cache
// ===========================
builder.Services.AddMemoryCache();

// ===========================
// Email sender (NECESARIO para Register)
// ===========================
builder.Services.AddSingleton<IEmailSender, Microsoft.AspNetCore.Identity.UI.Services.NoOpEmailSender>();

// ===========================
// DB (EF Core + SQL Server)
// ===========================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No existe ConnectionStrings:DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sql =>
    {
        sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        );
    });

#if DEBUG
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
#endif
});

// ===========================
// Identity (con Roles)
// ===========================
builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        // Password
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        // Lockout
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);

        // User
        options.User.RequireUniqueEmail = true;

        // SignIn
        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// ===========================
// Cookies
// ===========================
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "TostonApp.Auth";
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// ===========================
// Authorization avanzado (Permisos)
// ===========================

// Provider dinámico: PERMISO:CODIGO
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermisosPolicyProvider>();

// Handler de permisos
builder.Services.AddScoped<IAuthorizationHandler, PermisoAuthorizationHandler>();

// Helper para vistas y controllers
builder.Services.AddScoped<IPermisosService, PermisosService>();

// ===========================
// Build
// ===========================
var app = builder.Build();

// ===========================
// Pipeline
// ===========================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ===========================
// Seed (roles, permisos, admin, datos demo)
// ===========================
await SeedData.EnsureSeedAsync(app.Services);

// ===========================
// Endpoints
// ===========================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
