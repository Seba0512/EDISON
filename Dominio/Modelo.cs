using Dominio.Excepciones;

namespace Dominio;

/// <summary>
/// Modelo del catalogo y raiz de la composicion con sus <see cref="UnidadVehiculo"/>.
/// La letra separa las dos cosas: el catalogo es permanente y lo que varia es el stock de unidades.
/// Por eso el modelo no guarda un contador de stock; las cantidades de CR8 se responden contando
/// unidades por estado (GRASP Experto en informacion), y asi no hay contadores que se desincronicen.
/// </summary>
public class Modelo
{
    private readonly List<UnidadVehiculo> unidades = new();
    private FichaModelo ficha;

    public string CreadoPor { get; }

    public Modelo(FichaModelo ficha, string creadoPor)
    {
        this.ficha = ficha;
        CreadoPor = NombreDeCreadorValidado(creadoPor);
    }

    public string Nombre => ficha.Nombre;

    public string Descripcion => ficha.Descripcion;

    public decimal Precio => ficha.Precio;

    public DateTime FechaEntregaPrometida => ficha.FechaEntregaPrometida;

    public string NombreArchivoImagen => ficha.NombreArchivoImagen;

    public bool TieneImagen => ficha.TieneImagen;

    public int StockTotal => unidades.Count;

    public int UnidadesDisponibles => unidades.Count(unidad => unidad.EstaDisponible);

    public int UnidadesReservadas => StockTotal - UnidadesDisponibles;

    public bool TieneUnidadDisponible => UnidadesDisponibles > 0;

    /// <summary>
    /// Condicion de baja que el modelo puede responder por si mismo (CR4). El caso de uso debe
    /// ademas verificar que no queden reservas pendientes, porque una pendiente todavia no ocupa
    /// unidad y el modelo no las conoce.
    /// </summary>
    public bool NoTieneUnidadesReservadas => UnidadesReservadas == 0;

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
        if (cantidad <= 0)
        {
            throw new DominioException("La cantidad de unidades a agregar debe ser mayor a cero");
        }

        for (int agregadas = 0; agregadas < cantidad; agregadas++)
        {
            unidades.Add(new UnidadVehiculo(Nombre));
        }
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

    /// <summary>Ajuste de stock (CR3): nunca por debajo de las unidades que ya estan reservadas.</summary>
    public void AjustarStock(int stockNuevo)
    {
        if (stockNuevo < UnidadesReservadas)
        {
            throw new DominioException(
                $"El stock no puede quedar por debajo de las {UnidadesReservadas} unidades reservadas");
        }

        if (stockNuevo > StockTotal)
        {
            AgregarUnidades(stockNuevo - StockTotal);
            return;
        }

        QuitarUnidadesDisponibles(StockTotal - stockNuevo);
    }

    /// <summary>
    /// Toma una unidad libre para una reserva que se confirma (CR5). Es un comando: no devuelve la
    /// unidad, el vinculo queda del lado de la unidad y evita la referencia cruzada con la reserva.
    /// </summary>
    public void AsignarUnidadA(Guid reservaId)
    {
        UnidadVehiculo unidadLibre = unidades.Find(unidad => unidad.EstaDisponible)
                                     ?? throw new DominioException(
                                         $"El modelo {Nombre} no tiene unidades disponibles");

        unidadLibre.AsignarA(reservaId);
    }

    /// <summary>Devuelve al stock la unidad de una reserva cancelada (CR6).</summary>
    public void LiberarUnidadDe(Guid reservaId)
    {
        UnidadVehiculo unidadOcupada = unidades.Find(unidad => unidad.EstaOcupadaPor(reservaId))
                                       ?? throw new DominioException(
                                           $"La reserva {reservaId} no tiene una unidad asignada en {Nombre}");

        unidadOcupada.Liberar();
    }

    public bool TieneUnidadAsignadaA(Guid reservaId)
    {
        return unidades.Exists(unidad => unidad.EstaOcupadaPor(reservaId));
    }

    private void QuitarUnidadesDisponibles(int cantidad)
    {
        for (int quitadas = 0; quitadas < cantidad; quitadas++)
        {
            UnidadVehiculo unidadLibre = unidades.Find(unidad => unidad.EstaDisponible)!;
            unidades.Remove(unidadLibre);
        }
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
