namespace Server.Domain.Services;

public class RelojDelSistema : IReloj
{
    public DateTime Ahora => DateTime.UtcNow;
}
