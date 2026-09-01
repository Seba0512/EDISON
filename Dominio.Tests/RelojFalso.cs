using Dominio.Tiempo;

namespace Dominio.Tests;

/// <summary>
/// Reloj controlado por la prueba. Es la razon por la que <see cref="IReloj"/> existe: permite
/// verificar el plazo de CR5 adelantando el tiempo en vez de dormir el hilo durante segundos reales.
/// </summary>
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
