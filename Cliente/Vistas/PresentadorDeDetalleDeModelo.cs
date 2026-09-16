using Common.Protocolo.Vistas;

namespace Cliente.Vistas;

/// <summary>
/// Presentación en consola de la ficha de CR8 (CMD 14). Sólo formatea texto: no sabe nada de red ni
/// de protocolo. Si el modelo tiene imagen muestra el nombre de archivo igual, aunque esta rama
/// todavía no pueda descargarla (CMD 16, rama de imágenes).
/// </summary>
internal static class PresentadorDeDetalleDeModelo
{
    public static void Mostrar(DetalleDeModelo detalle)
    {
        Console.WriteLine($"Nombre: {detalle.Nombre}");
        Console.WriteLine($"Descripción: {detalle.Descripcion}");
        Console.WriteLine($"Precio: {detalle.Precio}");
        Console.WriteLine($"Fecha de entrega prometida: {detalle.FechaEntregaPrometida:yyyy-MM-dd}");
        Console.WriteLine($"Unidades disponibles: {detalle.UnidadesDisponibles}");
        Console.WriteLine($"Unidades reservadas: {detalle.UnidadesReservadas}");
        Console.WriteLine(
            detalle.NombreArchivoImagen.Length > 0
                ? $"Imagen: {detalle.NombreArchivoImagen}"
                : "Imagen: (sin imagen)");
    }
}
