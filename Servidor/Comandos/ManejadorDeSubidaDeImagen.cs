using Common.Protocolo;
using Dominio;
using Dominio.Excepciones;
using Dominio.Fachada;
using Servidor.Repositorios;

namespace Servidor.Comandos;

/// <summary>
/// CMD 15: subida de la imagen de un modelo. No es una opcion de menu propia, sino un paso opcional
/// dentro de "Alta de modelo" y "Modificar modelo". Depende de <see cref="ICatalogoDeModelos"/> para
/// las reglas de negocio y de <see cref="IRepositorioDeImagenes"/> para el disco: dos interfaces
/// chicas en vez de recibir <c>Edison</c> completo (ISP, constructor injection).
///
/// <para>
/// Punto critico de sincronizacion del stream: el cliente manda el archivo completo (Int64 + bytes)
/// inmediatamente despues del REQ, sin esperar ninguna respuesta intermedia
/// (<c>Cliente.ConexionConServidor.EnviarSolicitudConArchivo</c>). Por eso este manejador SIEMPRE
/// recibe el archivo completo hacia un <see cref="MemoryStream"/> como primerisimo paso, antes de
/// cualquier validacion de dominio: si devolviera un ER sin haber leido esos bytes, quedarian
/// interpretados como el inicio de la proxima trama y la conexion entera quedaria rota para todo lo
/// que venga despues, no solo para este comando. Recibir en memoria es aceptable porque el archivo
/// pesa a lo sumo <c>maxFileBytes</c> (10 MB por defecto).
/// </para>
///
/// <para>
/// Supuesto de diseno aceptado: como esta operacion arma una <see cref="FichaDeModelo"/> con el
/// nuevo nombre de archivo y la aplica via <see cref="ICatalogoDeModelos.ModificarModelo"/>, hereda
/// todas sus reglas (CR3): solo el creador del modelo puede subir imagen, y un modelo agotado no
/// admite subir imagen porque la subida no cambia el stock (no cae en la clausula que permite
/// modificar un modelo sin unidades disponibles cuando el stock nuevo aumenta). No es un bug a
/// corregir: es la consecuencia esperada de reusar <c>ModificarModelo</c> en lugar de duplicar sus
/// validaciones en un camino aparte.
/// </para>
/// </summary>
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

        return RespuestaDelServidor.Ok($"Imagen subida para el modelo '{modelo.Nombre}'");
    }
}
