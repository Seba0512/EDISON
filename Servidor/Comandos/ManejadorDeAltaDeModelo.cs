using Common.Protocolo;
using Common.Protocolo.Vistas;
using Dominio;
using Dominio.Fachada;

namespace Servidor.Comandos;

internal sealed class ManejadorDeAltaDeModelo : ManejadorAutenticado
{
    private readonly ICatalogoDeModelos _catalogoDeModelos;

    public ManejadorDeAltaDeModelo(ICatalogoDeModelos catalogoDeModelos)
    {
        _catalogoDeModelos = catalogoDeModelos;
    }

    protected override RespuestaDelServidor ResponderAutenticado(SolicitudRecibida solicitud, Usuario usuario)
    {
        DatosDeAltaDeModelo datos = CodecDeDatosDeModelo.Decodificar(solicitud.Datos.Span);

        _catalogoDeModelos.AltaDeModelo(FichaRecibida.Desde(datos), usuario);

        return RespuestaDelServidor.Ok("Modelo creado correctamente.");
    }
}
