namespace Dominio.Estados;

/// <summary>
/// Estado terminal de una solicitud que no consiguio unidad (CR5). No permite ninguna transicion:
/// una rechazada no puede cancelarse (CR6) ni volver a resolverse.
/// </summary>
public class Rechazada : EstadoDeReserva
{
    private static readonly Lazy<Rechazada> instancia = new(() => new Rechazada());

    public static Rechazada Instancia => instancia.Value;

    private Rechazada()
    {
    }

    public override string Nombre => "Rechazada";
}
