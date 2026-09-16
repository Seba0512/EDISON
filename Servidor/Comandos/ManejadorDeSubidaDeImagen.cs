using Common.Protocolo;
using Dominio;
using Dominio.Excepciones;
using Dominio.Fachada;
using Servidor.Repositorios;

namespace Servidor.Comandos;

internal sealed class ManejadorDeSubidaDeImagen : ManejadorAutenticado
{
    private readonly ICatalogoDeModelos _catalogoDeModelos;
    private readonly IRepositorioDeImagenes _repositorioDeImagenes;

    public ManejadorDeSubidaDeImagen(
        ICatalogoDeModelos catalogoDeModelos, IRepositorioDeImagenes repositorioDeImagenes)
    {
        _catalogoDeModelos = catalogoDeModelos;
        _repositorioDeImagenes = repositorioDeImagenes;
    }

    protected override RespuestaDelServidor ResponderAutenticado(SolicitudRecibida solicitud, Usuario usuario)
    {
        // El archivo se drena del stream antes de decodificar DATOS: si DATOS viene mal formado,
        // el stream queda igual sincronizado para la próxima trama.
        using MemoryStream buffer = new();
        solicitud.Archivos.RecibirEn(buffer);

        (string nombreModelo, string nombreArchivoCliente) =
            CodecDeSubidaDeImagen.DecodificarSolicitud(solicitud.Datos.Span);

        buffer.Position = 0;
        string nombreValidado = NombreDeArchivoSeguro.Validar(nombreArchivoCliente);
        string extension = Path.GetExtension(nombreValidado);

        string nombreArchivoNuevo = $"{Guid.NewGuid():N}{extension}";
        _repositorioDeImagenes.Guardar(nombreArchivoNuevo, buffer);

        string nombreArchivoAnterior;
        try
        {
            nombreArchivoAnterior =
                _catalogoDeModelos.CambiarImagenDeModelo(nombreModelo, nombreArchivoNuevo, usuario);
        }
        catch (DominioException)
        {
            _repositorioDeImagenes.Eliminar(nombreArchivoNuevo);
            throw;
        }

        if (nombreArchivoAnterior.Length > 0)
        {
            _repositorioDeImagenes.Eliminar(nombreArchivoAnterior);
        }

        return RespuestaDelServidor.Ok($"Imagen subida para el modelo '{nombreModelo}'");
    }
}
