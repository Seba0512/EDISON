using Common.Protocolo;
using Dominio;
using Dominio.Excepciones;
using Dominio.Fachada;
using Servidor.Repositorios;

namespace Servidor.Comandos;

/// <summary>
/// CMD 16: descarga de la imagen de un modelo. No es una opcion de menu propia, sino un paso opcional
/// dentro de "Consultar modelo". Depende de <see cref="ICatalogoDeModelos"/> y de
/// <see cref="IRepositorioDeImagenes"/> (ISP, constructor injection), igual que
/// <see cref="ManejadorDeSubidaDeImagen"/>.
///
/// A diferencia de CMD 15, el cliente no manda nada binario en el REQ de CMD 16, asi que no hay
/// riesgo de desincronizar el stream si se rechaza con ER: la validacion de dominio puede ir primero
/// y, si falla, ningun byte fuera de frame llega a viajar.
///
/// El archivo viaja fuera de frame recien despues de que el RES salio por la red
/// (<see cref="RespuestaDelServidor.OkConEnvioPosterior"/>): el stream de lectura del disco se abre
/// antes de construir la respuesta, y se cierra dentro de la accion posterior una vez que
/// <see cref="ArchivosDeSesion.EnviarDesde"/> termino de usarlo, nunca antes.
/// </summary>
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
        Modelo modelo = _catalogoDeModelos.Consultar(nombreModelo);

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
