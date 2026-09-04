namespace Dominio.Estados;

/// <summary>
/// Estado inicial de toda reserva (CR5): espera la resolucion diferida. Es el unico estado desde el
/// que se puede confirmar o rechazar, y cancelarla no toca el stock porque todavia no ocupa unidad.
/// </summary>
public class Pendiente : EstadoDeReserva
{
    private static readonly Lazy<Pendiente> instancia = new(() => new Pendiente());

    public static Pendiente Instancia => instancia.Value;

    private Pendiente()
    {
    }

    public override string Nombre => "Pendiente";

    public override bool EsActiva => true;

    public override bool EstaPendiente => true;

    public override EstadoDeReserva Confirmar()
    {
        return Confirmada.Instancia;
    }

    public override EstadoDeReserva Rechazar()
    {
        return Rechazada.Instancia;
    }

    public override EstadoDeReserva Cancelar(Modelo modelo)
    {
        return Cancelada.Instancia;
    }
}
