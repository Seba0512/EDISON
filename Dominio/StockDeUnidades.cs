using Dominio.Excepciones;

namespace Dominio;

/// <summary>
/// Conjunto de unidades fisicas de un modelo: las crea, las cuenta por estado, las asigna a una
/// reserva y las libera (CR3, CR5, CR6, CR8).
///
/// Se separa de <see cref="Modelo"/> por SRP: la ficha del catalogo cambia cuando el vendedor corrige
/// precio, descripcion o fecha de entrega; el stock cambia cuando se reserva, se cancela o se ajusta
/// la cantidad de unidades. Son dos motivos de cambio distintos, entonces son dos clases.
///
/// Es el Creador (GRASP) de sus <see cref="UnidadVehiculo"/>: las contiene y tiene sus datos de
/// inicializacion. No guarda contadores sino que cuenta las unidades por estado (Experto en
/// informacion), asi no hay totales que se puedan desincronizar del estado real.
/// </summary>
public class StockDeUnidades
{
    private readonly List<UnidadVehiculo> unidades = new();
    private readonly string nombreModelo;

    public StockDeUnidades(string nombreModelo)
    {
        this.nombreModelo = nombreModelo;
    }

    public int Total => unidades.Count;

    public int Disponibles => unidades.Count(unidad => unidad.EstaDisponible);

    public int Reservadas => Total - Disponibles;

    public bool HayAlgunaDisponible => Disponibles > 0;

    public bool NoHayNingunaReservada => Reservadas == 0;

    public void Agregar(int cantidad)
    {
        if (cantidad <= 0)
        {
            throw new DominioException("La cantidad de unidades a agregar debe ser mayor a cero");
        }

        for (int agregadas = 0; agregadas < cantidad; agregadas++)
        {
            unidades.Add(new UnidadVehiculo(nombreModelo));
        }
    }

    /// <summary>Ajuste de stock (CR3): nunca por debajo de las unidades que ya estan reservadas.</summary>
    public void AjustarA(int totalNuevo)
    {
        if (totalNuevo < Reservadas)
        {
            throw new DominioException(
                $"El stock no puede quedar por debajo de las {Reservadas} unidades reservadas");
        }

        if (totalNuevo > Total)
        {
            Agregar(totalNuevo - Total);
            return;
        }

        QuitarDisponibles(Total - totalNuevo);
    }

    /// <summary>
    /// Toma una unidad libre para una reserva que se confirma (CR5). Es un comando: no devuelve la
    /// unidad, el vinculo queda del lado de la unidad y evita la referencia cruzada con la reserva.
    /// </summary>
    public void AsignarUnaA(Guid reservaId)
    {
        UnidadVehiculo unidadLibre = unidades.Find(unidad => unidad.EstaDisponible)
                                     ?? throw new DominioException(
                                         $"El modelo {nombreModelo} no tiene unidades disponibles");

        unidadLibre.AsignarA(reservaId);
    }

    /// <summary>Devuelve al stock la unidad de una reserva cancelada (CR6).</summary>
    public void LiberarLaDe(Guid reservaId)
    {
        UnidadVehiculo unidadOcupada = unidades.Find(unidad => unidad.EstaOcupadaPor(reservaId))
                                       ?? throw new DominioException(
                                           $"La reserva {reservaId} no tiene una unidad asignada en {nombreModelo}");

        unidadOcupada.Liberar();
    }

    public bool TieneUnaAsignadaA(Guid reservaId)
    {
        return unidades.Exists(unidad => unidad.EstaOcupadaPor(reservaId));
    }

    private void QuitarDisponibles(int cantidad)
    {
        for (int quitadas = 0; quitadas < cantidad; quitadas++)
        {
            UnidadVehiculo unidadLibre = unidades.Find(unidad => unidad.EstaDisponible)!;
            unidades.Remove(unidadLibre);
        }
    }
}
