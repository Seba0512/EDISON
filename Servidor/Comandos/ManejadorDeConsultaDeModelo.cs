using Common.Protocolo;
using Common.Protocolo.Vistas;
using Dominio;
using Dominio.Fachada;

namespace Servidor.Comandos;

/// <summary>
/// CMD 14: consulta de la ficha completa de un modelo (CR8). Decodifica el nombre pedido, delega en
/// <see cref="ICatalogoDeModelos.Consultar"/> (que lanza <see cref="Dominio.Excepciones.ModeloInexistenteException"/>
/// si no existe, traducida sola a ER por <see cref="DespachadorDeComandos"/>) y mapea el
/// <see cref="Modelo"/> devuelto a la vista de solo lectura que viaja por el cable, sin exponer el
/// agregado de dominio al protocolo.
/// </summary>
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
