namespace Dominio.Estados;

public class Cancelada : EstadoDeReserva
{
    private static readonly Lazy<Cancelada> instancia = new(() => new Cancelada());

    public static Cancelada Instancia => instancia.Value;

    private Cancelada()
    {
    }

    public override string Nombre => "Cancelada";
}
