namespace Dominio.Estados;

public class Rechazada : EstadoDeReserva
{
    private static readonly Lazy<Rechazada> instancia = new(() => new Rechazada());

    public static Rechazada Instancia => instancia.Value;

    private Rechazada()
    {
    }

    public override string Nombre => "Rechazada";
}
