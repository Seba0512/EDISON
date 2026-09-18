namespace Dominio;

public enum EstadoDeReserva
{
    Pendiente,
    Confirmada,
    Rechazada,
    Cancelada
}

public enum ResultadoResolucion
{
    SinCambios,
    Confirmada,
    Rechazada
}

public sealed record LecturaDeReserva(
    Guid Id,
    string NombreModelo,
    EstadoDeReserva Estado,
    DateTime FechaCreacion,
    DateTime UltimaActualizacion)
{
    public bool EstaActiva => Estado is EstadoDeReserva.Pendiente or EstadoDeReserva.Confirmada;
}

public class Reserva
{
    private readonly Usuario _solicitante;
    private readonly Modelo _modelo;
    private readonly IReloj _reloj;
    // Solo una reserva confirmada tiene unidad asignada. En cualquier otro estado no hay unidad.
    private Vehiculo? _vehiculoAsignado;

    public Guid Id { get; }

    public EstadoDeReserva Estado { get; private set; }

    public DateTime FechaCreacion { get; }

    public DateTime UltimaActualizacion { get; private set; }

    public Vehiculo? VehiculoAsignado => _vehiculoAsignado;

    internal Reserva(Usuario solicitante, Modelo modelo, IReloj reloj)
    {
        _solicitante = solicitante;
        _modelo = modelo;
        _reloj = reloj;
        Id = Guid.NewGuid();
        Estado = EstadoDeReserva.Pendiente;
        FechaCreacion = reloj.Ahora;
        UltimaActualizacion = FechaCreacion;
    }

    public bool EstaPendiente => Estado == EstadoDeReserva.Pendiente;

    public bool EstaActiva => Estado is EstadoDeReserva.Pendiente or EstadoDeReserva.Confirmada;

    public string NombreDelUsuario()
    {
        return _solicitante.NombreUsuario;
    }

    public string NombreDelModelo()
    {
        return _modelo.Nombre;
    }

    public bool FueHechaPor(string nombreUsuario)
    {
        return _solicitante.SeLlama(nombreUsuario);
    }

    public LecturaDeReserva TomarLectura()
    {
        return _modelo.TomarLecturaDe(this);
    }

    public bool VencioElPlazo(int segundosDeEspera)
    {
        return EstaPendiente && _reloj.Ahora >= FechaCreacion.AddSeconds(segundosDeEspera);
    }

    public ResultadoResolucion Resolver()
    {
        return _modelo.ResolverReserva(this);
    }

    public void Cancelar()
    {
        _modelo.CancelarReserva(this);
    }

    // Las transiciones las coordina el Modelo bajo su candado. Pendiente puede confirmarse,
    // rechazarse o cancelarse. Confirmada solo puede cancelarse. Rechazada y Cancelada son finales.
    internal void Confirmar(Vehiculo unidad)
    {
        ExigirQueEstePendiente("confirmar");
        ExigirUnidadAsignable(unidad);
        _vehiculoAsignado = unidad;
        PasarA(EstadoDeReserva.Confirmada);
    }

    internal void Rechazar()
    {
        ExigirQueEstePendiente("rechazar");
        PasarA(EstadoDeReserva.Rechazada);
    }

    internal void ExigirCancelable()
    {
        if (Estado is EstadoDeReserva.Rechazada or EstadoDeReserva.Cancelada)
        {
            throw new DominioException($"No se puede cancelar una reserva {Estado}");
        }
    }

    internal void MarcarCancelada()
    {
        _vehiculoAsignado = null;
        PasarA(EstadoDeReserva.Cancelada);
    }

    internal LecturaDeReserva CrearLectura(string nombreModelo)
    {
        return new LecturaDeReserva(Id, nombreModelo, Estado, FechaCreacion, UltimaActualizacion);
    }

    private void ExigirQueEstePendiente(string operacion)
    {
        if (Estado != EstadoDeReserva.Pendiente)
        {
            throw new DominioException($"No se puede {operacion} una reserva {Estado}");
        }
    }

    private void ExigirUnidadAsignable(Vehiculo unidad)
    {
        if (_vehiculoAsignado is not null)
        {
            throw new DominioException("La reserva ya tiene una unidad asignada");
        }

        if (!_modelo.ContieneUnidad(unidad))
        {
            throw new DominioException("La unidad no pertenece al modelo de la reserva");
        }

        if (unidad.EstaDisponible)
        {
            throw new DominioException("La unidad debe estar reservada antes de asignarla");
        }
    }

    private void PasarA(EstadoDeReserva estadoNuevo)
    {
        Estado = estadoNuevo;
        UltimaActualizacion = _reloj.Ahora;
    }
}
