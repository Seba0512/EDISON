using Dominio.Excepciones;

namespace Dominio.Busqueda;

public class CriterioPorPrecioMaximo : ICriterioDeBusqueda
{
    private readonly decimal precioMaximo;

    public CriterioPorPrecioMaximo(decimal precioMaximo)
    {
        if (precioMaximo <= 0)
        {
            throw new DominioException("El precio maximo debe ser mayor a cero");
        }

        this.precioMaximo = precioMaximo;
    }

    public bool Cumple(IVehiculoBuscable vehiculo)
    {
        return vehiculo.Precio <= precioMaximo;
    }
}
