using Dominio.Excepciones;

namespace Dominio.Busqueda;

/// <summary>Filtra los modelos cuyo precio llega al minimo pedido (CR7).</summary>
public class CriterioPrecioMinimo : ICriterioBusqueda
{
    private readonly decimal precioMinimo;

    public CriterioPrecioMinimo(decimal precioMinimo)
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
