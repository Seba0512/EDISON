using Dominio.Tiempo;

namespace Dominio.Tests;

public class RelojFalso : IReloj
{
    private DateTime ahora;

    public RelojFalso(DateTime inicio)
    {
        ahora = inicio;
    }

    public DateTime Ahora => ahora;

    public void Avanzar(TimeSpan tiempo)
    {
        ahora = ahora.Add(tiempo);
    }
}
