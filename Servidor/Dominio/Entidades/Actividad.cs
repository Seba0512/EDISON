namespace Servidor.Dominio.Entidades;

public enum TipoActividad
{
    InicioSesion,
    CierreSesion,
    ModeloCreado,
    ModeloModificado,
    ModeloEliminado,
    ReservaCreada,
    ReservaCancelada,
    Compra
}

public class Actividad
{
    public TipoActividad Tipo { get; }

    public DateTime FechaHora { get; }

    public string Detalle { get; }

    private Actividad(TipoActividad tipo, string detalle, DateTime fechaHora)
    {
        Tipo = tipo;
        Detalle = detalle;
        FechaHora = fechaHora;
    }

    private Actividad(TipoActividad tipo, string detalle)
        : this(tipo, detalle, DateTime.UtcNow)
    {
    }

    // Rehidrata una actividad guardada. La fecha se guarda en UTC: la base la devuelve sin zona.
    internal static Actividad Restaurar(TipoActividad tipo, string detalle, DateTime fechaHoraUtc)
    {
        return new Actividad(tipo, detalle, DateTime.SpecifyKind(fechaHoraUtc, DateTimeKind.Utc));
    }

    public static Actividad InicioSesion()
    {
        return new Actividad(TipoActividad.InicioSesion, "Inicio de sesion");
    }

    public static Actividad CierreSesion()
    {
        return new Actividad(TipoActividad.CierreSesion, "Cierre de sesion");
    }

    public static Actividad ModeloCreado(string nombreModelo)
    {
        return new Actividad(TipoActividad.ModeloCreado, $"Creo el modelo {nombreModelo}");
    }

    public static Actividad ModeloModificado(string nombreModelo)
    {
        return new Actividad(TipoActividad.ModeloModificado, $"Modifico el modelo {nombreModelo}");
    }

    public static Actividad ModeloEliminado(string nombreModelo)
    {
        return new Actividad(TipoActividad.ModeloEliminado, $"Elimino el modelo {nombreModelo}");
    }

    public static Actividad ReservaCreada(string nombreModelo)
    {
        return new Actividad(TipoActividad.ReservaCreada, $"Solicito una reserva de {nombreModelo}");
    }

    public static Actividad ReservaCancelada(string nombreModelo)
    {
        return new Actividad(TipoActividad.ReservaCancelada, $"Cancelo su reserva de {nombreModelo}");
    }

    public static Actividad Compra(string nombreModelo)
    {
        return new Actividad(TipoActividad.Compra, $"Se confirmo su reserva de {nombreModelo}");
    }
}
