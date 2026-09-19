namespace Dominio;

public interface IReloj
{
    DateTime Ahora { get; }
}

public class RelojDelSistema : IReloj
{
    public DateTime Ahora => DateTime.UtcNow;
}
