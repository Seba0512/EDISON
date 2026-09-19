namespace Server.Domain.Entities;

public class Historial
{
    private readonly object _candado = new();
    private readonly List<Actividad> _actividades = new();

    public IReadOnlyList<Actividad> Actividades
    {
        get
        {
            lock (_candado)
            {
                // Devuelve una copia porque otro hilo puede agregar una actividad mientras se recorre la lista.
                return _actividades.ToList();
            }
        }
    }

    public void Registrar(Actividad actividad)
    {
        lock (_candado)
        {
            _actividades.Add(actividad);
        }
    }
}
