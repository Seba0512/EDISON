using Dominio.Tiempo;

namespace Dominio;

public class Actividad
{
    public TipoActividad Tipo { get; }

    public DateTime FechaHora { get; }

    public string Detalle { get; }

    private Actividad(TipoActividad tipo, string detalle, IReloj reloj)
    {
        Tipo = tipo;
        Detalle = detalle;
        FechaHora = reloj.Ahora;
    }

    public static Actividad InicioSesion(IReloj reloj)
    {
        return new Actividad(TipoActividad.InicioSesion, "Inicio de sesion", reloj);
    }

    public static Actividad CierreSesion(IReloj reloj)
    {
        return new Actividad(TipoActividad.CierreSesion, "Cierre de sesion", reloj);
    }

    public static Actividad ModeloCreado(string nombreModelo, IReloj reloj)
    {
        return new Actividad(TipoActividad.ModeloCreado, $"Creo el modelo {nombreModelo}", reloj);
    }

    public static Actividad ModeloModificado(string nombreModelo, IReloj reloj)
    {
        return new Actividad(
            TipoActividad.ModeloModificado, $"Modifico el modelo {nombreModelo}", reloj);
    }

    public static Actividad ModeloEliminado(string nombreModelo, IReloj reloj)
    {
        return new Actividad(
            TipoActividad.ModeloEliminado, $"Elimino el modelo {nombreModelo}", reloj);
    }

    public static Actividad ReservaCreada(string nombreModelo, IReloj reloj)
    {
        return new Actividad(
            TipoActividad.ReservaCreada, $"Solicito una reserva de {nombreModelo}", reloj);
    }

    public static Actividad ReservaCancelada(string nombreModelo, IReloj reloj)
    {
        return new Actividad(
            TipoActividad.ReservaCancelada, $"Cancelo su reserva de {nombreModelo}", reloj);
    }

    public static Actividad Compra(string nombreModelo, IReloj reloj)
    {
        return new Actividad(
            TipoActividad.Compra, $"Se confirmo su reserva de {nombreModelo}", reloj);
    }
}
