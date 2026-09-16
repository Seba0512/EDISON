namespace Dominio.Estados;

public class Confirmada : EstadoDeReserva
{
    private static readonly Lazy<Confirmada> instancia = new(() => new Confirmada());

    public static Confirmada Instancia => instancia.Value;

    private Confirmada()
    {
    }

    public override string Nombre => "Confirmada";

    public override bool EsActiva => true;

    public override bool EstaConfirmada => true;

    public override EstadoDeReserva Cancelar()
    {
        return Cancelada.Instancia;
    }
}
