using Servidor.Dominio.Entidades;
using Servidor.Dominio.Excepciones;

namespace Servidor.Dominio.Repositorios;

public sealed record ModeloConConfirmadas(VehiculoModelo Modelo, int Confirmadas);

// Puerto de persistencia de modelos. Las operaciones que además dejan una actividad en el
// historial reciben la actividad, así la implementación puede guardar las dos cosas en una
// sola transacción.
public interface IRepositorioDeModelos
{
    Task<VehiculoModelo?> BuscarPorNombreAsync(string nombreModelo);

    Task<VehiculoModelo?> BuscarPorIdAsync(int idModelo);

    Task<IReadOnlyList<ModeloConConfirmadas>> ListarAsync();

    // Asigna el Id que le dio la base al modelo.
    Task AgregarAsync(VehiculoModelo modelo, Actividad actividad, string nombreUsuario);

    Task ActualizarAsync(VehiculoModelo modelo, Actividad actividad, string nombreUsuario);

    Task EliminarAsync(VehiculoModelo modelo, Actividad actividad, string nombreUsuario);
}

public static class RepositorioDeModelosExtensiones
{
    public static async Task<VehiculoModelo> ObtenerAsync(
        this IRepositorioDeModelos repositorio, string nombreModelo)
    {
        string nombre = nombreModelo?.Trim() ?? string.Empty;
        return await repositorio.BuscarPorNombreAsync(nombre)
            ?? throw new DominioException($"No existe el modelo {nombre}");
    }
}
