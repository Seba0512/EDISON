using Dominio;

namespace Dominio.Tests.Dobles;

public class AlmacenDeImagenesFalso : IAlmacenDeImagenes
{
    private readonly List<string> borrados = new();

    public IReadOnlyList<string> Borrados => borrados;

    public void Eliminar(string nombreArchivo)
    {
        borrados.Add(nombreArchivo);
    }
}
