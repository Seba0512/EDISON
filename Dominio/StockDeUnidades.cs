using Dominio.Excepciones;

namespace Dominio;

// No tiene candado propio. Siempre se usa bajo el candado del Modelo que lo contiene.
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

    public Vehiculo TomarUna()
    {
        Vehiculo unidadLibre = unidades.Find(unidad => unidad.EstaDisponible)
                                     ?? throw new SinUnidadesDisponiblesException(nombreModelo);

        unidadLibre.Reservar();
        return unidadLibre;
    }

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
