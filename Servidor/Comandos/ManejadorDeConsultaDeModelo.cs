using Common.Protocolo;
using Common.Protocolo.Vistas;
using Dominio;
using Dominio.Fachada;

namespace Servidor.Comandos;

internal sealed class ManejadorDeConsultaDeModelo : ManejadorAutenticado
{
    private readonly ICatalogoDeModelos _catalogoDeModelos;

    public ManejadorDeConsultaDeModelo(ICatalogoDeModelos catalogoDeModelos)
    {
        _catalogoDeModelos = catalogoDeModelos;
    }

    protected override RespuestaDelServidor ResponderAutenticado(SolicitudRecibida solicitud, Usuario usuario)
    {
        string nombreModelo = CodecDeConsultaDeModelo.DecodificarSolicitud(solicitud.Datos.Span);
        Modelo modelo = _catalogoDeModelos.Consultar(nombreModelo);
        DetalleDeModelo detalle = MapearAVista(modelo);
        return RespuestaDelServidor.Ok(CodecDeConsultaDeModelo.CodificarResultado(detalle));
    }

    private static DetalleDeModelo MapearAVista(Modelo modelo)
        => new(
            modelo.Nombre,
            modelo.Descripcion,
            modelo.Precio,
            modelo.FechaEntregaPrometida,
            modelo.UnidadesDisponibles,
            modelo.UnidadesReservadas,
            modelo.NombreArchivoImagen);
}
