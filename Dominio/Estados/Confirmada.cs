namespace Dominio.Estados;

/// <summary>
/// Reserva con unidad tomada (CR5). Al cancelarse es este estado, y no un if del llamador, quien
/// devuelve la unidad al modelo (CR6): "una confirmada ocupa una unidad" vive en un solo lugar.
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

    public override EstadoDeReserva Cancelar(Modelo modelo)
    {
        modelo.LiberarUnidad();
        return Cancelada.Instancia;
    }
}
