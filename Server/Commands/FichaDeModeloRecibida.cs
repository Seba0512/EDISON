using Common;
using Server.Domain.Entities;

namespace Server.Commands;

// El alta y la modificación arman la misma ficha a partir del mismo payload.
internal static class FichaDeModeloRecibida
{
    public static FichaDeModelo De(Trama solicitud)
    {
        DatosDeAltaDeModelo datos = Mensajes.ADatosDeModelo(solicitud.DatosComoTexto());
        // La imagen no viaja en este comando: se asigna aparte, con CMD 15.
        DatosDeModelo datosDeModelo = new(
            datos.Nombre, datos.Descripcion, datos.Precio, datos.FechaEntregaPrometida, string.Empty);

        return new FichaDeModelo(datosDeModelo, datos.Stock);
    }
}
