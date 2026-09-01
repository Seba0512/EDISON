using Dominio.Excepciones;

namespace Dominio;

/// <summary>
/// Unidad fisica de un modelo: el recurso escaso que se reserva (CR5) y se libera (CR6).
/// Es parte de la composicion que encabeza <see cref="Modelo"/>, por eso solo el modelo puede
/// crearla (GRASP Creador: el contenedor crea a sus partes).
/// </summary>
public class UnidadVehiculo
{
    public Guid Id { get; }

    public string NombreModelo { get; }

    public EstadoUnidad Estado { get; private set; }

    public Guid ReservaQueLaOcupa { get; private set; }

    internal UnidadVehiculo(string nombreModelo)
    {
        Id = Guid.NewGuid();
        NombreModelo = nombreModelo;
        Estado = EstadoUnidad.Disponible;
        ReservaQueLaOcupa = Guid.Empty;
    }

    public bool EstaDisponible => Estado == EstadoUnidad.Disponible;

    public bool EstaOcupadaPor(Guid reservaId)
    {
        return Estado == EstadoUnidad.Reservada && ReservaQueLaOcupa == reservaId;
    }

    internal void AsignarA(Guid reservaId)
    {
        if (!EstaDisponible)
        {
            throw new DominioException($"La unidad {Id} ya esta reservada");
        }

        Estado = EstadoUnidad.Reservada;
        ReservaQueLaOcupa = reservaId;
    }

    internal void Liberar()
    {
        if (EstaDisponible)
        {
            throw new DominioException($"La unidad {Id} ya estaba disponible");
        }

        Estado = EstadoUnidad.Disponible;
        ReservaQueLaOcupa = Guid.Empty;
    }
}
