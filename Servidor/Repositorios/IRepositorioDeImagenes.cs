namespace Servidor.Repositorios;

internal interface IRepositorioDeImagenes
{
    void Guardar(string nombreArchivo, Stream contenido);

    Stream AbrirLectura(string nombreArchivo);

    bool Existe(string nombreArchivo);

    long TamanioDe(string nombreArchivo);

    void Eliminar(string nombreArchivo);
}
