using Dominio.Excepciones;

namespace Dominio.Busqueda;

/// <summary>Filtra los modelos que no superan el precio pedido (CR7).</summary>
public class CriterioPrecioMaximo : ICriterioBusqueda
{
    private readonly decimal precioMaximo;

    public CriterioPrecioMaximo(decimal precioMaximo)
    {
        if (precioMaximo <= 0)
        {
            throw new DominioException("El precio maximo debe ser mayor a cero");
        }

        this.precioMaximo = precioMaximo;
    }

    public bool Cumple(Modelo modelo)
    {
        return modelo.Precio <= precioMaximo;
    }
}
