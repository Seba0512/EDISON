using Dominio.Excepciones;
using Dominio.Tiempo;

namespace Dominio;

/// <summary>
/// Solicitud de reserva de una unidad (CR5, CR6, CR9). Nace pendiente y un proceso diferido la
/// confirma o la rechaza pasados los segundos configurados.
///
/// Vive dentro de su <see cref="Modelo"/> y lo referencia: <see cref="Resolver"/> es un doble
/// despacho que delega en <c>modelo.ResolverReserva(this)</c>, para que la decision de confirmar o
/// rechazar ocurra donde estan las unidades. Los datos del modelo y del usuario se exponen con
/// accesores delegados (<see cref="NombreDelModelo"/>) para que ningun llamador escriba un choque
/// de trenes como <c>reserva.Modelo.Nombre</c> (Ley de Demeter).
///
/// El plazo se mide contra el <see cref="IReloj"/> inyectado y no contra <c>DateTime.UtcNow</c>
/// (DIP): "ya vencio el plazo" es una regla de negocio verificable sin dormir el hilo de prueba.
/// </summary>
public class Reserva
{
    private readonly Usuario solicitante;
    private readonly Modelo modelo;
    private readonly IReloj reloj;

    public Guid Id { get; }

    public EstadoReserva Estado { get; private set; }

    public DateTime FechaCreacion { get; }

    public DateTime UltimaActualizacion { get; private set; }

    public Reserva(Usuario solicitante, Modelo modelo, IReloj reloj)
    {
        this.solicitante = solicitante;
        this.modelo = modelo;
        this.reloj = reloj;
        Id = Guid.NewGuid();
        Estado = EstadoReserva.Pendiente;
        FechaCreacion = reloj.Ahora;
        UltimaActualizacion = FechaCreacion;
    }

    public bool EstaPendiente => Estado == EstadoReserva.Pendiente;

    public bool EstaConfirmada => Estado == EstadoReserva.Confirmada;

    /// <summary>
    /// Pendiente o confirmada: es lo que CR3 y CR4 cuentan como reserva activa sobre un modelo.
    /// </summary>
    public bool EstaActiva => EstaPendiente || EstaConfirmada;

    /// <summary>Una reserva rechazada no se puede cancelar (CR6); una terminal tampoco.</summary>
    public bool PuedeCancelarse => EstaActiva;

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

    public void Confirmar()
    {
        ExigirQueEstePendiente("confirmar");
        PasarA(EstadoReserva.Confirmada);
    }

    public void Rechazar()
    {
        ExigirQueEstePendiente("rechazar");
        PasarA(EstadoReserva.Rechazada);
    }

    /// <summary>
    /// Cancelacion de CR6. Solo una confirmada tiene una unidad tomada que devolverle al modelo;
    /// una pendiente todavia no ocupa nada.
    /// </summary>
    public void Cancelar()
    {
        if (!PuedeCancelarse)
        {
            throw new DominioException($"Una reserva {Estado} no se puede cancelar");
        }

        bool teniaUnaUnidadTomada = EstaConfirmada;
        PasarA(EstadoReserva.Cancelada);
        if (teniaUnaUnidadTomada)
        {
            modelo.LiberarUnidad();
        }
    }

    private void ExigirQueEstePendiente(string operacion)
    {
        if (!EstaPendiente)
        {
            throw new DominioException(
                $"Solo se puede {operacion} una reserva pendiente, y esta esta {Estado}");
        }
    }

    private void PasarA(EstadoReserva estadoNuevo)
    {
        Estado = estadoNuevo;
        UltimaActualizacion = reloj.Ahora;
    }
}
