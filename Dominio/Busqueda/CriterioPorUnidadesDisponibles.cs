using Dominio.Excepciones;

namespace Dominio.Busqueda;

public class CriterioPorUnidadesDisponibles : ICriterioDeBusqueda
{
    private readonly int _minimoDeUnidades;

    public CriterioPorUnidadesDisponibles(int minimoDeUnidades)
    {
        if (minimoDeUnidades < 0)
        {
            throw new DominioException("El minimo de unidades disponibles no puede ser negativo");
        }

        _minimoDeUnidades = minimoDeUnidades;
    }

    public bool Cumple(IVehiculoBuscable vehiculo)
    {
        return vehiculo.UnidadesDisponibles >= _minimoDeUnidades;
    }
}
