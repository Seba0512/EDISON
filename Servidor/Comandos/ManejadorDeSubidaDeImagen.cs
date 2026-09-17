using Common.Protocolo;
using Dominio;
using Dominio.Excepciones;
using Dominio.Fachada;
using Servidor.Repositorios;
using ComandosDelProtocolo = Common.Protocolo.Comandos;

namespace Servidor.Comandos;

internal sealed class ManejadorDeSubidaDeImagen : IManejadorDeComando
{
    private readonly ICatalogoDeModelos _catalogoDeModelos;
    private readonly IRepositorioDeImagenes _repositorioDeImagenes;

    public ManejadorDeSubidaDeImagen(
        ICatalogoDeModelos catalogoDeModelos, IRepositorioDeImagenes repositorioDeImagenes)
    {
        _catalogoDeModelos = catalogoDeModelos;
        _repositorioDeImagenes = repositorioDeImagenes;
    }

    // No hereda de ManejadorAutenticado porque el archivo tiene que drenarse del stream antes
    // de mirar la sesión: si no, el stream queda desincronizado para la próxima trama.
    public RespuestaDelServidor Responder(SolicitudRecibida solicitud)
    {
        using MemoryStream archivo = new();
        solicitud.Archivos.RecibirEn(archivo);

        if (!solicitud.Sesion.TieneSesion)
            return RespuestaDelServidor.Rechazo(ManejadorAutenticado.SesionRequerida);

        (string nombreModelo, string nombreArchivoCliente) =
            CodecDeSubidaDeImagen.DecodificarSolicitud(solicitud.Datos.Span);

        Usuario usuario = solicitud.Sesion.UsuarioAutenticado;
        archivo.Position = 0;
        string referenciaNueva = Guardar(usuario, nombreArchivoCliente, archivo);
        ReemplazarImagen(nombreModelo, referenciaNueva, usuario);

        return RespuestaDelServidor.Ok($"Imagen subida para el modelo '{nombreModelo}'");
    }

    private string Guardar(Usuario usuario, string nombreArchivoCliente, Stream contenido)
    {
        string extension = Path.GetExtension(NombreDeArchivoSeguro.Validar(nombreArchivoCliente));
        string nombreArchivoNuevo = $"{Guid.NewGuid():N}{extension}";

        return _repositorioDeImagenes.Guardar(usuario.NombreUsuario, nombreArchivoNuevo, contenido);
    }

    private void ReemplazarImagen(string nombreModelo, string referenciaNueva, Usuario usuario)
    {
        string nombreArchivoAnterior = CambiarImagen(nombreModelo, referenciaNueva, usuario);
        if (nombreArchivoAnterior.Length > 0)
            IntentarEliminar(
                nombreArchivoAnterior,
                "No se pudo limpiar la imagen reemplazada después del commit");
    }

    private string CambiarImagen(string nombreModelo, string referenciaNueva, Usuario usuario)
    {
        try
        {
            return _catalogoDeModelos.CambiarImagenDeModelo(nombreModelo, referenciaNueva, usuario);
        }
        catch (DominioException)
        {
            IntentarEliminar(
                referenciaNueva,
                "No se pudo compensar la imagen nueva tras rechazarse el cambio");
            throw;
        }
    }

    private void IntentarEliminar(string nombreArchivo, string mensaje)
    {
        try
        {
            _repositorioDeImagenes.Eliminar(nombreArchivo);
        }
        catch (ErrorDeAlmacenamientoDeImagenException exception)
        {
            DiagnosticoDeLimpieza.Informar(
                $"[CMD {ComandosDelProtocolo.SubirImagen:D2}] {mensaje} '{nombreArchivo}': {exception}");
        }
    }
}
