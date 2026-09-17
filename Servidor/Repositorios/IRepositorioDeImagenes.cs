namespace Servidor.Repositorios;

internal interface IRepositorioDeImagenes
{
    string Guardar(string nombreUsuarioPropietario, string nombreArchivo, Stream contenido);

    Stream AbrirLectura(string nombreArchivo);

    bool Existe(string nombreArchivo);

    void Eliminar(string nombreArchivo);
}
