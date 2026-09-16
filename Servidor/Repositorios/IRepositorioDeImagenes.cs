namespace Servidor.Repositorios;

/// <summary>
/// Acceso a las imágenes de modelos en el servidor (CMD 15/16). Se declara acá y no en el dominio
/// porque el dominio sólo necesita eliminar (<see cref="Dominio.IAlmacenDeImagenes"/>): leer, guardar
/// y consultar tamaño son necesidades de infraestructura para servir el archivo por red, no reglas de
/// negocio.
/// </summary>
internal interface IRepositorioDeImagenes
{
    /// <summary>Guarda el contenido con el nombre dado, sobrescribiendo si ya existía.</summary>
    void Guardar(string nombreArchivo, Stream contenido);

    Stream AbrirLectura(string nombreArchivo);

    bool Existe(string nombreArchivo);

    long TamanioDe(string nombreArchivo);

    /// <summary>
    /// Deshace una subida fallida (CMD 15: la ficha se rechazo despues de guardar el archivo). Mismo
    /// contrato que <see cref="Dominio.IAlmacenDeImagenes.Eliminar"/> (no falla si el archivo no
    /// esta): <see cref="AlmacenDeImagenesEnDisco"/> implementa un unico metodo que satisface ambas
    /// interfaces, para no duplicar la logica de borrado.
    /// </summary>
    void Eliminar(string nombreArchivo);
}
