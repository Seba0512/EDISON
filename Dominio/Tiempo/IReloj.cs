namespace Dominio.Tiempo;

/// <summary>
/// Fuente de la hora para el dominio (DIP). Las clases que dependen del tiempo reciben un reloj en
/// lugar de llamar a <c>DateTime.UtcNow</c>: llamarlo es depender de una clase concreta y ademas
/// vuelve inverificable el plazo de CR5, que solo se podria probar esperando segundos reales.
/// </summary>
public interface IReloj
{
    DateTime Ahora { get; }
}
