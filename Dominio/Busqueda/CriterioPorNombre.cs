using Dominio.Excepciones;

namespace Dominio.Busqueda;

public class CriterioPorNombre : ICriterioDeBusqueda
{
    private readonly string _textoBuscado;

    public CriterioPorNombre(string textoBuscado)
    {
        if (string.IsNullOrWhiteSpace(textoBuscado))
        {
            throw new DominioException("El texto a buscar no puede estar vacio");
        }

        _textoBuscado = textoBuscado.Trim();
    }

    public bool Cumple(IVehiculoBuscable vehiculo)
    {
        return vehiculo.Nombre.Contains(_textoBuscado, StringComparison.OrdinalIgnoreCase);
    }
}
