using Common.Protocolo.Vistas;
using Dominio;

namespace Servidor.Comandos;

// El alta y la modificación arman la misma ficha a partir del mismo payload.
internal static class FichaRecibida
{
    // La imagen no viaja en este comando: se asigna aparte, con CMD 15.
    public static FichaDeModelo Desde(DatosDeAltaDeModelo datos)
    {
        DatosDeModelo datosDeModelo = new(
            datos.Nombre,
            datos.Descripcion,
            datos.Precio,
            datos.FechaEntregaPrometida,
            string.Empty);

        return new FichaDeModelo(datosDeModelo, datos.Stock);
    }
}
