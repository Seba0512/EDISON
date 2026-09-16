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
        (string nombreModelo, string nombreArchivoCliente) =
            CodecDeSubidaDeImagen.DecodificarSolicitud(solicitud.Datos.Span);

        using MemoryStream buffer = new();
        solicitud.Archivos.RecibirEn(buffer);
        buffer.Position = 0;

        string nombreValidado = NombreDeArchivoSeguro.Validar(nombreArchivoCliente);
        string extension = Path.GetExtension(nombreValidado);

        Modelo modelo = _catalogoDeModelos.Consultar(nombreModelo);
        string nombreArchivoAnterior = modelo.NombreArchivoImagen;

        string nombreArchivoNuevo = $"{Guid.NewGuid():N}{extension}";
        _repositorioDeImagenes.Guardar(nombreArchivoNuevo, buffer);

        try
        {
            DatosDeModelo datosConImagen = new(
                modelo.Nombre,
                modelo.Descripcion,
                modelo.Precio,
                modelo.FechaEntregaPrometida,
                nombreArchivoNuevo);
            FichaDeModelo ficha = new(datosConImagen, modelo.StockTotal);

            _catalogoDeModelos.ModificarModelo(ficha, usuario);
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

        return RespuestaDelServidor.Ok($"Imagen subida para el modelo '{modelo.Nombre}'");
    }
}
