namespace TostonApp.Models.Dominio
{
    public class Producto
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public decimal Precio { get; set; }

        // Controlado por permisos
        public bool VisibleParaClientes { get; set; }

        public bool Activo { get; set; } = true;
    }
}
