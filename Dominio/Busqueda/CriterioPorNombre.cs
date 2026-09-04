using Dominio.Excepciones;

namespace Dominio.Busqueda;

/// <summary>Filtra por coincidencia parcial en el nombre del modelo (CR7).</summary>
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
