using Dominio.Excepciones;

namespace Dominio;

public class Vehiculo
{
    public Guid Id { get; }

    public string NombreModelo { get; }

    public EstadoDeVehiculo Estado { get; private set; }

    internal Vehiculo(string nombreModelo)
    {
        Id = Guid.NewGuid();
        NombreModelo = nombreModelo;
        Estado = EstadoDeVehiculo.Disponible;
    }

    public bool EstaDisponible => Estado == EstadoDeVehiculo.Disponible;

    internal void Reservar()
    {
        if (!EstaDisponible)
        {
            throw new DominioException($"La unidad {Id} ya esta reservada");
        }

        Estado = EstadoDeVehiculo.Reservada;
    }

    internal void Liberar()
    {
        if (EstaDisponible)
        {
            throw new DominioException($"La unidad {Id} ya estaba disponible");
        }

        Estado = EstadoDeVehiculo.Disponible;
    }
}
