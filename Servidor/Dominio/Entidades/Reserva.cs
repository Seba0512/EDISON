using Servidor.Dominio.Excepciones;

namespace Servidor.Dominio.Entidades;

public enum EstadoDeReserva
{
    Pendiente,
    Confirmada,
    Rechazada,
    Cancelada
}

public sealed record LecturaDeReserva(
    Guid Id,
    string NombreModelo,
    EstadoDeReserva Estado,
    DateTime FechaCreacion,
    DateTime UltimaActualizacion);

// Las transiciones las coordina Edison bajo su candado global. Pendiente puede confirmarse,
// rechazarse o cancelarse. Confirmada solo puede cancelarse. Rechazada y Cancelada son finales.
public class Reserva
{
    private readonly Usuario _solicitante;
    private readonly VehiculoModelo _modelo;

    public Guid Id { get; }

    public EstadoDeReserva Estado { get; private set; }

    public DateTime FechaCreacion { get; }

    public DateTime UltimaActualizacion { get; private set; }

    internal Reserva(Usuario solicitante, VehiculoModelo modelo)
    {
        _solicitante = solicitante;
        _modelo = modelo;
        Id = Guid.NewGuid();
        Estado = EstadoDeReserva.Pendiente;
        FechaCreacion = DateTime.UtcNow;
        UltimaActualizacion = FechaCreacion;
    }

    public bool EstaPendiente => Estado == EstadoDeReserva.Pendiente;

    public bool EstaActiva => Estado is EstadoDeReserva.Pendiente or EstadoDeReserva.Confirmada;

    internal VehiculoModelo Modelo => _modelo;

    internal Usuario Solicitante => _solicitante;

    public string NombreDelModelo()
    {
        return _modelo.Nombre;
    }

    public bool FueHechaPor(string nombreUsuario)
    {
        return _solicitante.SeLlama(nombreUsuario);
    }

    public bool EsDelModelo(VehiculoModelo modelo)
    {
        return ReferenceEquals(_modelo, modelo);
    }

    public LecturaDeReserva TomarLectura()
    {
        return new LecturaDeReserva(Id, _modelo.Nombre, Estado, FechaCreacion, UltimaActualizacion);
    }

    internal void Confirmar()
    {
        ExigirQueEstePendiente("confirmar");
        PasarA(EstadoDeReserva.Confirmada);
    }

    internal void Rechazar()
    {
        ExigirQueEstePendiente("rechazar");
        PasarA(EstadoDeReserva.Rechazada);
    }

    internal void Cancelar()
    {
        if (Estado is EstadoDeReserva.Rechazada or EstadoDeReserva.Cancelada)
        {
            throw new DominioException($"No se puede cancelar una reserva {Estado}");
        }

        PasarA(EstadoDeReserva.Cancelada);
    }

    private void ExigirQueEstePendiente(string operacion)
    {
        if (Estado != EstadoDeReserva.Pendiente)
        {
            throw new DominioException($"No se puede {operacion} una reserva {Estado}");
        }
    }

    private void PasarA(EstadoDeReserva estadoNuevo)
    {
        Estado = estadoNuevo;
        UltimaActualizacion = DateTime.UtcNow;
    }
}
