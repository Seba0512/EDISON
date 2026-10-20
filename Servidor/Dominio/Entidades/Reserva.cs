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
    DateTime UltimaActualizacion,
    string NombreSolicitante);

// Las transiciones las coordinan los servicios bajo el candado global. Pendiente puede confirmarse,
// rechazarse o cancelarse. Confirmada solo puede cancelarse. Rechazada y Cancelada son finales.
// Guarda el nombre del modelo y no una referencia: sigue mostrándose aunque el modelo se dé de baja.
public class Reserva
{
    public Guid Id { get; }

    public string NombreSolicitante { get; }

    // Null cuando el modelo fue dado de baja (la base lo deja en NULL).
    public int? ModeloId { get; }

    public string NombreModelo { get; }

    public EstadoDeReserva Estado { get; private set; }

    public DateTime FechaCreacion { get; }

    public DateTime UltimaActualizacion { get; private set; }

    internal Reserva(Usuario solicitante, VehiculoModelo modelo)
    {
        Id = Guid.NewGuid();
        NombreSolicitante = solicitante.NombreUsuario;
        ModeloId = modelo.Id;
        NombreModelo = modelo.Nombre;
        Estado = EstadoDeReserva.Pendiente;
        FechaCreacion = DateTime.UtcNow;
        UltimaActualizacion = FechaCreacion;
    }

    private Reserva(
        Guid id,
        string nombreSolicitante,
        int? modeloId,
        string nombreModelo,
        EstadoDeReserva estado,
        DateTime fechaCreacion,
        DateTime ultimaActualizacion)
    {
        Id = id;
        NombreSolicitante = nombreSolicitante;
        ModeloId = modeloId;
        NombreModelo = nombreModelo;
        Estado = estado;
        FechaCreacion = fechaCreacion;
        UltimaActualizacion = ultimaActualizacion;
    }

    // Rehidrata una reserva guardada. Las fechas están en UTC: la base las devuelve sin zona.
    internal static Reserva Restaurar(
        Guid id,
        string nombreSolicitante,
        int? modeloId,
        string nombreModelo,
        EstadoDeReserva estado,
        DateTime fechaCreacion,
        DateTime ultimaActualizacion)
    {
        return new Reserva(
            id,
            nombreSolicitante,
            modeloId,
            nombreModelo,
            estado,
            DateTime.SpecifyKind(fechaCreacion, DateTimeKind.Utc),
            DateTime.SpecifyKind(ultimaActualizacion, DateTimeKind.Utc));
    }

    public bool EstaPendiente => Estado == EstadoDeReserva.Pendiente;

    public bool EstaActiva => Estado is EstadoDeReserva.Pendiente or EstadoDeReserva.Confirmada;

    public string NombreDelModelo()
    {
        return NombreModelo;
    }

    public bool FueHechaPor(string nombreUsuario)
    {
        return NombreSolicitante.Equals(nombreUsuario?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public LecturaDeReserva TomarLectura()
    {
        return new LecturaDeReserva(
            Id, NombreModelo, Estado, FechaCreacion, UltimaActualizacion, NombreSolicitante);
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
