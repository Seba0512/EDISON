using Dominio.Excepciones;

namespace Dominio;

/// <summary>
/// Unidad fisica de un modelo: el recurso escaso que se reserva (CR5) y se libera (CR6).
/// No sabe que reserva la ocupa: el pool no distingue unidades, solo estados. Es parte de la
/// composicion que encabeza <see cref="Modelo"/>, por eso solo el stock puede crearla y cambiarle
/// el estado (GRASP Creador: el contenedor crea a sus partes).
/// </summary>
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
