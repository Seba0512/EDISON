using Dominio.Excepciones;

namespace Dominio;

/// <summary>
/// Modelo del catalogo: su ficha descriptiva y quien lo dio de alta (CR2, CR3, CR4).
/// La letra separa el catalogo, que es permanente, del stock, que varia; por eso las unidades no
/// viven aca sino en un <see cref="StockDeUnidades"/> al que el modelo delega (SRP).
/// </summary>
public class Modelo
{
    private readonly StockDeUnidades stock;
    private FichaModelo ficha;

    public string CreadoPor { get; }

    public Modelo(FichaModelo ficha, string creadoPor)
    {
        this.ficha = ficha;
        CreadoPor = NombreDeCreadorValidado(creadoPor);
        stock = new StockDeUnidades(ficha.Nombre);
    }

    public string Nombre => ficha.Nombre;

    public string Descripcion => ficha.Descripcion;

    public decimal Precio => ficha.Precio;

    public DateTime FechaEntregaPrometida => ficha.FechaEntregaPrometida;

    public string NombreArchivoImagen => ficha.NombreArchivoImagen;

    public bool TieneImagen => ficha.TieneImagen;

    public int StockTotal => stock.Total;

    public int UnidadesDisponibles => stock.Disponibles;

    public int UnidadesReservadas => stock.Reservadas;

    public bool TieneUnidadDisponible => stock.HayAlgunaDisponible;

    /// <summary>
    /// Condicion de baja que el modelo puede responder por si mismo (CR4). El caso de uso debe
    /// ademas verificar que no queden reservas pendientes, porque una pendiente todavia no ocupa
    /// unidad y el modelo no las conoce.
    /// </summary>
    public bool NoTieneUnidadesReservadas => stock.NoHayNingunaReservada;

    public bool FueCreadoPor(string nombreUsuario)
    {
        return CreadoPor.Equals(nombreUsuario?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public bool SeLlama(string nombreModelo)
    {
        return Nombre.Equals(nombreModelo?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public void AgregarUnidades(int cantidad)
    {
        stock.Agregar(cantidad);
    }

    /// <summary>Modificacion de los datos descriptivos (CR3), permitida mientras haya stock libre.</summary>
    public void ActualizarFicha(FichaModelo fichaNueva)
    {
        if (!ficha.DescribeAlMismoModeloQue(fichaNueva))
        {
            throw new DominioException("El nombre del modelo no se puede modificar");
        }

        if (!TieneUnidadDisponible)
        {
            throw new DominioException(
                $"No se puede modificar el modelo {Nombre} porque no quedan unidades disponibles");
        }

        ficha = fichaNueva;
    }

    public void AjustarStock(int stockNuevo)
    {
        stock.AjustarA(stockNuevo);
    }

    public void AsignarUnidadA(Guid reservaId)
    {
        stock.AsignarUnaA(reservaId);
    }

    public void LiberarUnidadDe(Guid reservaId)
    {
        stock.LiberarLaDe(reservaId);
    }

    public bool TieneUnidadAsignadaA(Guid reservaId)
    {
        return stock.TieneUnaAsignadaA(reservaId);
    }

    private static string NombreDeCreadorValidado(string creadoPor)
    {
        if (string.IsNullOrWhiteSpace(creadoPor))
        {
            throw new DominioException("El modelo debe tener un creador");
        }

        return creadoPor.Trim();
    }
}
