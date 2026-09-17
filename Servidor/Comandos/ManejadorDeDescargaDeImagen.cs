using Common.Protocolo;
using Dominio;
using Dominio.Excepciones;
using Dominio.Fachada;
using Servidor.Repositorios;

namespace Servidor.Comandos;

internal sealed class ManejadorDeDescargaDeImagen : ManejadorAutenticado
{
    private readonly ICatalogoDeModelos _catalogoDeModelos;
    private readonly IRepositorioDeImagenes _repositorioDeImagenes;

    public ManejadorDeDescargaDeImagen(
        ICatalogoDeModelos catalogoDeModelos, IRepositorioDeImagenes repositorioDeImagenes)
    {
        _catalogoDeModelos = catalogoDeModelos;
        _repositorioDeImagenes = repositorioDeImagenes;
    }

    protected override RespuestaDelServidor ResponderAutenticado(SolicitudRecibida solicitud, Usuario usuario)
    {
        string nombreModelo = CodecDeDescargaDeImagen.DecodificarSolicitud(solicitud.Datos.Span);
        LecturaDeModelo modelo = _catalogoDeModelos.ConsultarLectura(nombreModelo);

        if (!modelo.TieneImagen)
            throw new DominioException($"El modelo {modelo.Nombre} no tiene imagen");

        string nombreArchivo = modelo.NombreArchivoImagen;
        string nombreSugerido = $"{modelo.Nombre}{Path.GetExtension(nombreArchivo)}";
        byte[] cuerpo = CodecDeDescargaDeImagen.CodificarRespuesta(nombreSugerido);
        Stream origen = _repositorioDeImagenes.AbrirLectura(nombreArchivo);

        try
        {
            long tamanio = solicitud.Archivos.ObtenerTamanioAdmitido(origen);
            return RespuestaDelServidor.OkConEnvioPosterior(
                cuerpo,
                _ => solicitud.Archivos.EnviarDesde(origen, tamanio),
                origen);
        }
        catch
        {
            origen.Dispose();
            throw;
        }
    }
}
