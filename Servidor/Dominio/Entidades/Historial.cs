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

    private Actividad(TipoActividad tipo, string detalle)
    {
        Tipo = tipo;
        Detalle = detalle;
        FechaHora = DateTime.UtcNow;
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

// Se lee y se escribe siempre con el candado global de Edison tomado; por eso no tiene lock propio.
public class Historial
{
    private readonly List<Actividad> _actividades = new();

    // Devuelve una copia para que quien la recorra no dependa del candado.
    public IReadOnlyList<Actividad> Actividades => _actividades.ToList();

    public void Registrar(Actividad actividad)
    {
        _actividades.Add(actividad);
    }
}
