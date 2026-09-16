using Common.Protocolo;
using Common.Protocolo.Vistas;
using Dominio;
using Dominio.Fachada;

namespace Servidor.Comandos;

/// <summary>CMD 10: alta de modelo (CR2). Exige sesión iniciada vía <see cref="ManejadorAutenticado"/>.</summary>
internal sealed class ManejadorDeAltaDeModelo : ManejadorAutenticado
{
    private readonly ICatalogoDeModelos _catalogoDeModelos;

    public ManejadorDeAltaDeModelo(ICatalogoDeModelos catalogoDeModelos)
    {
        _catalogoDeModelos = catalogoDeModelos;
    }

    protected override RespuestaDelServidor ResponderAutenticado(SolicitudRecibida solicitud, Usuario usuario)
    {
        DatosDeAltaDeModelo datos = CodecDeAltaDeModelo.Decodificar(solicitud.Datos.Span);
        DatosDeModelo datosDeModelo = new(
            datos.Nombre, datos.Descripcion, datos.Precio, datos.FechaEntregaPrometida, string.Empty);
        FichaDeModelo ficha = new(datosDeModelo, datos.Stock);

        _catalogoDeModelos.AltaDeModelo(ficha, usuario);

        return RespuestaDelServidor.Ok($"Modelo '{datos.Nombre}' creado");
    }
}
