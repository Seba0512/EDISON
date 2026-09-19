using Server.Domain.Entities;
using Server.Domain.Exceptions;

namespace Server.Domain.Repositories;

public class RepositorioDeModelosEnMemoria : IRepositorioDeModelos
{
    private readonly Dictionary<string, Modelo> _modelos = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _candado = new();

    public void Agregar(Modelo modelo)
    {
        lock (_candado)
        {
            _modelos.Add(modelo.Nombre, modelo);
        }
    }

    public bool Existe(string nombreModelo)
    {
        lock (_candado)
        {
            return _modelos.ContainsKey(ClaveDe(nombreModelo));
        }
    }

    public Modelo Obtener(string nombreModelo)
    {
        lock (_candado)
        {
            string clave = ClaveDe(nombreModelo);
            if (!_modelos.TryGetValue(clave, out Modelo? modelo))
            {
                throw new DominioException($"No existe el modelo {clave}");
            }

            return modelo;
        }
    }

    public IReadOnlyList<Modelo> Todos()
    {
        lock (_candado)
        {
            // Copia: se recorre fuera del candado mientras otro hilo puede dar de alta.
            return _modelos.Values.ToList();
        }
    }

    public void Quitar(string nombreModelo)
    {
        lock (_candado)
        {
            _modelos.Remove(ClaveDe(nombreModelo));
        }
    }

    private static string ClaveDe(string nombreModelo)
    {
        return nombreModelo?.Trim() ?? string.Empty;
    }
}
