using Dominio.Excepciones;

namespace Dominio;

/// <summary>
/// Datos descriptivos de un modelo del catalogo. Es un objeto parametro inmutable: evita que el
/// constructor de <see cref="Modelo"/> reciba cinco argumentos sueltos y se reutiliza tal cual en la
/// modificacion (CR3), que altera exactamente estos campos.
/// No incluye el stock: las cantidades viven en las unidades, no en estos datos.
/// </summary>
public class DatosDeModelo
{
    private const int LargoMinimoNombre = 2;
    private const int LargoMaximoNombre = 60;
    private const int LargoMaximoDescripcion = 500;

    public string Nombre { get; }

    public string Descripcion { get; }

    public decimal Precio { get; }

    public DateTime FechaEntregaPrometida { get; }

    /// <summary>Cadena vacia cuando el modelo no tiene imagen: la imagen es opcional (CR2).</summary>
    public string NombreArchivoImagen { get; }

    public DatosDeModelo(
        string nombre,
        string descripcion,
        decimal precio,
        DateTime fechaEntregaPrometida,
        string nombreArchivoImagen)
    {
        Nombre = NombreValidado(nombre);
        Descripcion = DescripcionValidada(descripcion);
        Precio = PrecioValidado(precio);
        FechaEntregaPrometida = fechaEntregaPrometida;
        NombreArchivoImagen = nombreArchivoImagen?.Trim() ?? string.Empty;
    }

    public bool TieneImagen => NombreArchivoImagen.Length > 0;

    public bool DescribeAlMismoModeloQue(DatosDeModelo otrosDatos)
    {
        return Nombre.Equals(otrosDatos.Nombre, StringComparison.OrdinalIgnoreCase);
    }

    private static string NombreValidado(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DominioException("El nombre del modelo no puede estar vacio");
        }

        string nombreSinEspacios = nombre.Trim();
        if (nombreSinEspacios.Length is < LargoMinimoNombre or > LargoMaximoNombre)
        {
            throw new DominioException(
                $"El nombre del modelo debe tener entre {LargoMinimoNombre} y {LargoMaximoNombre} caracteres");
        }

        return nombreSinEspacios;
    }

    private static string DescripcionValidada(string descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new DominioException("La descripcion del modelo no puede estar vacia");
        }

        string descripcionSinEspacios = descripcion.Trim();
        if (descripcionSinEspacios.Length > LargoMaximoDescripcion)
        {
            throw new DominioException(
                $"La descripcion no puede superar los {LargoMaximoDescripcion} caracteres");
        }

        return descripcionSinEspacios;
    }

    private static decimal PrecioValidado(decimal precio)
    {
        if (precio <= 0)
        {
            throw new DominioException("El precio debe ser mayor a cero");
        }

        return precio;
    }
}
