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
        DatosDeModelo datosDeModelo = new(
            datos.Nombre, datos.Descripcion, datos.Precio, datos.FechaEntregaPrometida, string.Empty);
        FichaDeModelo ficha = new(datosDeModelo, datos.Stock);

        _catalogoDeModelos.AltaDeModelo(ficha, usuario);

        return RespuestaDelServidor.Ok($"Modelo '{datos.Nombre}' creado");
    }
}
