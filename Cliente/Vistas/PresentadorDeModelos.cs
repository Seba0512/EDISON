using Common.Protocolo.Vistas;

namespace Cliente.Vistas;

internal sealed class PresentadorDeModelos : PresentadorDeTabla<ModeloListado>
{
    protected override IReadOnlyList<string> Encabezados { get; } =
        ["Nombre", "Precio", "Entrega", "Disponibles"];

    protected override IReadOnlyList<bool> AlinearADerecha { get; } = [false, true, false, true];

    protected override string MensajeSinElementos => "No se encontraron modelos.";

    protected override string[] AFila(ModeloListado modelo)
        =>
        [
            modelo.Nombre,
            modelo.Precio.ToString("0.00"),
            modelo.FechaEntregaPrometida.ToString("dd-MM-yyyy"),
            modelo.UnidadesDisponibles.ToString()
        ];
}
