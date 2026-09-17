using Dominio.Estados;
using Dominio.Excepciones;
using Dominio.Tiempo;

namespace Dominio;

public class Reserva
{
    private readonly Usuario _solicitante;
    private readonly Modelo _modelo;
    private readonly IReloj _reloj;
    private EstadoDeReserva _estado;
    // Solo una reserva confirmada tiene unidad asignada. En cualquier otro estado no hay unidad.
    private Vehiculo? _vehiculoAsignado;

    public Guid Id { get; }

    public DateTime FechaCreacion { get; }

    public DateTime UltimaActualizacion { get; private set; }

    public Vehiculo? VehiculoAsignado => _vehiculoAsignado;

    internal Reserva(Usuario solicitante, Modelo modelo, IReloj reloj)
    {
        _solicitante = solicitante;
        _modelo = modelo;
        _reloj = reloj;
        Id = Guid.NewGuid();
        _estado = Pendiente.Instancia;
        FechaCreacion = reloj.Ahora;
        UltimaActualizacion = FechaCreacion;
    }

    public bool EstaPendiente => _estado.EstaPendiente;

    internal bool EstaConfirmada => _estado.EstaConfirmada;

    public bool EstaActiva => _estado.EsActiva;

    internal string NombreDelEstado()
    {
        return _estado.Nombre;
    }

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

    internal void Confirmar(Vehiculo unidad)
    {
        EstadoDeReserva estadoConfirmado = _estado.Confirmar();
        ExigirUnidadAsignable(unidad);
        _vehiculoAsignado = unidad;
        PasarA(estadoConfirmado);
    }

    internal void Rechazar()
    {
        PasarA(_estado.Rechazar());
    }

    // La cancelación va en dos pasos y el modelo libera la unidad entre uno y otro, bajo su candado.
    internal EstadoDeReserva PrepararCancelacion()
    {
        EstadoDeReserva estadoCancelado = _estado.Cancelar();
        ExigirInvarianteDeAsignacion();
        return estadoCancelado;
    }

    internal void CompletarCancelacion(EstadoDeReserva estadoCancelado)
    {
        _vehiculoAsignado = null;
        PasarA(estadoCancelado);
    }

    internal LecturaDeReserva CrearLectura(string nombreModelo)
    {
        return new LecturaDeReserva(
            Id,
            nombreModelo,
            _estado.Nombre,
            FechaCreacion,
            UltimaActualizacion);
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

    private void ExigirInvarianteDeAsignacion()
    {
        bool debeTenerUnidad = _estado.EstaConfirmada;
        if (debeTenerUnidad != (_vehiculoAsignado is not null))
        {
            throw new DominioException(
                "Solo una reserva confirmada puede tener exactamente una unidad asignada");
        }
    }

    private void PasarA(EstadoDeReserva estadoNuevo)
    {
        _estado = estadoNuevo;
        UltimaActualizacion = _reloj.Ahora;
    }
}
