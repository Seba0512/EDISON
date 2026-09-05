using Dominio.Excepciones;

namespace Dominio;

/// <summary>
/// Pool de unidades fisicas de un modelo: las crea, las cuenta por estado, toma una libre cuando
/// una reserva se confirma y la devuelve cuando se cancela (CR3, CR5, CR6, CR8).
///
/// Se separa de <see cref="Modelo"/> por SRP: los datos del catalogo cambian cuando el vendedor
/// corrige precio, descripcion o fecha de entrega; el stock cambia cuando se reserva, se cancela o
/// se ajusta la cantidad de unidades. Son dos motivos de cambio distintos, entonces son dos clases.
///
/// Es el Creador (GRASP) de sus <see cref="Vehiculo"/> y cuenta por estado en lugar de llevar
/// contadores (Experto en informacion), asi no hay totales que se desincronicen del estado real.
/// Al tomar una unidad devuelve la entidad concreta, y solo libera unidades que pertenecen a este
/// pool: la reserva puede conservar la identidad fisica sin abrir la composicion del modelo.
///
/// Es <c>internal</c> y no lleva candado propio: es parte interna del agregado que encabeza
/// <see cref="Modelo"/> y se accede siempre bajo el candado de ese modelo. Dejarlo publico seria
/// dejar abierta una puerta para tocar las unidades sin sincronizar.
/// </summary>
internal class StockDeUnidades
{
    private readonly List<Vehiculo> unidades = new();
    private readonly string nombreModelo;

    public StockDeUnidades(string nombreModelo)
    {
        this.nombreModelo = nombreModelo;
    }

    public int Total => unidades.Count;

    public int Disponibles => unidades.Count(unidad => unidad.EstaDisponible);

    public int Reservadas => Total - Disponibles;

    public bool HayAlgunaDisponible => Disponibles > 0;

    public void Agregar(int cantidad)
    {
        if (cantidad <= 0)
        {
            throw new DominioException("La cantidad de unidades a agregar debe ser mayor a cero");
        }

        for (int agregadas = 0; agregadas < cantidad; agregadas++)
        {
            unidades.Add(new Vehiculo(nombreModelo));
        }
    }

    /// <summary>Ajuste fisico del stock: nunca puede quitar unidades que ya estan reservadas.</summary>
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

    /// <summary>Toma y devuelve la unidad libre concreta asignada a una reserva (CR5).</summary>
    public Vehiculo TomarUna()
    {
        Vehiculo unidadLibre = unidades.Find(unidad => unidad.EstaDisponible)
                                     ?? throw new SinUnidadesDisponiblesException(nombreModelo);

        unidadLibre.Reservar();
        return unidadLibre;
    }

    /// <summary>Devuelve exactamente la unidad asignada cuando una reserva se cancela (CR6).</summary>
    public void Liberar(Vehiculo unidad)
    {
        if (!Contiene(unidad))
        {
            throw new DominioException(
                $"La unidad {unidad.Id} no pertenece al modelo {nombreModelo}");
        }

        unidad.Liberar();
    }

    public bool Contiene(Vehiculo unidad)
    {
        return unidades.Contains(unidad);
    }

    private void QuitarDisponibles(int cantidad)
    {
        for (int quitadas = 0; quitadas < cantidad; quitadas++)
        {
            Vehiculo unidadLibre = unidades.Find(unidad => unidad.EstaDisponible)!;
            unidades.Remove(unidadLibre);
        }
    }
}
