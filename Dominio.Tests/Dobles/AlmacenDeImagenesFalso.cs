using Dominio;

namespace Dominio.Tests.Dobles;

/// <summary>
/// Almacen que solo anota que archivos le pidieron borrar. Permite verificar SR3 sin tocar el disco,
/// que es exactamente para lo que existe <see cref="IAlmacenDeImagenes"/>.
/// </summary>
public class AlmacenDeImagenesFalso : IAlmacenDeImagenes
{
    private readonly List<string> borrados = new();

    public IReadOnlyList<string> Borrados => borrados;

    public void Eliminar(string nombreArchivo)
    {
        borrados.Add(nombreArchivo);
    }
}
