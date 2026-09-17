using Common.Protocolo.Vistas;

namespace Cliente.Vistas;

internal static class PresentadorDeDetalleDeModelo
{
    public static void Mostrar(DetalleDeModelo detalle)
    {
        Consola.Titulo("Detalle del modelo");
        Consola.Campo("Nombre", detalle.Nombre);
        Consola.Campo(
            "Descripción",
            detalle.Descripcion.Length > 0 ? detalle.Descripcion : "(sin descripción)");
        Consola.Campo("Precio", detalle.Precio.ToString());
        Consola.Campo("Fecha de entrega prometida", detalle.FechaEntregaPrometida.ToString("yyyy-MM-dd"));
        Consola.Campo("Unidades disponibles", detalle.UnidadesDisponibles.ToString());
        Consola.Campo("Unidades reservadas", detalle.UnidadesReservadas.ToString());
        Consola.Campo(
            "Imagen",
            detalle.NombreArchivoImagen.Length > 0 ? detalle.NombreArchivoImagen : "(sin imagen)");
    }
}
