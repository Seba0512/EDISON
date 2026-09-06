namespace Dominio.Tiempo;

/// <summary>Reloj de produccion: obtiene la hora UTC del sistema.</summary>
public class RelojDelSistema : IReloj
{
    public DateTime Ahora => DateTime.UtcNow;
}
