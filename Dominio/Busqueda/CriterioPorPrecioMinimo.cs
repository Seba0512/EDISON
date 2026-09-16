using Dominio.Excepciones;

namespace Dominio.Busqueda;

public class CriterioPorPrecioMinimo : ICriterioDeBusqueda
{
    private readonly decimal precioMinimo;

    public CriterioPorPrecioMinimo(decimal precioMinimo)
    {
        if (precioMinimo < 0)
        {
            throw new DominioException("El precio minimo no puede ser negativo");
        }

        this.precioMinimo = precioMinimo;
    }

    public bool Cumple(IVehiculoBuscable vehiculo)
    {
        return vehiculo.Precio >= precioMinimo;
    }
}
