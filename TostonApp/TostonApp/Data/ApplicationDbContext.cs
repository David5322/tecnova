using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TostonApp.Models.Dominio;
using TostonApp.Models.Identity;
using TostonApp.Models.Seguridad;

namespace TostonApp.Data
{
    // IdentityDbContext<User, Role, Key>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ========== DOMINIO ==========
        public DbSet<Producto> Productos => Set<Producto>();

        // ========== SEGURIDAD ==========
        public DbSet<Permiso> Permisos => Set<Permiso>();
        public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();
        public DbSet<UsuarioPermiso> UsuarioPermisos => Set<UsuarioPermiso>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ============================
            // TABLAS Identity (nombres opcionales)
            // ============================
            builder.Entity<ApplicationUser>(b =>
            {
                b.ToTable("Usuarios");
                b.Property(x => x.NombreCompleto).HasMaxLength(120);
            });

            builder.Entity<ApplicationRole>(b =>
            {
                b.ToTable("Roles");
                b.Property(x => x.Descripcion).HasMaxLength(200);
            });

            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>(b =>
            {
                b.ToTable("UsuariosRoles");
            });

            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>(b => b.ToTable("UsuariosClaims"));
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>(b => b.ToTable("UsuariosLogins"));
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>(b => b.ToTable("RolesClaims"));
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>(b => b.ToTable("UsuariosTokens"));

            // ============================
            // Permiso
            // ============================
            builder.Entity<Permiso>(b =>
            {
                b.ToTable("Permisos");
                b.HasKey(x => x.Id);

                b.Property(x => x.Codigo)
                    .HasMaxLength(80)
                    .IsRequired();

                b.HasIndex(x => x.Codigo).IsUnique();

                b.Property(x => x.Descripcion)
                    .HasMaxLength(200)
                    .IsRequired();
            });

            // ============================
            // RolPermiso (Many-to-Many)
            // ============================
            builder.Entity<RolPermiso>(b =>
            {
                b.ToTable("RolesPermisos");
                b.HasKey(x => new { x.RolId, x.PermisoId });

                b.HasOne(x => x.Rol)
                    .WithMany(r => r.RolPermisos)
                    .HasForeignKey(x => x.RolId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.Permiso)
                    .WithMany(p => p.RolPermisos)
                    .HasForeignKey(x => x.PermisoId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(x => x.RolId);
                b.HasIndex(x => x.PermisoId);
            });

            // ============================
            // UsuarioPermiso (override)
            // ============================
            builder.Entity<UsuarioPermiso>(b =>
            {
                b.ToTable("UsuariosPermisos");
                b.HasKey(x => new { x.UsuarioId, x.PermisoId });

                b.Property(x => x.Permitido)
                    .IsRequired();

                b.HasOne(x => x.Usuario)
                    .WithMany(u => u.UsuarioPermisos)
                    .HasForeignKey(x => x.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.Permiso)
                    .WithMany(p => p.UsuarioPermisos)
                    .HasForeignKey(x => x.PermisoId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(x => x.UsuarioId);
                b.HasIndex(x => x.PermisoId);
            });

            // ============================
            // Producto
            // ============================
            builder.Entity<Producto>(b =>
            {
                b.ToTable("Productos");
                b.HasKey(x => x.Id);

                b.Property(x => x.Nombre)
                    .HasMaxLength(120)
                    .IsRequired();

                b.Property(x => x.Descripcion)
                    .HasMaxLength(300);

                b.Property(x => x.Precio)
                    .HasPrecision(18, 2);

                b.HasIndex(x => x.Activo);
                b.HasIndex(x => x.VisibleParaClientes);
            });
        }
    }
}
