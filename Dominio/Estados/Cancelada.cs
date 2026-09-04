namespace Dominio.Estados;

/// <summary>
/// Estado terminal de una reserva que su usuario dio de baja (CR6). No permite ninguna transicion.
/// </summary>
public class Cancelada : EstadoDeReserva
{
    private static readonly Lazy<Cancelada> instancia = new(() => new Cancelada());

    public static Cancelada Instancia => instancia.Value;

    private Cancelada()
    {
    }

    public override string Nombre => "Cancelada";
}
