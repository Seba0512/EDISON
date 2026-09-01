namespace Dominio.Tiempo;

/// <summary>Reloj de produccion: la hora UTC de la maquina donde corre el servidor.</summary>
public class RelojDelSistema : IReloj
{
    public DateTime Ahora => DateTime.UtcNow;
}
