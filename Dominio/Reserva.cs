using Dominio.Estados;
using Dominio.Excepciones;
using Dominio.Tiempo;

namespace Dominio;

/// <summary>
/// Solicitud de reserva de una unidad (CR5, CR6, CR9). Nace pendiente y un proceso diferido la
/// confirma o la rechaza pasados los segundos configurados.
///
/// Su ciclo de vida es un State (GoF): la reserva no valida transiciones con condicionales, le pide
/// al <see cref="EstadoDeReserva"/> actual la transicion y este devuelve el estado siguiente o lanza
/// si no la permite. Vive dentro de su <see cref="Modelo"/> y lo referencia: <see cref="Resolver"/>
/// es un doble despacho que delega en <c>modelo.ResolverReserva(this)</c>, para que la decision de
/// confirmar o rechazar ocurra donde estan las unidades.
///
/// Los datos del modelo y del usuario se exponen con accesores delegados
/// (<see cref="NombreDelModelo"/>) para que ningun llamador escriba un choque de trenes como
/// <c>reserva.Modelo.Nombre</c> (Ley de Demeter). El plazo se mide contra el <see cref="IReloj"/>
/// inyectado y no contra <c>DateTime.UtcNow</c> (DIP): "ya vencio el plazo" es una regla de negocio
/// verificable sin dormir el hilo de prueba.
/// </summary>
public class Reserva
{
    private readonly Usuario solicitante;
    private readonly Modelo modelo;
    private readonly IReloj reloj;
    private EstadoDeReserva estado;
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

    /// <summary>
    /// Pendiente o confirmada: es lo que CR3 y CR4 cuentan como reserva activa sobre un modelo.
    /// </summary>
    public bool EstaActiva => estado.EsActiva;

    /// <summary>Una reserva rechazada no se puede cancelar (CR6); una terminal tampoco.</summary>
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

    public bool VencioElPlazo(int segundosDeEspera)
    {
        return EstaPendiente && reloj.Ahora >= FechaCreacion.AddSeconds(segundosDeEspera);
    }

    /// <summary>
    /// Doble despacho de CR5: la reserva no sabe si quedan unidades, su modelo si. El modelo
    /// termina llamando a <see cref="Confirmar"/> o <see cref="Rechazar"/> segun el stock.
    /// </summary>
    public void Resolver()
    {
        modelo.ResolverReserva(this);
    }

    /// <summary>
    /// Cancelacion de CR6, por doble despacho igual que <see cref="Resolver"/>: el modelo es quien
    /// tiene el candado bajo el que deben ocurrir las transiciones, asi que la reserva le pide a el
    /// que la cancele en lugar de cambiarse el estado por su cuenta.
    /// </summary>
    public void Cancelar()
    {
        modelo.CancelarReserva(this);
    }

    /// <summary>
    /// Las tres transiciones son <c>internal</c> y las invoca unicamente <see cref="Modelo"/>, ya
    /// dentro de su candado. Si fueran publicas, cualquiera podria cambiarle el estado a una reserva
    /// sin sincronizar con el stock que ese estado representa.
    /// </summary>
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

    /// <summary>
    /// Si la reserva esta confirmada, devuelve exactamente la unidad que conserva asignada. La
    /// transicion se valida antes de tocar el stock, y solo se completa despues de liberarlo.
    /// </summary>
    internal void AplicarCancelacion()
    {
        EstadoDeReserva estadoCancelado = estado.Cancelar();
        ExigirInvarianteDeAsignacion();

        if (vehiculoAsignado is Vehiculo unidad)
        {
            modelo.LiberarUnidad(unidad);
            vehiculoAsignado = null;
        }

        PasarA(estadoCancelado);
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
