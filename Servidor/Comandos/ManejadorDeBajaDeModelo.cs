using Common.Protocolo;
using Dominio;
using Dominio.Fachada;

namespace Servidor.Comandos;

internal sealed class ManejadorDeBajaDeModelo : ManejadorAutenticado
{
    private readonly ICatalogoDeModelos _catalogoDeModelos;

    public ManejadorDeBajaDeModelo(ICatalogoDeModelos catalogoDeModelos)
    {
        _catalogoDeModelos = catalogoDeModelos;
    }

    protected override RespuestaDelServidor ResponderAutenticado(SolicitudRecibida solicitud, Usuario usuario)
    {
        string nombreModelo = CodecDeBajaDeModelo.Decodificar(solicitud.Datos.Span);
        _catalogoDeModelos.EliminarModelo(nombreModelo, usuario);
        return RespuestaDelServidor.Ok($"Modelo '{nombreModelo}' eliminado");
    }
}
