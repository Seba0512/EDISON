using Dominio.Excepciones;

namespace Dominio.Busqueda;

public class CriterioPorUnidadesDisponibles : ICriterioDeBusqueda
{
    private readonly int minimoDeUnidades;

    public CriterioPorUnidadesDisponibles(int minimoDeUnidades)
    {
        if (minimoDeUnidades < 0)
        {
            throw new DominioException("El minimo de unidades disponibles no puede ser negativo");
        }

        this.minimoDeUnidades = minimoDeUnidades;
    }

    public bool Cumple(IVehiculoBuscable vehiculo)
    {
        return vehiculo.UnidadesDisponibles >= minimoDeUnidades;
    }
}
