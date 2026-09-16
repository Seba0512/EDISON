using Dominio.Excepciones;

namespace Dominio.Busqueda;

public class CriterioPorNombre : ICriterioDeBusqueda
{
    private readonly string textoBuscado;

    public CriterioPorNombre(string textoBuscado)
    {
        if (string.IsNullOrWhiteSpace(textoBuscado))
        {
            throw new DominioException("El texto a buscar no puede estar vacio");
        }

        this.textoBuscado = textoBuscado.Trim();
    }

    public bool Cumple(IVehiculoBuscable vehiculo)
    {
        return vehiculo.Nombre.Contains(textoBuscado, StringComparison.OrdinalIgnoreCase);
    }
}
