using Common.Protocolo;
using Dominio;
using Dominio.Excepciones;
using Dominio.Fachada;
using Servidor.Repositorios;

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

    public RespuestaDelServidor Responder(SolicitudRecibida solicitud)
    {
        // El archivo se drena del stream antes de decodificar DATOS: si DATOS viene mal formado,
        // o falta una sesión autenticada, el stream queda sincronizado para la próxima trama.
        using MemoryStream buffer = new();
        solicitud.Archivos.RecibirEn(buffer);

        if (!solicitud.Sesion.TieneSesion)
            return RespuestaDelServidor.Rechazo("Debe iniciar sesión para realizar esta operación");

        Usuario usuario = solicitud.Sesion.UsuarioAutenticado;
        (string nombreModelo, string nombreArchivoCliente) =
            CodecDeSubidaDeImagen.DecodificarSolicitud(solicitud.Datos.Span);

        buffer.Position = 0;
        string nombreValidado = NombreDeArchivoSeguro.Validar(nombreArchivoCliente);
        string extension = Path.GetExtension(nombreValidado);

        string nombreArchivoNuevo = $"{Guid.NewGuid():N}{extension}";
        string referenciaNueva = _repositorioDeImagenes.Guardar(
            usuario.NombreUsuario, nombreArchivoNuevo, buffer);

        string nombreArchivoAnterior = CambiarImagen(
            nombreModelo, referenciaNueva, usuario);

        if (nombreArchivoAnterior.Length > 0)
            IntentarEliminar(
                nombreArchivoAnterior,
                "No se pudo limpiar la imagen reemplazada después del commit");

        return RespuestaDelServidor.Ok($"Imagen subida para el modelo '{nombreModelo}'");
    }

    private string CambiarImagen(
        string nombreModelo, string referenciaNueva, Usuario usuario)
    {
        try
        {
            return _catalogoDeModelos.CambiarImagenDeModelo(
                nombreModelo, referenciaNueva, usuario);
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
            InformarFallaDeCleanup($"[CMD 15] {mensaje} '{nombreArchivo}': {exception}");
        }
    }

    private static void InformarFallaDeCleanup(string mensaje)
    {
        try
        {
            Console.Error.WriteLine(mensaje);
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException)
        {
            // Una salida de diagnóstico no disponible no puede cambiar un commit ya realizado.
        }
    }
}
