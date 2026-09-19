namespace Server.Commands;

// Lo que un handler necesita de la conexión que lo invoca: quién está autenticado y por dónde
// viajan los archivos. Vive lo mismo que la conexión.
internal sealed class ContextoDeConexion
{
    public ContextoDeConexion(EstadoDeSesion sesion, ArchivosDeSesion archivos)
    {
        Sesion = sesion;
        Archivos = archivos;
    }

    public EstadoDeSesion Sesion { get; }

    public ArchivosDeSesion Archivos { get; }
}
