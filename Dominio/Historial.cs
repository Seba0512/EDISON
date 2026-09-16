namespace Dominio;

public class Historial
{
    private readonly object candado = new();
    private readonly List<Actividad> actividades = new();

    public IReadOnlyList<Actividad> Actividades
    {
        get
        {
            lock (candado)
            {
                return actividades.ToList();
            }
        }
    }

    public void Registrar(Actividad actividad)
    {
        lock (candado)
        {
            actividades.Add(actividad);
        }
    }
}
