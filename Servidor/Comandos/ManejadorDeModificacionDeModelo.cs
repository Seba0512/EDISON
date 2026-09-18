using Common.Protocolo;
using Common.Protocolo.Vistas;
using Dominio;
using Dominio.Fachada;

namespace Servidor.Comandos;

internal sealed class ManejadorDeModificacionDeModelo : ManejadorAutenticado
{
    private readonly ICatalogoDeModelos _catalogoDeModelos;

    public ManejadorDeModificacionDeModelo(ICatalogoDeModelos catalogoDeModelos)
    {
        _catalogoDeModelos = catalogoDeModelos;
    }

    protected override RespuestaDelServidor ResponderAutenticado(SolicitudRecibida solicitud, Usuario usuario)
    {
        DatosDeAltaDeModelo datos = CodecDeDatosDeModelo.Decodificar(solicitud.Datos.Span);

        _catalogoDeModelos.ModificarModelo(FichaRecibida.Desde(datos), usuario);

        return RespuestaDelServidor.Ok("Modelo modificado correctamente.");
    }
}
