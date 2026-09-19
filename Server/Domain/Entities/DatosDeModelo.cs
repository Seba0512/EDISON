using Server.Domain.Exceptions;

namespace Server.Domain.Entities;

public class DatosDeModelo
{
    private const int LargoMinimoNombre = 2;
    private const int LargoMaximoNombre = 60;
    private const int LargoMaximoDescripcion = 500;

    public string Nombre { get; }

    public string Descripcion { get; }

    public decimal Precio { get; }

    public DateTime FechaEntregaPrometida { get; }

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

    // La descripcion es opcional: una cadena vacia significa que el modelo no tiene descripcion.
    private static string DescripcionValidada(string descripcion)
    {
        string descripcionSinEspacios = descripcion?.Trim() ?? string.Empty;
        if (descripcionSinEspacios.Length > LargoMaximoDescripcion)
        {
            throw new DominioException(
                $"La descripcion no puede superar los {LargoMaximoDescripcion} caracteres");
        }

        return descripcionSinEspacios;
    }

    private static decimal PrecioValidado(decimal precio)
    {
        decimal precioRedondeado = Math.Round(precio, 0, MidpointRounding.AwayFromZero);
        if (precioRedondeado <= 0)
        {
            throw new DominioException("El precio debe ser mayor a cero");
        }

        return precioRedondeado;
    }
}
