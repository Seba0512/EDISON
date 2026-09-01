using Dominio.Excepciones;

namespace Dominio;

/// <summary>
/// Solicitud de reserva de una unidad (CR5, CR6, CR9). Nace pendiente y un proceso diferido la
/// confirma o la rechaza pasados los segundos configurados.
///
/// La reserva no guarda que unidad ocupa: la unidad se asigna recien al confirmar y el vinculo vive
/// del lado de <see cref="UnidadVehiculo"/>. Con asignacion tardia el rechazo de CR5 es alcanzable
/// (dos pendientes sobre la ultima unidad, solo una confirma); con asignacion temprana nunca
/// ocurriria. Que el stock no baje por debajo de las pendientes lo cubre el caso de uso de CR3.
/// </summary>
public class Reserva
{
    public Guid Id { get; }

    public string NombreUsuario { get; }

    public string NombreModelo { get; }

    public EstadoReserva Estado { get; private set; }

    public DateTime FechaCreacion { get; }

    public DateTime UltimaActualizacion { get; private set; }

    public Reserva(string nombreUsuario, string nombreModelo)
    {
        Id = Guid.NewGuid();
        NombreUsuario = TextoValidado(nombreUsuario, "El usuario de la reserva no puede estar vacio");
        NombreModelo = TextoValidado(nombreModelo, "El modelo de la reserva no puede estar vacio");
        Estado = EstadoReserva.Pendiente;
        FechaCreacion = DateTime.UtcNow;
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

    /// <summary>Solo una reserva confirmada tiene una unidad tomada que haya que devolver (CR6).</summary>
    public bool OcupaUnaUnidad => EstaConfirmada;

    public bool FueHechaPor(string nombreUsuario)
    {
        return NombreUsuario.Equals(nombreUsuario?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public bool EsDelModelo(string nombreModelo)
    {
        return NombreModelo.Equals(nombreModelo?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public bool VencioElPlazo(int segundosDeEspera)
    {
        return EstaPendiente && DateTime.UtcNow >= FechaCreacion.AddSeconds(segundosDeEspera);
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

    public void Cancelar()
    {
        if (!PuedeCancelarse)
        {
            throw new DominioException($"Una reserva {Estado} no se puede cancelar");
        }

        PasarA(EstadoReserva.Cancelada);
    }

    private void ExigirQueEstePendiente(string operacion)
    {
        if (!EstaPendiente)
        {
            throw new DominioException($"Solo se puede {operacion} una reserva pendiente, y esta esta {Estado}");
        }
    }

    private void PasarA(EstadoReserva estadoNuevo)
    {
        Estado = estadoNuevo;
        UltimaActualizacion = DateTime.UtcNow;
    }

    private static string TextoValidado(string texto, string mensajeDeError)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            throw new DominioException(mensajeDeError);
        }

        return texto.Trim();
    }
}
