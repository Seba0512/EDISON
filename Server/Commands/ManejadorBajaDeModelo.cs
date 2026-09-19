using Common;
using Server.Domain.Facade;

namespace Server.Commands;

internal sealed class ManejadorBajaDeModelo : ManejadorDeComando
{
    private readonly Edison _edison;
    private readonly ImagenesEnDisco _imagenes;

    public ManejadorBajaDeModelo(Edison edison, ImagenesEnDisco imagenes)
    {
        _edison = edison;
        _imagenes = imagenes;
    }

    public override int Comando => Comandos.BajaDeModelo;

    protected override RespuestaDelServidor Atender(Trama solicitud, ContextoDeConexion conexion)
    {
        string nombreArchivoImagen = _edison.EliminarModelo(
            solicitud.DatosComoTexto(), conexion.Sesion.UsuarioAutenticado);
        _imagenes.EliminarSinPropagar(
            nombreArchivoImagen, "No se pudo limpiar la imagen del modelo eliminado");
        return RespuestaDelServidor.Ok("Modelo eliminado correctamente.");
    }
}
