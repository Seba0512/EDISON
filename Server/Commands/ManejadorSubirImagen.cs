using Common;
using Server.Domain.Entities;
using Server.Domain.Exceptions;
using Server.Domain.Facade;

namespace Server.Commands;

// No hereda el esqueleto de ManejadorDeComando: el archivo viaja pegado a la trama y hay que
// drenarlo del stream antes de validar la sesión, o la próxima trama quedaría desincronizada.
internal sealed class ManejadorSubirImagen : IManejadorDeComando
{
    private readonly Edison _edison;
    private readonly ImagenesEnDisco _imagenes;

    public ManejadorSubirImagen(Edison edison, ImagenesEnDisco imagenes)
    {
        _edison = edison;
        _imagenes = imagenes;
    }

    public int Comando => Comandos.SubirImagen;

    public RespuestaDelServidor Ejecutar(Trama solicitud, ContextoDeConexion conexion)
    {
        using MemoryStream archivo = new();
        conexion.Archivos.RecibirEn(archivo);

        if (!conexion.Sesion.TieneSesion)
        {
            return RespuestaDelServidor.Rechazo(ManejadorDeComando.SesionRequerida);
        }

        (string nombreModelo, string nombreArchivoCliente) =
            Mensajes.ASubidaDeImagen(solicitud.DatosComoTexto());
        string nombreArchivoNuevo = Guardar(nombreArchivoCliente, archivo);
        CambioDeImagen cambio = new(nombreModelo, nombreArchivoNuevo);
        string nombreArchivoAnterior = CambiarImagen(cambio, conexion.Sesion.UsuarioAutenticado);
        _imagenes.EliminarSinPropagar(
            nombreArchivoAnterior, "No se pudo limpiar la imagen reemplazada");
        return RespuestaDelServidor.Ok("Imagen actualizada correctamente.");
    }

    // El nombre en disco es un Guid: el del cliente solo aporta la extensión.
    private string Guardar(string nombreArchivoCliente, MemoryStream archivo)
    {
        string extension = Path.GetExtension(ImagenesEnDisco.ValidarNombre(nombreArchivoCliente));
        string nombreArchivoNuevo = $"{Guid.NewGuid():N}{extension}";
        archivo.Position = 0;
        _imagenes.Guardar(nombreArchivoNuevo, archivo);
        return nombreArchivoNuevo;
    }

    private string CambiarImagen(CambioDeImagen cambio, Usuario usuario)
    {
        try
        {
            return _edison.CambiarImagenDeModelo(cambio, usuario);
        }
        catch (DominioException)
        {
            // Si el dominio rechaza el cambio, la imagen recién guardada no debe quedar huérfana.
            _imagenes.EliminarSinPropagar(
                cambio.NuevoNombreArchivoImagen,
                "No se pudo compensar la imagen nueva tras rechazarse el cambio");
            throw;
        }
    }
}
