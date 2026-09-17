using Dominio.Estados;
using Dominio.Excepciones;
using Dominio.Tiempo;

namespace Dominio;

public class Reserva
{
    private readonly Usuario solicitante;
    private readonly Modelo modelo;
    private readonly IReloj reloj;
    private EstadoDeReserva estado;
    // Solo una reserva confirmada tiene unidad asignada. En cualquier otro estado no hay unidad.
    private Vehiculo? vehiculoAsignado;

    public Guid Id { get; }

    public DateTime FechaCreacion { get; }

    public DateTime UltimaActualizacion { get; private set; }

    public Vehiculo? VehiculoAsignado => vehiculoAsignado;

    internal Reserva(Usuario solicitante, Modelo modelo, IReloj reloj)
    {
        this.solicitante = solicitante;
        this.modelo = modelo;
        this.reloj = reloj;
        Id = Guid.NewGuid();
        estado = Pendiente.Instancia;
        FechaCreacion = reloj.Ahora;
        UltimaActualizacion = FechaCreacion;
    }

    public bool EstaPendiente => estado.EstaPendiente;

    public bool EstaConfirmada => estado.EstaConfirmada;

    public bool EstaActiva => estado.EsActiva;

    public bool PuedeCancelarse => estado.EsActiva;

    public string NombreDelEstado()
    {
        return estado.Nombre;
    }

    public string NombreDelUsuario()
    {
        return solicitante.NombreUsuario;
    }

    public string NombreDelModelo()
    {
        return modelo.Nombre;
    }

    public bool FueHechaPor(string nombreUsuario)
    {
        return solicitante.SeLlama(nombreUsuario);
    }

    public bool EsDelModelo(string nombreModelo)
    {
        return modelo.SeLlama(nombreModelo);
    }

    public LecturaDeReserva TomarLectura()
    {
        return modelo.TomarLecturaDe(this);
    }

    public bool VencioElPlazo(int segundosDeEspera)
    {
        return EstaPendiente && reloj.Ahora >= FechaCreacion.AddSeconds(segundosDeEspera);
    }

    public ResultadoResolucion Resolver()
    {
        return modelo.ResolverReserva(this);
    }

    public void Cancelar()
    {
        modelo.CancelarReserva(this);
    }

    internal void Confirmar(Vehiculo unidad)
    {
        EstadoDeReserva estadoConfirmado = estado.Confirmar();
        ExigirUnidadAsignable(unidad);
        vehiculoAsignado = unidad;
        PasarA(estadoConfirmado);
    }

    internal void Rechazar()
    {
        PasarA(estado.Rechazar());
    }

    // La cancelación va en dos pasos y el modelo libera la unidad entre uno y otro, bajo su candado.
    internal EstadoDeReserva PrepararCancelacion()
    {
        EstadoDeReserva estadoCancelado = estado.Cancelar();
        ExigirInvarianteDeAsignacion();
        return estadoCancelado;
    }

    internal void CompletarCancelacion(EstadoDeReserva estadoCancelado)
    {
        vehiculoAsignado = null;
        PasarA(estadoCancelado);
    }

    internal LecturaDeReserva CrearLectura(string nombreModelo)
    {
        return new LecturaDeReserva(
            Id,
            nombreModelo,
            estado.Nombre,
            FechaCreacion,
            UltimaActualizacion);
    }

    private void ExigirUnidadAsignable(Vehiculo unidad)
    {
        if (vehiculoAsignado is not null)
        {
            throw new DominioException("La reserva ya tiene una unidad asignada");
        }

        if (!modelo.ContieneUnidad(unidad))
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
        bool debeTenerUnidad = estado.EstaConfirmada;
        if (debeTenerUnidad != (vehiculoAsignado is not null))
        {
            throw new DominioException(
                "Solo una reserva confirmada puede tener exactamente una unidad asignada");
        }
    }

    private void PasarA(EstadoDeReserva estadoNuevo)
    {
        estado = estadoNuevo;
        UltimaActualizacion = reloj.Ahora;
    }
}
