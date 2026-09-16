namespace Dominio.Estados;

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

    public override EstadoDeReserva Cancelar()
    {
        return Cancelada.Instancia;
    }
}
