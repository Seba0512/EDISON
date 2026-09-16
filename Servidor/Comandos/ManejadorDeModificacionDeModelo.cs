using Common.Protocolo;
using Common.Protocolo.Vistas;
using Dominio;
using Dominio.Fachada;

namespace Servidor.Comandos;

/// <summary>
/// CMD 11: modificacion de modelo (CR3). Exige sesion iniciada via <see cref="ManejadorAutenticado"/>.
/// El payload es identico al de CMD 10 (la ficha completa), pero a diferencia del alta, la
/// modificacion no debe pisar la imagen ya subida del modelo: como el protocolo no manda un nombre
/// de archivo de imagen en CMD 11, se consulta el modelo vigente con
/// <see cref="ICatalogoDeModelos.Consultar"/> antes de armar la ficha, para preservar su
/// <see cref="Modelo.NombreArchivoImagen"/> en vez de reemplazarlo por una cadena vacia. Si el
/// modelo no existe, <c>Consultar</c> lanza <see cref="Dominio.Excepciones.ModeloInexistenteException"/>,
/// que <see cref="DespachadorDeComandos"/> traduce sola a ER.
/// </summary>
internal sealed class ManejadorDeModificacionDeModelo : ManejadorAutenticado
{
    private readonly ICatalogoDeModelos _catalogoDeModelos;

    public ManejadorDeModificacionDeModelo(ICatalogoDeModelos catalogoDeModelos)
    {
        _catalogoDeModelos = catalogoDeModelos;
    }

    protected override RespuestaDelServidor ResponderAutenticado(SolicitudRecibida solicitud, Usuario usuario)
    {
        DatosDeAltaDeModelo datos = CodecDeModificacionDeModelo.Decodificar(solicitud.Datos.Span);
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
