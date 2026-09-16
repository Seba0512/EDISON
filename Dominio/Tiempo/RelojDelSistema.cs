namespace Dominio.Tiempo;

public class RelojDelSistema : IReloj
{
    public DateTime Ahora => DateTime.UtcNow;
}
