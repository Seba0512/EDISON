using Common.Protocolo.Vistas;

namespace Cliente.Vistas;

internal sealed class PresentadorDeReservas : PresentadorDeTabla<ReservaListada>
{
    protected override IReadOnlyList<string> Encabezados { get; } =
        ["ID", "MODELO", "ESTADO", "CREACION", "ACTUALIZACION"];

    protected override string MensajeSinElementos => "No tenés reservas.";

    protected override void MostrarTitulo()
        => Consola.Info("Mis reservas:");

    protected override string[] AFila(ReservaListada reserva)
        =>
        [
            reserva.Id.ToString(),
            reserva.NombreModelo,
            reserva.Estado,
            reserva.FechaCreacion.ToString(),
            reserva.UltimaActualizacion.ToString()
        ];
}
