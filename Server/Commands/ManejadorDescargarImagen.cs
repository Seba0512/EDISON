using Common;
using Server.Domain.Entities;
using Server.Domain.Facade;

namespace Server.Commands;

internal sealed class ManejadorDescargarImagen : ManejadorDeComando
{
    private readonly Edison _edison;
    private readonly ImagenesEnDisco _imagenes;

    public ManejadorDescargarImagen(Edison edison, ImagenesEnDisco imagenes)
    {
        _edison = edison;
        _imagenes = imagenes;
    }

    public override int Comando => Comandos.DescargarImagen;

    protected override RespuestaDelServidor Atender(Trama solicitud, ContextoDeConexion conexion)
    {
        LecturaDeModelo modelo = _edison.ConsultarLectura(solicitud.DatosComoTexto());
        if (!modelo.TieneImagen)
        {
            return RespuestaDelServidor.Rechazo($"El modelo {modelo.Nombre} no tiene imagen");
        }

        Stream origen = _imagenes.AbrirLectura(modelo.NombreArchivoImagen);
        try
        {
            return ResponderConEnvio(modelo, origen, conexion.Archivos);
        }
        catch
        {
            origen.Dispose();
            throw;
        }
    }

    private RespuestaDelServidor ResponderConEnvio(
        LecturaDeModelo modelo, Stream origen, ArchivosDeSesion archivos)
    {
        long tamanio = origen.Length;
        if (tamanio > _imagenes.MaximoDeBytes)
        {
            origen.Dispose();
            return RespuestaDelServidor.Rechazo(
                $"La imagen supera el tamaño máximo de transferencia ({_imagenes.MaximoDeBytes} bytes)");
        }

        string nombreSugerido = $"{modelo.Nombre}{Path.GetExtension(modelo.NombreArchivoImagen)}";
        // La respuesta posee el stream: lo libera aunque el envío posterior nunca llegue a correr.
        return RespuestaDelServidor.OkConEnvioPosterior(
            nombreSugerido,
            _ => archivos.EnviarDesde(origen, tamanio),
            origen);
    }
}
