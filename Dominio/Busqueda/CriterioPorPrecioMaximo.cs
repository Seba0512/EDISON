using Dominio.Excepciones;

namespace Dominio.Busqueda;

public class CriterioPorPrecioMaximo : ICriterioDeBusqueda
{
    private readonly decimal _precioMaximo;

    public CriterioPorPrecioMaximo(decimal precioMaximo)
    {
        if (precioMaximo <= 0)
        {
            throw new DominioException("El precio maximo debe ser mayor a cero");
        }

        _precioMaximo = precioMaximo;
    }

    public bool Cumple(IVehiculoBuscable vehiculo)
    {
        return vehiculo.Precio <= _precioMaximo;
    }
}
