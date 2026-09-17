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
        Stream origen = _repositorioDeImagenes.AbrirLectura(nombreArchivo);
        long tamanio = _repositorioDeImagenes.TamanioDe(nombreArchivo);
        string nombreSugerido = $"{modelo.Nombre}{Path.GetExtension(nombreArchivo)}";
        byte[] cuerpo = CodecDeDescargaDeImagen.CodificarRespuesta(nombreSugerido);

        return RespuestaDelServidor.OkConEnvioPosterior(cuerpo, _ =>
        {
            using (origen)
            {
                solicitud.Archivos.EnviarDesde(origen, tamanio);
            }
        });
    }
}
