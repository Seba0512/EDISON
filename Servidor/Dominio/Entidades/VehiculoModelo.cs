using Servidor.Dominio.Excepciones;

namespace Servidor.Dominio.Entidades;

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

public class FichaDeModelo
{
    public DatosDeModelo Datos { get; }

    public int Stock { get; }

    public FichaDeModelo(DatosDeModelo datos, int stock)
    {
        if (stock <= 0)
        {
            throw new DominioException("El stock debe ser mayor a cero");
        }

        Datos = datos;
        Stock = stock;
    }

    public string Nombre => Datos.Nombre;
}

public sealed record LecturaDeModelo(
    string Nombre,
    string Descripcion,
    decimal Precio,
    DateTime FechaEntregaPrometida,
    int StockTotal,
    int UnidadesDisponibles,
    int UnidadesReservadas,
    string NombreArchivoImagen)
{
    public bool TieneImagen => NombreArchivoImagen.Length > 0;

    public bool TieneUnidadDisponible => UnidadesDisponibles > 0;
}

// El stock es la capacidad total y las disponibles se derivan restando las reservas confirmadas,
// que Edison cuenta sobre su lista de reservas: no hay contadores duplicados que desincronizar.
// Todos los métodos se llaman con el candado global de Edison ya tomado; acá no hay locks.
public class VehiculoModelo
{
    private readonly Usuario _creador;
    private DatosDeModelo _datos;
    private bool _eliminado;

    public VehiculoModelo(FichaDeModelo ficha, Usuario creador)
    {
        _datos = ficha.Datos;
        Stock = ficha.Stock;
        _creador = creador;
    }

    public string Nombre => _datos.Nombre;

    public bool EstaEliminado => _eliminado;

    public int Stock { get; private set; }

    public LecturaDeModelo TomarLectura(int confirmadas)
    {
        return new LecturaDeModelo(
            _datos.Nombre,
            _datos.Descripcion,
            _datos.Precio,
            _datos.FechaEntregaPrometida,
            Stock,
            Stock - confirmadas,
            confirmadas,
            _datos.NombreArchivoImagen);
    }

    public DateTime FechaEntregaVigente => _datos.FechaEntregaPrometida;

    public void ModificarDatos(FichaDeModelo ficha, Usuario solicitante, int confirmadas, int activas)
    {
        ExigirActivo();
        ExigirPropietario(solicitante);
        if (!_datos.DescribeAlMismoModeloQue(ficha.Datos))
        {
            throw new DominioException("El nombre del modelo no se puede modificar");
        }

        // Se permite reponer stock para que un modelo agotado no quede congelado para siempre.
        if (Stock - confirmadas == 0 && ficha.Stock <= Stock)
        {
            throw new DominioException(
                $"No se puede modificar el modelo {Nombre} porque no quedan unidades disponibles");
        }

        if (ficha.Stock != Stock && ficha.Stock <= activas)
        {
            throw new DominioException(
                $"El stock debe ser mayor a las {activas} reservas activas del modelo {Nombre}");
        }

        if (ficha.Stock < confirmadas)
        {
            throw new DominioException(
                $"El stock no puede quedar por debajo de las {confirmadas} unidades reservadas");
        }

        Stock = ficha.Stock;
        // La imagen no forma parte de esta operación y se conserva desde el estado vigente.
        _datos = new DatosDeModelo(
            ficha.Datos.Nombre,
            ficha.Datos.Descripcion,
            ficha.Datos.Precio,
            ficha.Datos.FechaEntregaPrometida,
            _datos.NombreArchivoImagen);
    }

    // Devuelve la imagen reemplazada para que quien llama borre el archivo sin fugas.
    public string CambiarImagen(Usuario solicitante, string nuevoNombreArchivoImagen, int confirmadas)
    {
        ExigirActivo();
        ExigirPropietario(solicitante);
        if (Stock - confirmadas == 0)
        {
            throw new DominioException(
                $"No se puede cambiar la imagen del modelo {Nombre} porque no quedan unidades disponibles");
        }

        string nombreArchivoAnterior = _datos.NombreArchivoImagen;
        _datos = new DatosDeModelo(
            _datos.Nombre,
            _datos.Descripcion,
            _datos.Precio,
            _datos.FechaEntregaPrometida,
            nuevoNombreArchivoImagen);

        return nombreArchivoAnterior;
    }

    // Prepara la baja y devuelve la imagen para borrarla fuera del candado.
    public string MarcarComoEliminado(Usuario solicitante, int reservasActivas)
    {
        ExigirActivo();
        ExigirPropietario(solicitante);
        if (reservasActivas > 0)
        {
            throw new DominioException(
                $"No se puede eliminar el modelo {Nombre} porque tiene reservas activas");
        }

        _eliminado = true;
        return _datos.NombreArchivoImagen;
    }

    // Si el archivo no pudo borrarse, el modelo vuelve a estar disponible para reintentar.
    public void RestaurarTrasBajaFallida()
    {
        _eliminado = false;
    }

    public void ExigirActivo()
    {
        if (_eliminado)
        {
            throw new DominioException($"El modelo {Nombre} fue eliminado");
        }
    }

    private void ExigirPropietario(Usuario usuario)
    {
        if (!_creador.SeLlama(usuario.NombreUsuario))
        {
            throw new DominioException(
                $"Solo el creador del modelo {Nombre} puede modificarlo o eliminarlo");
        }
    }
}
