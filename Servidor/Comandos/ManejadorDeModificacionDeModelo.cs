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
        // El pedido de modificación no trae imagen, así que se consulta el modelo actual para conservarla.
        Modelo modeloVigente = _catalogoDeModelos.Consultar(datos.Nombre);
        DatosDeModelo datosDeModelo = new(
            datos.Nombre,
            datos.Descripcion,
            datos.Precio,
            datos.FechaEntregaPrometida,
            modeloVigente.NombreArchivoImagen);
        FichaDeModelo ficha = new(datosDeModelo, datos.Stock);

        _catalogoDeModelos.ModificarModelo(ficha, usuario);

        return RespuestaDelServidor.Ok($"Modelo '{datos.Nombre}' modificado");
    }
}
