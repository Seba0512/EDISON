namespace Servidor.Dominio.Servicios;

// Puerto para quitar la imagen de un modelo en dos pasos, coordinados con la baja en la base. La
// imagen primero se aparta (deja de estar disponible pero no se pierde) y, según cómo termine la
// transacción, se descarta o se restaura. El dominio no sabe dónde se guardan las imágenes.
public interface IImagenesDeModelos
{
    // Si no se puede apartar, lanza y la baja no ocurre: un modelo no se borra dejando su imagen (SR3).
    void Apartar(string nombreArchivo);

    // La baja quedó confirmada en la base: la imagen apartada se borra. Una falla solo se registra.
    void Descartar(string nombreArchivo);

    // La baja falló: la imagen vuelve a su lugar y el modelo sigue completo. Una falla solo se registra.
    void Restaurar(string nombreArchivo);
}
