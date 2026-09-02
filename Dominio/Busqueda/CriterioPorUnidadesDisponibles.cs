using Dominio.Excepciones;

namespace Dominio.Busqueda;

/// <summary>
/// Filtra por cantidad minima de unidades libres (CR7). Con minimo 1 responde el caso mas usado:
/// "mostrar solo los vehiculos disponibles".
/// </summary>
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
