namespace Dominio.Estados;

/// <summary>
/// Reserva con una unidad concreta asignada (CR5). Permite cancelar; la propia reserva, como experta
/// en esa asociacion, devuelve exactamente esa unidad al agregado (CR6).
/// </summary>
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
